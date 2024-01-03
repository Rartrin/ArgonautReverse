namespace ArgonautReverse.WadSections.TPSX
{
	public record class Box(int _0,int _1,int _2,int _3);

	public sealed class TextureData:BaseDataClass
	{
		public readonly TextureFlags flags;
		public readonly IReadOnlyList<XY> raw_coords;
		public readonly Box cm;
		public readonly int? palette_start;

		public TextureData(TextureFlags flags, IReadOnlyList<XY> raw_coords, Box cm, int? palette_start)
		{
			this.flags = flags;
			Utils.Assert(((int)this.flags & 0xFE00) == 0);
			this.raw_coords = raw_coords;
			this.cm = cm;
			this.palette_start = palette_start;
		}

		public static TextureData parse(Parser data_in, Configuration conf)
		{
			//Bottom Right
			var brX = data_in.ReadByte();
			var brY = data_in.ReadByte();
			
			var palette_info = data_in.ReadUInt16();
			
			//Bottom Left
			var blX = data_in.ReadByte();
			var blY = data_in.ReadByte();

			var flags = (TextureFlags)data_in.ReadUInt16();
			
			//Top Right
			var trX = data_in.ReadByte();
			var trY = data_in.ReadByte();

			//Top Left
			var tlX = data_in.ReadByte();
			var tlY = data_in.ReadByte();

			// Raw coordinates are contained in a 1024x1024, 512x1024 or 256x1024 space
			// (16-colors paletted, 256-colors paletted and non-paletted high color respectively)
			var raw_coords = new XY[]
			{
				new XY(brX, brY),
				new XY(blX, blY),
				new XY(trX, trY),
				new XY(tlX, tlY)
			};

			Box cm;
			// Coordinates Mapping, needed to put the coordinates in the right order
			// (top-left, top-right, bottom-left then bottom-right)
			if(raw_coords[0].X > raw_coords[1].X)
			{
				if(raw_coords[0].Y > raw_coords[2].Y)
				{
					cm = new Box(3, 2, 1, 0);
				}
				else
				{
					cm = new Box(1, 0, 3, 2);
				}
			}
			else
			{
				if(raw_coords[0].Y > raw_coords[2].Y)
				{
					cm = new Box(2, 3, 0, 1);
				}
				else
				{
					cm = new Box(0, 1, 2, 3);
				}
			}

			int? palette_start = null;
			if((flags&TextureFlags.IS_NOT_PALETTED)==0)
			{
				palette_start = ((palette_info & 0xFFC0) << 3) + ((palette_info & 0xF) << 5);
			}

			// The top-left x coordinate of the 256-colors or high color textures needs to be corrected
			// 1024x1024 space -> 512x1024 or 256x1024 space respectively
			return new TextureData(flags, raw_coords, cm, palette_start);
		}

		/// <summary>Textures tend to be better delimited when rounded to the nearest multiple of 2</summary>
		public static IReadOnlyList<XY> round_coords(IEnumerable<XY> coords)
		{
			return coords.Select(coord => new XY(2 * (int)MathF.Ceiling(coord.X / 2f), 2 * (int)MathF.Ceiling(coord.Y / 2f))).ToArray();
		}

		/// <summary>Unordered coordinates of this texture (256x1024, 512x1024 or 1024x1024 space)</summary>
		public IReadOnlyList<XY> input_coords
		{
			get
			{
				return round_coords(this.raw_coords.Select(raw_coord => new XY
				(
						raw_coord.X + (256 / this.flags.correction_ratio()) * this.flags.n_column(),//Floor divide
						raw_coord.Y + 256 * this.flags.n_row())
				));
			}
		}

		/// <summary>Unordered coordinates of this texture (1024x1024 space)</summary>
		public IReadOnlyList<XY> output_coords
		{
			get
			{
				var x_correction = (
					this.raw_coords[this.cm._0].X * this.flags.correction_ratio()
					- this.raw_coords[this.cm._0].X
				);
				return round_coords(this.raw_coords.Select(raw_coord => new XY
				(
					raw_coord.X + x_correction + 256 * this.flags.n_column(),
					raw_coord.Y + 256 * this.flags.n_row()
				)));
			}
		}

		/// <summary>Left, top, right, bottom coordinates of this texture (256x1024, 512x1024 or 1024x1024 space)</summary>
		public Box input_box
		{
			get
			{
				var ic = this.input_coords;
				return new Box
				(
					ic[this.cm._0].X,
					ic[this.cm._0].Y,
					ic[this.cm._3].X,
					ic[this.cm._3].Y
				);
			}
		}

		/// <summary>x, y coordinates of this texture's top left corner (1024x1024 space)</summary>
		public XY output_top_left_corner => this.output_coords[this.cm._0];
	}
}