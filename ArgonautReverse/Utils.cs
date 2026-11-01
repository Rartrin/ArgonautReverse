using ArgonautReverse.IO;
using ArgonautReverse.Universal;

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


		public static byte[] PadIn2048Bytes(FileReader bio)
		{
			var padding = RoundUpPadding(bio.Position) - bio.Position;
			return bio.ReadArray<byte>(padding);
			//bio.Position = RoundUpPadding(bio.Position);
		}

		// Images

		/// <summary>Converts 15-bit high color raw bytes (see doc @Textures.md#15-bit-high-color) into a flattened list of RGB colors.</summary>
		public static ColorARGB555[] ParsePixelData(Span<byte> data_in, bool has_alpha, bool legacy_alpha=false)// TODO Legacy alpha (Croc 2)
		{
			//ABGR1555
			
			//TODO: Alpha bit is unused?

			var res = new ColorARGB555[data_in.Length/2];
			for(int i=0; i<res.Length; i++)
			{
				var color_bytes = BitConverter.ToUInt16(data_in.Slice(i*2, 2));
				res[i] = new ColorABGR555(color_bytes).ToRGB555();
			}
			return res;
		}

		/// <summary>Converts 15-bit high color raw bytes (see doc @Textures.md#15-bit-high-color) into a flattened list of RGB colors.</summary>
		public static ColorABGR555[] ParsePalette(Span<byte> data, bool hasAlpha, bool legacyAlpha=false)// TODO Legacy alpha (Croc 2)
		{
			//ABGR1555
			
			//TODO: Alpha bit is unused?

			var res = new ColorABGR555[data.Length/2];
			for(int i=0; i<res.Length; i++)
			{
				var color_bytes = BitConverter.ToUInt16(data.Slice(i*2, 2));
				res[i] = new ColorABGR555(color_bytes);
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

		public static void Assert(bool condition)
		{
			//Don't replace with Debug.Assert(). This is used for validating inputs from files.
			if(!condition)
			{
				throw new Exception("Assertion failed");
			}
		}

		public static float Deg2Rad(float degrees) => degrees * MathF.PI / 180f;
		public static float Rad2Deg(float radians) => radians * 180f / MathF.PI;
	}
}