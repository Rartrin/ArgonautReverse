using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.DPSX
{
	public sealed class LevelFile
	{
		public readonly ChunksMatrix chunks_matrix;

		public LevelFile(ChunksMatrix chunks_matrix)
		{
			this.chunks_matrix = chunks_matrix;
		}

		public static LevelFile parse(WadReader data_in, WadFlag wadFlag)
		{
			//Track Objects
			var n_chunk_models = data_in.Read<int>();
			var _chunk_model_headers = new Model3DHeader[n_chunk_models];
			for(int i=0; i<n_chunk_models; i++)
			{
				_chunk_model_headers[i] = Model3DHeader.Parse(data_in, true);
			}
			var chunk_models = new LevelGeom3DData[n_chunk_models];
			for(int i=0; i<n_chunk_models; i++)
			{
				chunk_models[i] = LevelGeom3DData.Parse(data_in, header:_chunk_model_headers[i]);
			}

			uint localPoolSize = 8;
			if((wadFlag&WadFlag.WF_LOCALPOOLSIZE)!=0)
			{
				if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception();
				}
				localPoolSize = data_in.Read<uint>();
			}
			localPoolSize <<= 10;

			uint maxParticles = 200;
			if((wadFlag&WadFlag.WF_PARTICLESIZE)!=0)
			{
				//TODO: This check should not be needed
				if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					maxParticles = data_in.Read<uint>();
				}
			}

			//Track pieces
			var n_sub_chunks = data_in.Read<int>();

			Map map = null;
			if(n_sub_chunks != 0)
			{
				map = Map.Parse(data_in, wadFlag, chunk_models);
				Utils.Assert(n_sub_chunks == map.NumberOfPieces);
			}

			if(map != null)
			{
				IReadOnlyList<int> parse_chunks_info(MAPINDEX mapIndex)
				{
					if(mapIndex == null){return Array.Empty<int>();}

					var chunks_ids_list = new List<int>();
					var linked_chunk_offset = mapIndex;
					while(linked_chunk_offset != null)
					{
						chunks_ids_list.Add((int)linked_chunk_offset.Index);
						linked_chunk_offset = linked_chunk_offset.Next;
					}
					return chunks_ids_list;
				}
				
				var _sub_chunks_height_0 = new Dictionary<int,(int _0, int _1)>();
				
				var _chunks_matrix = new IReadOnlyList<int>[map.MapXY];
				for(int i=0; i<map.MapZ; i++)
				{
					for(int j=0; j<map.MapX; j++)
					{
						var index = i * map.MapX + j;
						var mapIndex = map.Grid[(int)index];
						if(mapIndex == null)
						{
							_chunks_matrix[index] = null;
						}
						else
						{
							var sub_chunk_ids = parse_chunks_info(mapIndex);
							_chunks_matrix[index] = sub_chunk_ids;
							foreach(var sub_chunk_id in sub_chunk_ids)
							{
								_sub_chunks_height_0.Add(sub_chunk_id, (i, j));
							}
						}
					}
				}

				var _sub_chunks_height = new Dictionary<int,int>();
				var _sub_chunks_rotation = new Dictionary<int,ChunkRotation>();
				for(int i=0; i<n_sub_chunks; i++)
				{
					var pos = map.Positions[i];

					Utils.Assert(pos.rot.vx == 0);
					Utils.Assert(pos.rot.vy==0 || pos.rot.vy==(1<<10) || pos.rot.vy==(2<<10) || pos.rot.vy==(3<<10));
					Utils.Assert(pos.rot.vz == 0);
					Utils.Assert(pos.rot.pad == 0);

					Utils.Assert(pos.trn.vx == (2048 + 4096 * _sub_chunks_height_0[i]._1));// Chunks are 4096-large, so +2048 for the chunk's center
					Utils.Assert(pos.trn.vz == (2048 + 4096 * _sub_chunks_height_0[i]._0));
					Utils.Assert(pos.trn.pad == 0);
					
					_sub_chunks_rotation[i] = (ChunkRotation)(pos.rot.vy >> 10);
					_sub_chunks_height[i] = pos.trn.vy;
				}

				var chunks_holders = new ChunkHolder[map.MapXY];
				for(int i=0; i<map.MapXY; i++)
				{
					if(_chunks_matrix[i] is not null)
					{
						var sub_chunks = _chunks_matrix[i].Select(sub_chunk_id => new SubChunk
						(
							chunk_models[map.Pieces[sub_chunk_id]],
							_sub_chunks_height[sub_chunk_id],
							_sub_chunks_rotation[sub_chunk_id]
						)).ToList();

						if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
						{
							chunks_holders[i] = new ChunkHolder(sub_chunks, map.ZoneData[i], map.LightTuples?[i]);
						}
						else
						{
							chunks_holders[i] = new ChunkHolder(sub_chunks);
						}
					}
					else
					{
						if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
						{
							chunks_holders[i] = new ChunkHolder(zone_id:map.ZoneData[i], lightTuples: map.LightTuples?[i]);
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
						map.ZoneData is not null
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