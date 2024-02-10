using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public sealed class LevelFilePSX
	{
		public readonly ChunksMatrixPSX chunks_matrix;

		public readonly MapPSX map;

		public LevelFilePSX(ChunksMatrixPSX chunks_matrix, MapPSX map)
		{
			this.chunks_matrix = chunks_matrix;
			this.map = map;
		}

		public static LevelFilePSX Parse(WadReader data_in, WadFlagPSX wadFlag)
		{
			//Track Objects
			var n_chunk_models = data_in.Read<int>();
			var _chunk_model_headers = new TObjectPSX[n_chunk_models];
			for(int i = 0; i < n_chunk_models; i++)
			{
				_chunk_model_headers[i] = TObjectPSX.Parse(data_in);
			}
			var chunk_models = new TObjectDataPSX[n_chunk_models];
			for(int i = 0; i < n_chunk_models; i++)
			{
				chunk_models[i] = TObjectDataPSX.Parse(data_in, _chunk_model_headers[i]);
			}

			uint localPoolSize = 8;
			if((wadFlag & WadFlagPSX.WF_LOCALPOOLSIZE) != 0)
			{
				if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception();
				}
				localPoolSize = data_in.Read<uint>();
			}
			localPoolSize <<= 10;

			uint maxParticles = 200;
			if((wadFlag & WadFlagPSX.WF_PARTICLESIZE) != 0)
			{
				//TODO: This check should not be needed
				if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					maxParticles = data_in.Read<uint>();
				}
			}

			//Track pieces
			var n_sub_chunks = data_in.Read<int>();

			MapPSX map = null;
			if(n_sub_chunks != 0)
			{
				map = MapPSX.Parse(data_in, wadFlag, chunk_models);
				Utils.Assert(n_sub_chunks == map.NumberOfPieces);
			}

			if(map != null)
			{
				IReadOnlyList<int> parse_chunks_info(MapIndexPSX mapIndex)
				{
					if(mapIndex == null) { return Array.Empty<int>(); }

					var chunks_ids_list = new List<int>();
					var linked_chunk_offset = mapIndex;
					while(linked_chunk_offset != null)
					{
						chunks_ids_list.Add((int)linked_chunk_offset.Index);
						linked_chunk_offset = linked_chunk_offset.Next;
					}
					return chunks_ids_list;
				}

				var _sub_chunks_height_0 = new Dictionary<int, (int _0, int _1)>();

				var _chunks_matrix = new IReadOnlyList<int>[map.MapXY];
				for(int i = 0; i < map.MapZ; i++)
				{
					for(int j = 0; j < map.MapX; j++)
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

				var _sub_chunks_height = new Dictionary<int, int>();
				var _sub_chunks_rotation = new Dictionary<int, ChunkRotationPSX>();
				for(int i = 0; i < n_sub_chunks; i++)
				{
					var pos = map.Positions[i];

					Utils.Assert(pos.rot.vx == 0);
					Utils.Assert(pos.rot.vy == 0 || pos.rot.vy == 1 << 10 || pos.rot.vy == 2 << 10 || pos.rot.vy == 3 << 10);
					Utils.Assert(pos.rot.vz == 0);
					Utils.Assert(pos.rot.pad == 0);

					Utils.Assert(pos.trn.vx == 2048 + 4096 * _sub_chunks_height_0[i]._1);// Chunks are 4096-large, so +2048 for the chunk's center
					Utils.Assert(pos.trn.vz == 2048 + 4096 * _sub_chunks_height_0[i]._0);
					Utils.Assert(pos.trn.pad == 0);

					_sub_chunks_rotation[i] = (ChunkRotationPSX)(pos.rot.vy >> 10);
					_sub_chunks_height[i] = pos.trn.vy;
				}

				var chunks_holders = new ChunkHolderPSX[map.MapXY];
				for(int i = 0; i < map.MapXY; i++)
				{
					if(_chunks_matrix[i] is not null)
					{
						var sub_chunks = _chunks_matrix[i].Select(sub_chunk_id => new SubChunkPSX
						(
							chunk_models[map.Pieces[sub_chunk_id]],
							_sub_chunks_height[sub_chunk_id],
							_sub_chunks_rotation[sub_chunk_id]
						)).ToList();

						if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
						{
							chunks_holders[i] = new ChunkHolderPSX(sub_chunks, map.ZoneData[i], map.LightTuples?[i]);
						}
						else
						{
							chunks_holders[i] = new ChunkHolderPSX(sub_chunks);
						}
					}
					else
					{
						if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
						{
							chunks_holders[i] = new ChunkHolderPSX(zone_id: map.ZoneData[i], lightTuples: map.LightTuples?[i]);
						}
						else
						{
							chunks_holders[i] = new ChunkHolderPSX();
						}
					}
				}

				return new LevelFilePSX
				(
					new ChunksMatrixPSX
					(
						chunks_holders,
						chunk_models,
						(int)map.MapZ,
						(int)map.MapX,
						map.ZoneData is not null
					),
					map
				);
			}
			else
			{
				return new LevelFilePSX
				(
					new ChunksMatrixPSX
					(
						Array.Empty<ChunkHolderPSX>(),
						chunk_models,
						0,
						0,
						false
					),
					map
				);
			}
		}
	}
}