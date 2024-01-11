using System.Text;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class LevelFile:BaseDataClass
	{
		public readonly ChunksMatrix chunks_matrix;

		public LevelFile(ChunksMatrix chunks_matrix)
		{
			this.chunks_matrix = chunks_matrix;
		}

		public static LevelFile parse(WadReader data_in, WadFlag wadFlag)
		{
			//Track Objects
			var n_chunk_models = data_in.ReadInt32();
			var _chunk_model_headers = new Model3DHeader[n_chunk_models];
			for(int i=0; i<n_chunk_models; i++)
			{
				_chunk_model_headers[i] = Model3DHeader.Parse(data_in);
			}
			var chunk_models = new LevelGeom3DData[n_chunk_models];
			for(int i=0; i<n_chunk_models; i++)
			{
				chunk_models[i] = LevelGeom3DData.Parse(data_in, header:_chunk_model_headers[i]);
			}

			UInt32 localPoolSize = 8;
			if((wadFlag&WadFlag.WF_LOCALPOOLSIZE)!=0)
			{
				if(data_in.Version == CROC_2_DEMO_PS1_DUMMY.Instance)
				{
					throw new Exception();
				}
				localPoolSize = data_in.ReadUInt32();
			}
			localPoolSize <<= 10;

			UInt32 maxParticles = 200;
			if((wadFlag&WadFlag.WF_PARTICLESIZE)!=0)
			{
				//TODO: This check should not be needed
				if(data_in.Version != CROC_2_DEMO_PS1_DUMMY.Instance)
				{
					maxParticles = data_in.ReadUInt32();
				}
			}

			//Track pieces
			var n_sub_chunks = data_in.ReadInt32();

			Map map;
			if(n_sub_chunks != 0)
			{
				map = Map.Parse(data_in, wadFlag);
				Utils.Assert(n_sub_chunks == map.NumberOfPieces);

				var raw_chunks_matrix = new int[map.MapXY];
				for(int i=0; i<map.MapXY; i++)
				{
					raw_chunks_matrix[i] = data_in.ReadInt32();
				}
		
				var _sub_chunks_height_0 = new Dictionary<int,(int _0, int _1)>();
				var _sub_chunks_height = new Dictionary<int,int>();

				var chunks_info_start_offset = data_in.Position;

				IReadOnlyList<int> parse_chunks_info(int offset)
				{
					if(offset == -1)
					{
						return Array.Empty<int>();
					}

					var chunks_ids_list = new List<int>();
					int linked_chunk_offset = offset;
					while(linked_chunk_offset != -1)
					{
						data_in.Seek(chunks_info_start_offset + linked_chunk_offset);
						chunks_ids_list.Add(data_in.ReadInt32());
						linked_chunk_offset = data_in.ReadInt32();
					}
					return chunks_ids_list;
				}
				var _chunks_matrix = new IReadOnlyList<int>[map.MapXY];
				for(int i=0; i<map.MapZ; i++)
				{
					for(int j=0; j<map.MapX; j++)
					{
						var index = i * map.MapX + j;
						var chunk_info_offset = raw_chunks_matrix[index];
						if(chunk_info_offset != -1)
						{
							var sub_chunk_ids = parse_chunks_info(chunk_info_offset);
							_chunks_matrix[index] = sub_chunk_ids;
							foreach(var sub_chunk_id in sub_chunk_ids)
							{
								_sub_chunks_height_0.Add(sub_chunk_id, (i, j));
							}
						}
						else
						{
							_chunks_matrix[index] = null;
						}
					}
				}

				data_in.Seek(chunks_info_start_offset + 8 * n_sub_chunks);
				int[] zone_ids;
				byte[][] fvw_data;
				if(data_in.Version != CROC_2_DEMO_PS1_DUMMY.Instance)
				{
					var header256bytes = data_in.ReadBytes(256);
					var n_zone_ids = data_in.ReadInt32();
					zone_ids = new int[n_zone_ids];
					for(int i=0; i<n_zone_ids; i++)
					{
						zone_ids[i] = data_in.ReadInt32();
					}
					Utils.Assert(n_zone_ids == map.MapXY);

					if(Encoding.ASCII.GetString(data_in.ReadBytes(4)) == "fvw\x00")
					{
						fvw_data = new byte[map.MapXY][];
						for(int i=0; i<map.MapXY;i++)
						{
							fvw_data[i] = data_in.ReadBytes(2);
						}
					}
					else
					{
						fvw_data = null;
						data_in.Seek(-4, SeekOrigin.Current);
					}
				}
				else
				{
					zone_ids = null;
					fvw_data = null;
				}
				var _sub_chunks_rotation = new Dictionary<int,ChunkRotation>();
				for(int i=0; i<n_sub_chunks; i++)
				{
					var rotation = data_in.ReadUInt32BE();
					Utils.Assert(rotation==0 || rotation==4 || rotation==8 || rotation==12);
					_sub_chunks_rotation[i] = (ChunkRotation)rotation;
					Utils.Assert(data_in.ReadInt32() == 0);//b"\x00\x00\x00\x00"
					var x = data_in.ReadInt32();
					var y = data_in.ReadInt32();
					var z = data_in.ReadInt32();
					Utils.Assert(data_in.ReadInt32() == 0);//b"\x00\x00\x00\x00"
					Utils.Assert(x == 2048 + 4096 * _sub_chunks_height_0[i]._1);  // Chunks are 4096-large, so +2048 for the chunk's center
					Utils.Assert(z == 2048 + 4096 * _sub_chunks_height_0[i]._0);
					_sub_chunks_height[i] = y;
				}
				var chunks_models_mapping = data_in.ReadUInt32Array(n_sub_chunks);

				byte[][] lighting_headers = null;
				if(data_in.Version != CROC_2_DEMO_PS1_DUMMY.Instance)
				{
					lighting_headers = data_in.ReadArrayOfByteArrays(84, map.NumberOfDoors.Value);
				}

				//This might be Waypoints
				var idk_4 = data_in.ReadArray<Waypoint>((int)map.NumberOfWP);//data_in.ReadArrayOfByteArrays(36, (int)map.NumberOfWP);

				for(int i=0; i<map.NumberOfStrats; i++)
				{
					data_in.Seek(24, SeekOrigin.Current);
					var actor_offset = data_in.ReadInt32();
					data_in.Seek(32, SeekOrigin.Current);
					var actor_sound_level = data_in.ReadInt32();
				}
				int[] add_models_mapping;
				if(data_in.Version!=CROC_2_DEMO_PS1.Instance && data_in.Version!=CROC_2_DEMO_PS1_DUMMY.Instance)
				{
					add_models_mapping = new int[map.NumberOfOtherPieces.Value];
					for(int i=0; i<map.NumberOfOtherPieces; i++)
					{
						data_in.Seek(16, SeekOrigin.Current);
						add_models_mapping[i] = data_in.ReadInt32();
						data_in.Seek(4, SeekOrigin.Current);
					}
					var n_idk2 = data_in.ReadInt32();
					data_in.Seek(32 * n_idk2, SeekOrigin.Current);  // TODO Reverse this
				}
				else
				{
					add_models_mapping = null;
					data_in.Seek(32 * n_sub_chunks, SeekOrigin.Current);  // Two different 32-bytes long structures
					data_in.Seek(32 * n_sub_chunks, SeekOrigin.Current);
					data_in.Seek((data_in.Version == CROC_2_DEMO_PS1.Instance) ? 32 : 92, SeekOrigin.Current);
				}
				if(data_in.Version == CROC_2_PS1.Instance)
				{
					data_in.Seek(30732, SeekOrigin.Current);
				}
				else if(data_in.Version != CROC_2_DEMO_PS1_DUMMY.Instance && n_sub_chunks != 0)
				{
					var sub_chunks_n_lighting = data_in.ReadUInt32Array(n_sub_chunks);
					var sub_chunks_n_add_lighting = data_in.ReadUInt32Array(map.NumberOfOtherPieces.Value);
					for(int model_id=0; model_id<n_sub_chunks; model_id++)
					{
						for(int i=0; i<sub_chunks_n_lighting[model_id]; i++)
						{
							var size = 4 * chunk_models[chunks_models_mapping[model_id]].Data.n_vertices;
							data_in.Seek(size, SeekOrigin.Current);
						}
					}
					for(int model_id=0; model_id<map.NumberOfOtherPieces; model_id++)
					{
						for(int i=0; i<sub_chunks_n_add_lighting[model_id]; i++)
						{
							var size = 4 * chunk_models[add_models_mapping[model_id]].Data.n_vertices;
							data_in.Seek(size, SeekOrigin.Current);
						}
					}
					if(data_in.Version != CROC_2_DEMO_PS1.Instance)// Not present in Croc 2 Demo Dummy
					{
						var idk_size = data_in.ReadInt32();
						if(idk_size != 0)
						{
							data_in.Seek(4 + idk_size, SeekOrigin.Current);
						}
						else
						{
							data_in.Seek(-4, SeekOrigin.Current);
						}
						var n_idk3 = data_in.ReadInt32();
						if(n_idk3 == 0)
						{
							data_in.Seek(-4, SeekOrigin.Current);
						}
						var idk3 = data_in.ReadArrayOfByteArrays(40, n_idk3);//var idk3 = [int.from_bytes(data_in.read(40), "little") for _ in range(n_idk3)]
					}
					data_in.Seek(12, SeekOrigin.Current);
				}
				var chunks_holders = new ChunkHolder[map.MapXY];
				for(int i=0; i<map.MapXY; i++)
				{
					if(_chunks_matrix[i] is not null)
					{
						var sub_chunks = _chunks_matrix[i].Select(sub_chunk_id => new SubChunk(
							chunk_models[chunks_models_mapping[sub_chunk_id]],
							_sub_chunks_height[sub_chunk_id],
							_sub_chunks_rotation[sub_chunk_id]
						)).ToList();

						if(data_in.Version != CROC_2_DEMO_PS1_DUMMY.Instance)
						{
							chunks_holders[i] = new ChunkHolder(sub_chunks, zone_ids[i], fvw_data?[i]);
						}
						else
						{
							chunks_holders[i] = new ChunkHolder(sub_chunks);
						}
					}
					else
					{
						if(data_in.Version != CROC_2_DEMO_PS1_DUMMY.Instance)
						{
							chunks_holders[i] = new ChunkHolder(zone_id:zone_ids[i], fvw_data: fvw_data?[i]);
						}
						else
						{
							chunks_holders[i] = new ChunkHolder();
						}
					}
				}
				return new LevelFile
				(
					new ChunksMatrix
					(
						chunks_holders,
						chunk_models,
						(int)map.MapZ,
						(int)map.MapX,
						zone_ids is not null
					)
				);
			}
			else
			{
				return new LevelFile
				(
					new ChunksMatrix
					(
						Array.Empty<ChunkHolder>(),
						chunk_models,
						0,
						0,
						false
					)
				);
			}
		}
	}
}