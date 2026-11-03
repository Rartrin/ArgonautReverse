using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadChunks.PSX;

namespace ArgonautReverse.PSX
{
	public sealed class LevelFilePSX
	{
		public readonly IReadOnlyList<ObjectDataPSX> StratObjectData;
		public readonly IReadOnlyList<AnimationDataPSX> AnimationData;

		public readonly ChunksMatrixPSX chunks_matrix;

		public readonly MapPSX? map;

		public uint LocalPoolSize;
		public uint MaxParticles;

		private LevelFilePSX(IReadOnlyList<ObjectDataPSX> stratObjectData, IReadOnlyList<AnimationDataPSX> animationData, ChunksMatrixPSX chunks_matrix, MapPSX? map, uint localPoolSize, uint maxParticles)
		{
			StratObjectData = stratObjectData;
			AnimationData = animationData;
			this.chunks_matrix = chunks_matrix;
			this.map = map;
			LocalPoolSize = localPoolSize;
			MaxParticles = maxParticles;
		}

		public static LevelFilePSX Setup(DPSXChunk chunk, WadReader data_in)
		{
			var stratObjectData = new ObjectDataPSX[chunk.StratObjects.Length];
			for(int i = 0; i < chunk.StratObjects.Length; i++)
			{
				stratObjectData[i] = ObjectDataPSX.Setup(data_in, chunk.StratObjects[i]);
			}

			var animationData = new AnimationDataPSX[chunk.Animations.Length];
			for(int i = 0; i < chunk.Animations.Length; i++)
			{
				animationData[i] = AnimationDataPSX.Setup(chunk.Animations[i]);
			}

			//Track Objects
			var chunk_models = new TObjectDataPSX[chunk.TrackObjects.Length];
			for(int i = 0; i < chunk_models.Length; i++)
			{
				chunk_models[i] = TObjectDataPSX.Setup(data_in, chunk.TrackObjects[i]);
			}

			if(chunk.Map != null)
			{
				static IReadOnlyList<int> parse_chunks_info(MapIndexPSX mapIndex)
				{
					if(mapIndex == null){return [];}

					var chunks_ids_list = new List<int>();
					var linked_chunk_offset = mapIndex;
					while(linked_chunk_offset != null)
					{
						chunks_ids_list.Add((int)linked_chunk_offset.Index);
						linked_chunk_offset = linked_chunk_offset.Next;
					}
					return chunks_ids_list;
				}

				var _sub_chunks_height_0 = new Dictionary<int, (int z, int x)>();

				var _chunks_matrix = new IReadOnlyList<int>?[chunk.Map.MapXY];
				for(int z = 0; z < chunk.Map.MapZ; z++)
				{
					for(int x = 0; x < chunk.Map.MapX; x++)
					{
						var index = z * chunk.Map.MapX + x;
						var mapIndex = chunk.Map.Grid[z][x];
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
								_sub_chunks_height_0.Add(sub_chunk_id, (z, x));
							}
						}
					}
				}

				var _sub_chunks_height = new Dictionary<int, int>();
				var _sub_chunks_rotation = new Dictionary<int, ChunkRotationPSX>();
				for(int i = 0; i < chunk.Map.NumberOfPieces; i++)
				{
					var pos = chunk.Map.Positions[i];

					Utils.Assert(pos.rot.vx == 0);
					Utils.Assert(pos.rot.vy == 0 || pos.rot.vy == 1 << 10 || pos.rot.vy == 2 << 10 || pos.rot.vy == 3 << 10);
					Utils.Assert(pos.rot.vz == 0);
					Utils.Assert(pos.rot.pad == 0);

					Utils.Assert(pos.trn.vx == 2048 + 4096 * _sub_chunks_height_0[i].x);// Chunks are 4096-large, so +2048 for the chunk's center
					Utils.Assert(pos.trn.vz == 2048 + 4096 * _sub_chunks_height_0[i].z);
					Utils.Assert(pos.trn.pad == 0);

					_sub_chunks_rotation[i] = (ChunkRotationPSX)(pos.rot.vy >> 10);
					_sub_chunks_height[i] = pos.trn.vy;
				}

				var chunks_holders = new ChunkHolderPSX[chunk.Map.MapXY];
				for(int i = 0; i < chunk.Map.MapXY; i++)
				{
					SubChunkPSX[]? subChunks = null;
					if(_chunks_matrix[i] is IReadOnlyList<int> sub_chunk_ids)
					{
						var sub_chunks = new SubChunkPSX[sub_chunk_ids.Count];
						for(int s=0; s< sub_chunk_ids.Count; s++)
						{
							var sub_chunk_id = sub_chunk_ids[s];
							sub_chunks[s] = new SubChunkPSX
							(
								chunk_models[chunk.Map.Pieces[sub_chunk_id]],
								_sub_chunks_height[sub_chunk_id],
								_sub_chunks_rotation[sub_chunk_id]
							);
						}
					}
					chunks_holders[i] = new ChunkHolderPSX(subChunks, chunk.Map.ZoneData?[i], chunk.Map.LightTuples?[i]);
				}

				return new LevelFilePSX
				(
					stratObjectData,
					animationData,
					new ChunksMatrixPSX
					(
						chunks_holders,
						chunk_models,
						chunk.Map.MapZ,
						chunk.Map.MapX,
						chunk.Map.ZoneData is not null
					),
					chunk.Map,
					chunk.LocalPoolSize,
					chunk.MaxParticles
				);
			}
			else
			{
				return new LevelFilePSX
				(
					stratObjectData,
					animationData,
					new ChunksMatrixPSX
					(
						[],
						chunk_models,
						0,
						0,
						false
					),
					chunk.Map,
					chunk.LocalPoolSize,
					chunk.MaxParticles
				);
			}
		}
	}
}