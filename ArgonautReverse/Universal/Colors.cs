using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
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
			value0 = 0;
			value1 = 0;
			value2 = 0;
			alpha = 0;
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

	public struct ColorBGRA32(byte blue, byte green, byte red, byte alpha):IReadable<ColorBGRA32>, IWritable
	{
		public byte B = blue;
		public byte G = green;
		public byte R = red;
		public byte A = alpha;

		public static ColorBGRA32 Parse(WadReader reader)
		{
			return reader.ReadData<ColorBGRA32>();
		}

		public readonly void Write(WadWriter writer)
		{
			writer.WriteData(this);
		}
	}

	public struct ColorABGR555(ushort data):IReadable<ColorABGR555>
	{
		public ushort Data = data;

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

		public static ColorABGR555 Parse(WadReader reader)
		{
			return new ColorABGR555(reader.Read<ushort>());
		}

		public readonly ColorARGB555 ToRGB555()
		{
			ushort data = (ushort)(Data&0x83E0);//Copy over Alpha and Green
			data |= (ushort)((Data&0x001F)<<10);//Move Red
			data |= (ushort)((Data&0x7C00)>>10);//Move Blue
			return new(data);
		}
	}

	public struct ColorARGB555(ushort data)
	{
		public ushort Data = data;

		public byte Blue5
		{
			readonly get => (byte)(Data >> 0 & 0x1F);
			set => Data = (ushort)(Data & ~0x001F | (value & 0x1F) << 0);
		}
		public byte Green5
		{
			readonly get => (byte)(Data >> 5 & 0x1F);
			set => Data = (ushort)(Data & ~0x03E0 | (value & 0x1F) << 5);
		}
		public byte Red5
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
	}
}
