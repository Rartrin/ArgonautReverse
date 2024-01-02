
namespace ArgonautReverse
{
	public record struct XY(int X, int Y);

	public static class Utils
	{
		public const int padding_size = 2048;


		//Padding

		public static long round_up_padding(long n) => (n + padding_size - 1) & (-padding_size);


		public static void pad_out_2048_bytes(Stream bio)//BufferedIOBase | BinaryIO
		{
			var padding = new byte[round_up_padding(bio.Position) - bio.Position];
			bio.Write(padding);
		}


		public static void pad_in_2048_bytes(Stream bio)//BufferedIOBase | BinaryIO
		{
			bio.Position = round_up_padding(bio.Position);
		}


		// Images


		/// <summary>Converts 15-bit high color raw bytes (see doc @Textures.md#15-bit-high-color) into a flattened list of RGB colors.</summary>
		public static int[] parse_high_color(Span<byte> data_in, bool has_alpha, bool legacy_alpha=false)// TODO Legacy alpha (Croc 2)
		{
			int perPixel = has_alpha ? 4 : 3;
			var res = new int[perPixel*data_in.Length];
			for(int i=0; i<data_in.Length; i+=2)
			{
				var color_bytes = BitConverter.ToUInt16(data_in.Slice(i, 2));
				res[perPixel*i + 0] = ((color_bytes & 0x1F) * 527 + 23) >> 6;
				res[perPixel*i + 1] = (((color_bytes & 0x3E0) >> 5) * 527 + 23) >> 6;
				res[perPixel*i + 2] = (((color_bytes & 0x7C00) >> 10) * 527 + 23) >> 6;
				if(has_alpha)
				{
					res[perPixel*i + 3] = (color_bytes != 0) ? 255 : 0;
				}
			}
			return res;
		}


		public static int[] parse_4bits_paletted(Span<byte> data)
		{
			var res = new int[data.Length*2];
			for(int i=0; i<data.Length; i++)
			{
				var b = data[i];
				res[i*2 + 0] = b & 15;
				res[i*2 + 1] = (b & 240) >> 4;
			}
			return res;
		}

		public static int[] parse_palette(Span<byte> data, int n_palette_colors, bool has_alpha, bool legacy_alpha=false, int start=0)
		{
			return parse_high_color(data.Slice(start, 2*n_palette_colors), has_alpha, legacy_alpha);
		}

		public static void Assert(bool condition)
		{
			if(!condition)
			{
				throw new Exception("Assertion failed");
			}
		}
	}
}