using System.Collections;

namespace ArgonautReverse.WadChunks.DPSX
{
	public enum ChunkRotation
	{
		TOP = 0,
		RIGHT = 1,
		BOTTOM = 2,
		LEFT = 3,
	}

	public sealed class SubChunk
	{
		public readonly LevelGeom3DData model_3d_data;
		public readonly int height;
		public readonly ChunkRotation rotation;
		public SubChunk(LevelGeom3DData model_3d_data, int height, ChunkRotation rotation)
		{
			this.model_3d_data = model_3d_data;
			this.height = height;
			this.rotation = rotation;
		}
	}

	public sealed class ChunkHolder
	{
		public readonly IReadOnlyList<SubChunk> Subchunks;
		public readonly ZONE zone_id;
		public readonly LightTuple? LightTuples;

		public int Count => Subchunks.Count;

		public ChunkHolder(IReadOnlyList<SubChunk> sub_chunks = null, ZONE zone_id = null, LightTuple? lightTuples = null)
		{
			Subchunks = sub_chunks ?? Array.Empty<SubChunk>();
			this.zone_id = zone_id;
			this.LightTuples = lightTuples;
		}
	}

	public sealed class ChunksMatrix
	{
		public readonly IReadOnlyList<ChunkHolder> ChunkHolders;
		public readonly IReadOnlyList<LevelGeom3DData> chunks_models;
		public readonly int n_rows;
		public readonly int n_columns;
		public readonly bool has_zone_ids;
		public readonly ZONE max_zone_id;

		public ChunksMatrix(IReadOnlyList<ChunkHolder> chunks_holders, IReadOnlyList<LevelGeom3DData> chunks_models, int n_rows, int n_columns, bool has_zone_ids)
		{
			ChunkHolders = chunks_holders;

			this.n_rows = n_rows;
			this.n_columns = n_columns;
			this.chunks_models = chunks_models;
			if(has_zone_ids)
			{
				this.max_zone_id = chunks_holders.Select(chunk_holder => chunk_holder.zone_id).MaxBy(zone => zone.Zone);
			}
			else
			{
				this.max_zone_id = null;
			}
		}

		public int n_filled_chunks => ChunkHolders.Count(chunk => chunk.Subchunks.Count != 0);

		public override string ToString() => this.chunks_visual_map();

		public string chunks_visual_map()
		{
			return string.Join('\n',
				Enumerable.Range(0,this.n_rows).Select(x => string.Join(' ',
					Enumerable.Range(0,this.n_columns).Select(y =>
							ChunkHolders[x * this.n_columns + y]!=null ? "█" : "░"
					)
				))
			);
		}

		public string chunks_visual_ids()
		{
			return string.Join('\n',
				Enumerable.Range(0,this.n_rows).Select(x => string.Join(' ',
						Enumerable.Range(0,this.n_columns).Select(y =>
							ChunkHolders[x * this.n_columns + y]!=null ? $"{x * this.n_columns + y, -4}" : "░░░░"
						)
					)
				)
			);
		}

		public string subchunks_visual_ids()
		{
			int subchunk_id = 0;
			var res = new StringWriter();
			for(int x=0; x<this.n_rows; x++)
			{
				for(int y=0; y<this.n_columns; y++)
				{
					if(y != 0)
					{
						res.Write(' ');
					}
					var subchunks = ChunkHolders[x * this.n_columns + y];
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
			if(this.max_zone_id is null)
			{
				return "There are no zone ids in this level.";
			}
			var res = new StringWriter();
			for(int x=0; x<this.n_rows; x++)
			{
				for(int y=0; y<this.n_columns; y++)
				{
					if(y != 0)
					{
						res.Write(' ');
					}
					var zone_id = ChunkHolders[x * this.n_columns + y].zone_id;
					if(zone_id is not null && zone_id.Zone != this.max_zone_id.Zone)
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
		public (int X,int Z) x_z_coords(int chunk_id)
		{
			// Chunks are 4096-large, so +2048 is needed to point to the chunk's center
			return (
				4096 * (chunk_id % this.n_columns) + 2048,
				4096 * (chunk_id / this.n_columns) + 2048//Floor division
			);
		}
	}
}