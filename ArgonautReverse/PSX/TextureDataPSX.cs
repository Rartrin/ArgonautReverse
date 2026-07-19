using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public sealed class TextureDataPSX
	{
		public record class Box(int TL, int TR, int BL, int BR);

		public readonly TextureFlagsPSX flags;
		public readonly XY[] raw_coords;
		public readonly Box cm;
		public readonly int? palette_start;

		public TextureDataPSX(TextureFlagsPSX flags, XY[] raw_coords, Box cm, int? palette_start)
		{
			this.flags = flags;
			Utils.Assert(((int)this.flags & 0xFE00) == 0);
			this.raw_coords = raw_coords;
			this.cm = cm;
			this.palette_start = palette_start;
		}

		public static TextureDataPSX Parse(WadReader data_in)
		{
			//Bottom Right
			var brX = data_in.Read<byte>();
			var brY = data_in.Read<byte>();

			var colorLUT = data_in.Read<ushort>();

			//Bottom Left
			var blX = data_in.Read<byte>();
			var blY = data_in.Read<byte>();

			var flags = (TextureFlagsPSX)data_in.Read<ushort>();

			//Top Right
			var trX = data_in.Read<byte>();
			var trY = data_in.Read<byte>();

			//Top Left
			var tlX = data_in.Read<byte>();
			var tlY = data_in.Read<byte>();

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
			if(!flags.HasNoPaletted)
			{
				palette_start = ((colorLUT & 0xFFC0) << 3) + ((colorLUT & 0xF) << 5);
			}

			// The top-left x coordinate of the 256-colors or high color textures needs to be corrected
			// 1024x1024 space -> 512x1024 or 256x1024 space respectively
			return new TextureDataPSX(flags, raw_coords, cm, palette_start);
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