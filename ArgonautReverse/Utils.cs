using System.Drawing;

using ArgonautReverse.IO;

namespace ArgonautReverse
{
	public record struct XY(int X, int Y);

	public static class Utils
	{
		public const int PaddingSize = 2048;

		//Padding

		public static int RoundUpPadding(int n) => (n + PaddingSize - 1) & (-PaddingSize);


		public static void PadOut2048Bytes(WadWriter bio)
		{
			var padding = new byte[RoundUpPadding(bio.Position) - bio.Position];
			bio.WriteBytes(padding);
		}


		public static void PadIn2048Bytes(FileReader bio)
		{
			bio.Position = RoundUpPadding(bio.Position);
		}


		// Images


		/// <summary>Converts 15-bit high color raw bytes (see doc @Textures.md#15-bit-high-color) into a flattened list of RGB colors.</summary>
		public static Color[] parse_high_color(Span<byte> data_in, bool has_alpha, bool legacy_alpha=false)// TODO Legacy alpha (Croc 2)
		{
			//ABGR1555
			
			//TODO: Alpha bit is unused?

			var res = new Color[data_in.Length/2];
			for(int i=0; i<res.Length; i++)
			{
				var color_bytes = BitConverter.ToUInt16(data_in.Slice(i*2, 2));
				res[i] = Color.FromArgb
				(
					(!has_alpha || (color_bytes != 0)) ? 255 : 0,
					(byte)(((color_bytes & 0x1F) * 527 + 23) >> 6),
					(byte)((((color_bytes & 0x3E0) >> 5) * 527 + 23) >> 6),
					(byte)((((color_bytes & 0x7C00) >> 10) * 527 + 23) >> 6)
				);
			}
			return res;
		}


		public static byte[] parse_4bits_paletted(Span<byte> data)
		{
			var res = new byte[data.Length];
			for(int i=0; i<data.Length; i++)
			{
				var b = data[i];
				//Just flip the order
				res[i] = (byte)(((b & 0x0F) << 4) | ((b & 0xF0) >> 4));
			}
			return res;
		}

		public static Color[] parse_palette(Span<byte> data, int n_palette_colors, bool has_alpha, bool legacy_alpha=false, int start=0)
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


		public static float Deg2Rad(float degrees) => degrees * MathF.PI / 180f;
		public static float Rad2Deg(float radians) => radians * 180f / MathF.PI;
	}
}