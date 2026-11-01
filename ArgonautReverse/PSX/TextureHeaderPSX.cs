using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public readonly struct TextureHeaderFlagsPSX(ushort data):IReadable<TextureHeaderFlagsPSX>,IWritable
	{
		private const ushort HAS_256_COLORS_PALETTE = 0x80;
		private const ushort IS_NOT_PALETTED = 0x100;

		public readonly bool Has256ColorPalette
		{
			get => (data & HAS_256_COLORS_PALETTE) != 0;
			init
			{
				if(value)
				{
					data |= HAS_256_COLORS_PALETTE;
				}
				else
				{
					data &= unchecked((ushort)~HAS_256_COLORS_PALETTE);
				}
			}
		}
		public readonly bool HasNoPaletted
		{
			get => (data & IS_NOT_PALETTED) != 0;
			init
			{
				if(value)
				{
					data |= IS_NOT_PALETTED;
				}
				else
				{
					data &= unchecked((ushort)~IS_NOT_PALETTED);
				}
			}
		}

		public readonly int n_row() => ((data & 4) >> 1) + ((data & 16) >> 4);

		public readonly int n_column() => data & 3;

		///<summary>Correction Ratio, needed for non-4bit/pixel textures to be correctly positioned</summary>
		public readonly int correction_ratio()
		{
			return (data & IS_NOT_PALETTED) != 0 ? 4 : (data & HAS_256_COLORS_PALETTE) != 0 ? 2 : 1;
		}

		static TextureHeaderFlagsPSX IReadable<TextureHeaderFlagsPSX>.Parse(WadReader reader)
		{
			var data = reader.Read<ushort>();
			Utils.Assert((data & 0xFE00) == 0);
			return new(data);
		}

		readonly void IWritable.Write(WadWriter writer)
		{
			writer.Write<ushort>(data);
		}
	}

	public sealed class TextureHeaderPSX:IReadable<TextureHeaderPSX>,IWritable
	{
		//Bottom Right
		public byte BR_x;
		public byte BR_y;

		public ushort TClut;//Texture/Color Lookup Table?

		//Bottom Left
		public byte BL_x;
		public byte BL_y;

		public TextureHeaderFlagsPSX TPage;//Texture Page

		//Top Right
		public byte TR_x;
		public byte TR_y;
		//Top Left
		public byte TL_x;
		public byte TL_y;

		static TextureHeaderPSX IReadable<TextureHeaderPSX>.Parse(WadReader reader)
		{
			return new()
			{
				BR_x = reader.Read<byte>(),
				BR_y = reader.Read<byte>(),
				TClut = reader.Read<ushort>(),
				BL_x = reader.Read<byte>(),
				BL_y = reader.Read<byte>(),
				TPage = reader.Read<TextureHeaderFlagsPSX>(),
				TR_x = reader.Read<byte>(),
				TR_y = reader.Read<byte>(),
				TL_x = reader.Read<byte>(),
				TL_y = reader.Read<byte>(),
			};
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<byte>(BR_x);
			writer.Write<byte>(BR_y);
			writer.Write<ushort>(TClut);
			writer.Write<byte>(BL_x);
			writer.Write<byte>(BL_y);
			writer.Write<TextureHeaderFlagsPSX>(TPage);
			writer.Write<byte>(TR_x);
			writer.Write<byte>(TR_y);
			writer.Write<byte>(TL_x);
			writer.Write<byte>(TL_y);
		}
	}
}