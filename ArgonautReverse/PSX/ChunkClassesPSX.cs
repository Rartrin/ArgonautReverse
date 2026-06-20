using ArgonautReverse.Universal;

namespace ArgonautReverse.PSX
{
	public enum ChunkRotationPSX
	{
		TOP = 0,
		RIGHT = 1,
		BOTTOM = 2,
		LEFT = 3,
	}

	public sealed class SubChunkPSX
	{
		public readonly TObjectDataPSX model_3d_data;
		public readonly int height;
		public readonly ChunkRotationPSX rotation;
		public SubChunkPSX(TObjectDataPSX model_3d_data, int height, ChunkRotationPSX rotation)
		{
			this.model_3d_data = model_3d_data;
			this.height = height;
			this.rotation = rotation;
		}
	}

	public sealed class ChunkHolderPSX
	{
		public readonly IReadOnlyList<SubChunkPSX> Subchunks;
		public readonly Zone? zone_id;
		public readonly LightTuplePSX? LightTuples;

		public int Count => Subchunks.Count;

		public ChunkHolderPSX(IReadOnlyList<SubChunkPSX>? sub_chunks = null, Zone? zone_id = null, LightTuplePSX? lightTuples = null)
		{
			Subchunks = sub_chunks ?? [];
			this.zone_id = zone_id;
			LightTuples = lightTuples;
		}
	}

	public sealed class ChunksMatrixPSX
	{
		public readonly IReadOnlyList<ChunkHolderPSX> ChunkHolders;
		public readonly IReadOnlyList<TObjectDataPSX> chunks_models;
		public readonly int n_rows;
		public readonly int n_columns;
		public readonly bool has_zone_ids;
		public readonly Zone max_zone_id;

		public ChunksMatrixPSX(IReadOnlyList<ChunkHolderPSX> chunks_holders, IReadOnlyList<TObjectDataPSX> chunks_models, int n_rows, int n_columns, bool has_zone_ids)
		{
			ChunkHolders = chunks_holders;

			this.n_rows = n_rows;
			this.n_columns = n_columns;
			this.chunks_models = chunks_models;
			if(has_zone_ids)
			{
				max_zone_id = chunks_holders.Select(chunk_holder => chunk_holder.zone_id).MaxBy(zone => zone.InfoIndex);
			}
			else
			{
				max_zone_id = null;
			}
		}

		public int n_filled_chunks => ChunkHolders.Count(chunk => chunk.Subchunks.Count != 0);

		public (int X, int Z) x_z_coords(int chunk_id) =>
		(
			// Chunks are 4096-large, so +2048 is needed to point to the chunk's center
			X: 4096 * (chunk_id % n_columns) + 2048,
			Z: 4096 * (chunk_id / n_columns) + 2048//Floor division
		);
	}
}