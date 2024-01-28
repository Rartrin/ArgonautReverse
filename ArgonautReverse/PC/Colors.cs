using System.Runtime.InteropServices;
using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public struct Color3F
	{
		public float value0;
		public float value1;
		public float value2;

		public Color3F()
		{
			value0 = 0;
			value1 = 0;
			value2 = 0;
		}
		public Color3F(float value0, float value1, float value2)
		{
			this.value0 = value0;
			this.value1 = value1;
			this.value2 = value2;
		}
	}


	public struct Color4F
	{
		public float value0;
		public float value1;
		public float value2;
		public float alpha;

		public Color4F()
		{
			this.value0 = 0;
			this.value1 = 0;
			this.value2 = 0;
			this.alpha = 0;
		}
		public Color4F(float value0, float value1, float value2, float alpha)
		{
			this.value0 = value0;
			this.value1 = value1;
			this.value2 = value2;
			this.alpha = alpha;
		}

		public readonly Color3F GetColor3F()
		{
			return new Color3F(value0, value1, value2);
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 4)]
	public unsafe struct Color32:IReadable<Color32>
	{
		[FieldOffset(0)]public uint raw;

		[FieldOffset(0)]public byte v0;
		[FieldOffset(1)]public byte v1;
		[FieldOffset(2)]public byte v2;
		[FieldOffset(3)]public byte alpha;

		public Color32(uint raw)
		{
			this.raw = raw;
		}

		public Color32(byte v0, byte v1, byte v2, byte alpha)
		{
			this.v0 = v0;
			this.v1 = v1;
			this.v2 = v2;
			this.alpha = alpha;
		}

		public readonly uint GetD3DColor() => this.raw;

		public static Color32 Parse(WadReader reader)
		{
			var raw = reader.Read<uint>();
			return new Color32(raw);
		}
	}

	public struct ColorBGRA32:IReadable<ColorBGRA32>
	{
		private Color32 color;

		public ColorBGRA32(uint raw) => color.raw = raw;
		public ColorBGRA32(byte b, byte g, byte r, byte a) => color = new Color32(b, g, r, a);

		public byte R
		{
			readonly get => color.v2;
			set => color.v2 = value;
		}
		public byte G
		{
			readonly get => color.v1;
			set => color.v1 = value;
		}
		public byte B
		{
			readonly get => color.v0;
			set => color.v0 = value;
		}
		public byte A
		{
			readonly get => color.alpha;
			set => color.alpha = value;
		}

		public static ColorBGRA32 Parse(WadReader reader)
		{
			var raw = reader.Read<uint>();
			return new ColorBGRA32(raw);
		}
	}

	public struct ColorRGB555:IReadable<ColorRGB555>
	{
		public ushort Data;

		public ColorRGB555(ushort data)
		{
			Data = data;
		}

		public byte Red5
		{
			readonly get => (byte)(Data >> 0 & 0x1F);
			set => Data = (ushort)(Data & ~0x001F | (value & 0x1F) << 0);
		}
		public byte Green5
		{
			readonly get => (byte)(Data >> 5 & 0x1F);
			set => Data = (ushort)(Data & ~0x03E0 | (value & 0x1F) << 5);
		}
		public byte Blue5
		{
			readonly get => (byte)(Data >> 10 & 0x1F);
			set => Data = (ushort)(Data & ~0x7C00 | (value & 0x1F) << 10);
		}
		public bool Alpha1
		{
			readonly get => (Data & 0x8000) != 0;
			set
			{
				if(value)
				{
					Data |= 0x8000;
				}
				else
				{
					Data &= unchecked((ushort)~0x8000);
				}
			}
		}

		private const int Max5Bit = (1 << 5) - 1;
		public byte Red8
		{
			readonly get => (byte)(Red5 * 255 / Max5Bit);
			set => Red5 = (byte)(Red5 * Max5Bit / 255);
		}
		public byte Green8
		{
			readonly get => (byte)(Green5 * 255 / Max5Bit);
			set => Green5 = (byte)(Green5 * Max5Bit / 255);
		}
		public byte Blue8
		{
			readonly get => (byte)(Blue5 * 255 / Max5Bit);
			set => Blue5 = (byte)(Blue5 * Max5Bit / 255);
		}
		public readonly byte Alpha8 => Alpha1 ? (byte)0xFF : (byte)0;

		public float RedF
		{
			readonly get => Red5 / 31f;
			set => Red5 = (byte)(Red5 * 31);
		}
		public float GreenF
		{
			readonly get => Green5 / 31f;
			set => Green5 = (byte)(Green5 * 31);
		}
		public float BlueF
		{
			readonly get => Blue5 / 31f;
			set => Blue5 = (byte)(Blue5 * 31);
		}

		public static ColorRGB555 Parse(WadReader reader)
		{
			return new ColorRGB555(reader.Read<ushort>());
		}
	}
}
