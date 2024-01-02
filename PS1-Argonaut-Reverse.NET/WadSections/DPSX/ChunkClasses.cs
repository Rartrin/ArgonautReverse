using System.Collections;

namespace ArgonautReverse.WadSections.DPSX
{
	public enum ChunkRotation//(IntEnum)
	{
		TOP = 0,
		RIGHT = 4,
		BOTTOM = 8,
		LEFT = 12,
	}

	public sealed class SubChunk:BaseDataClass
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

	public sealed class ChunkHolder:BaseDataClass,IReadOnlyList<SubChunk>
	{
		private readonly IReadOnlyList<SubChunk> list;
		public readonly int? zone_id;
		public readonly byte[] fvw_data;

		public int Count => list.Count;
		public SubChunk this[int index] => list[index];

		public ChunkHolder(IReadOnlyList<SubChunk> sub_chunks = null, int? zone_id = null, byte[] fvw_data = null)
		{
			list = sub_chunks ?? Array.Empty<SubChunk>();
			this.zone_id = zone_id;
			this.fvw_data = fvw_data;
		}

		public IEnumerator<SubChunk> GetEnumerator() => list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
	}

	public sealed class ChunksMatrix:IReadOnlyList<ChunkHolder>
	{
		private readonly IReadOnlyList<ChunkHolder> list;
		public readonly IReadOnlyList<LevelGeom3DData> chunks_models;
		public readonly int n_rows;
		public readonly int n_columns;
		public readonly bool has_zone_ids;
		public readonly int? max_zone_id;

		public int Count => list.Count;
		public ChunkHolder this[int index] => list[index];
		public IEnumerator<ChunkHolder> GetEnumerator() => list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

		public ChunksMatrix(IReadOnlyList<ChunkHolder> chunks_holders, IReadOnlyList<LevelGeom3DData> chunks_models, int n_rows, int n_columns, bool has_zone_ids)
		{
			list = chunks_holders;

			this.n_rows = n_rows;
			this.n_columns = n_columns;
			this.chunks_models = chunks_models;
			if(has_zone_ids)
			{
				this.max_zone_id = chunks_holders.Select(chunk_holder => chunk_holder.zone_id).Max();
			}
			else
			{
				this.max_zone_id = null;
			}
		}
	
		//@property
		public int n_filled_chunks => this.Count(chunk => chunk.Any());

		public override string ToString() => this.chunks_visual_map();

		public string chunks_visual_map()
		{
			return string.Join('\n',
				Enumerable.Range(0,this.n_rows).Select(x => string.Join(' ',
					Enumerable.Range(0,this.n_columns).Select(y =>
							this[x * this.n_columns + y]!=null ? "█" : "░"
					)
				))
			);
		}

		public string chunks_visual_ids()
		{
			return string.Join('\n',
				Enumerable.Range(0,this.n_rows).Select(x => string.Join(' ',
						Enumerable.Range(0,this.n_columns).Select(y =>
							this[x * this.n_columns + y]!=null ? (x * this.n_columns + y).ToString().PadRight(4) : "░░░░"
						)
					)
				)
			);
		}

		public string subchunks_visual_ids()
		{
			int subchunk_id = 0;
			var res = new StringWriter();//StringIO()
			for(int x=0; x<this.n_rows; x++)
			{
				for(int y=0; y<this.n_columns; y++)
				{
					if(y != 0)
					{
						res.Write(' ');
					}
					var subchunks = this[x * this.n_columns + y];
					if(subchunks != null)
					{
						res.Write(subchunk_id.ToString().PadRight(4));
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
			var res = new StringWriter();//StringIO
			for(int x=0; x<this.n_rows; x++)
			{
				for(int y=0; y<this.n_columns; y++)
				{
					if(y != 0)
					{
						res.Write(' ');
					}
					var zone_id = this[x * this.n_columns + y].zone_id;
					if(zone_id is not null && zone_id != this.max_zone_id)
					{
						res.Write(zone_id.ToString().PadRight(3));
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