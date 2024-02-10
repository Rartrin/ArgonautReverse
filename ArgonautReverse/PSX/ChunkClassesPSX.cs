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
		public readonly ZonePSX zone_id;
		public readonly LightTuplePSX? LightTuples;

		public int Count => Subchunks.Count;

		public ChunkHolderPSX(IReadOnlyList<SubChunkPSX> sub_chunks = null, ZonePSX zone_id = null, LightTuplePSX? lightTuples = null)
		{
			Subchunks = sub_chunks ?? Array.Empty<SubChunkPSX>();
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
		public readonly ZonePSX max_zone_id;

		public ChunksMatrixPSX(IReadOnlyList<ChunkHolderPSX> chunks_holders, IReadOnlyList<TObjectDataPSX> chunks_models, int n_rows, int n_columns, bool has_zone_ids)
		{
			ChunkHolders = chunks_holders;

			this.n_rows = n_rows;
			this.n_columns = n_columns;
			this.chunks_models = chunks_models;
			if(has_zone_ids)
			{
				max_zone_id = chunks_holders.Select(chunk_holder => chunk_holder.zone_id).MaxBy(zone => zone.Zone);
			}
			else
			{
				max_zone_id = null;
			}
		}

		public int n_filled_chunks => ChunkHolders.Count(chunk => chunk.Subchunks.Count != 0);

		public override string ToString() => chunks_visual_map();

		public string chunks_visual_map()
		{
			return string.Join('\n',
				Enumerable.Range(0, n_rows).Select(x => string.Join(' ',
					Enumerable.Range(0, n_columns).Select(y =>
							ChunkHolders[x * n_columns + y] != null ? "█" : "░"
					)
				))
			);
		}

		public string chunks_visual_ids()
		{
			return string.Join('\n',
				Enumerable.Range(0, n_rows).Select(x => string.Join(' ',
						Enumerable.Range(0, n_columns).Select(y =>
							ChunkHolders[x * n_columns + y] != null ? $"{x * n_columns + y,-4}" : "░░░░"
						)
					)
				)
			);
		}

		public string subchunks_visual_ids()
		{
			int subchunk_id = 0;
			var res = new StringWriter();
			for(int x = 0; x < n_rows; x++)
			{
				for(int y = 0; y < n_columns; y++)
				{
					if(y != 0)
					{
						res.Write(' ');
					}
					var subchunks = ChunkHolders[x * n_columns + y];
					if(subchunks != null)
					{
						res.Write($"{subchunk_id,-4}");
						subchunk_id += subchunks.Count;
					}
					else
					{
						res.Write("░░░░");
					}
				}
				res.WriteLine();
			}
			return res.ToString();
		}

		public string chunks_visual_zone_ids()
		{
			if(max_zone_id is null)
			{
				return "There are no zone ids in this level.";
			}
			var res = new StringWriter();
			for(int x = 0; x < n_rows; x++)
			{
				for(int y = 0; y < n_columns; y++)
				{
					if(y != 0)
					{
						res.Write(' ');
					}
					var zone_id = ChunkHolders[x * n_columns + y].zone_id;
					if(zone_id is not null && zone_id.Zone != max_zone_id.Zone)
					{
						res.Write($"{zone_id.Zone,-3}");
					}
					else
					{
						res.Write("░░░");
					}
				}
				res.WriteLine();
			}
			return res.ToString();
		}
		public (int X, int Z) x_z_coords(int chunk_id)
		{
			// Chunks are 4096-large, so +2048 is needed to point to the chunk's center
			return (
				4096 * (chunk_id % n_columns) + 2048,
				4096 * (chunk_id / n_columns) + 2048//Floor division
			);
		}
	}
}