namespace ArgonautReverse.PSX
{
	public sealed class TextureDataPSX
	{
		public record class Box(int TL, int TR, int BL, int BR);

		public readonly TextureHeaderFlagsPSX flags;
		public readonly XY[] raw_coords;
		public readonly Box cm;
		public readonly int? palette_start;

		public TextureDataPSX(TextureHeaderFlagsPSX flags, XY[] raw_coords, Box cm, int? palette_start)
		{
			this.flags = flags;
			this.raw_coords = raw_coords;
			this.cm = cm;
			this.palette_start = palette_start;
		}

		public static TextureDataPSX Parse(TextureHeaderPSX header)
		{
			// Raw coordinates are contained in a 1024x1024, 512x1024 or 256x1024 space
			// (16-colors paletted, 256-colors paletted and non-paletted high color respectively)
			var raw_coords = new XY[]
			{
				new XY(header.BR_x, header.BR_y),
				new XY(header.BL_x, header.BL_y),
				new XY(header.TR_x, header.TR_y),
				new XY(header.TL_x, header.TL_y)
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
			if(!header.TPage.HasNoPaletted)
			{
				palette_start = ((header.TClut & 0xFFC0) << 3) + ((header.TClut & 0xF) << 5);
			}

			// The top-left x coordinate of the 256-colors or high color textures needs to be corrected
			// 1024x1024 space -> 512x1024 or 256x1024 space respectively
			return new TextureDataPSX(header.TPage, raw_coords, cm, palette_start);
		}

		/// <summary>Textures tend to be better delimited when rounded to the nearest multiple of 2</summary>
		private static XY RoundCoords(int x, int y) => new(2 * (int)MathF.Ceiling(x / 2f), 2 * (int)MathF.Ceiling(y / 2f));

		/// <summary>Unordered coordinates of this texture (256x1024, 512x1024 or 1024x1024 space)</summary>
		public XY[] GetInputCoords()
		{
			var ret = new XY[4];
			for(int i=0; i<4; i++)
			{
				ret[i] = RoundCoords
				(
					raw_coords[i].X + 256 / flags.correction_ratio() * flags.n_column(),//Floor divide
					raw_coords[i].Y + 256 * flags.n_row()
				);
			}
			return ret;
		}

		/// <summary>Unordered coordinates of this texture (1024x1024 space)</summary>
		public XY[] GetOutputCoords()
		{
			var x_correction = (raw_coords[cm.TL].X * flags.correction_ratio()) - raw_coords[cm.TL].X;

			var ret = new XY[4];
			for(int i=0; i<4; i++)
			{
				ret[i] = RoundCoords
				(
					raw_coords[i].X + x_correction + 256 * flags.n_column(),
					raw_coords[i].Y + 256 * flags.n_row()
				);
			}
			return ret;
		}

		/// <summary>Left, top, right, bottom coordinates of this texture (256x1024, 512x1024 or 1024x1024 space)</summary>
		public Box GetInputBox()
		{
			var ic = GetInputCoords();
			return new Box
			(
				ic[cm.TL].X,
				ic[cm.TL].Y,
				ic[cm.BR].X,
				ic[cm.BR].Y
			);
		}

		/// <summary>x, y coordinates of this texture's top left corner (1024x1024 space)</summary>
		public XY output_top_left_corner => GetOutputCoords()[cm.TL];
	}
}