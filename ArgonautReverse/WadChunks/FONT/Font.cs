using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.FONT
{
	public readonly struct FontStruct:IReadable<FontStruct>
	{
		public readonly ushort TextureId;
		public readonly short CharOffsetY;
		public readonly ushort CharWidth;
		public readonly short CharHeight;

		public FontStruct(UInt16 textureId, Int16 charOffsetY, UInt16 charWidth, Int16 charHeight)
		{
			TextureId = textureId;
			CharOffsetY = charOffsetY;
			CharWidth = charWidth;
			CharHeight = charHeight;
		}

		public static FontStruct Parse(WadReader reader)
		{
			var textureId = reader.Read<ushort>();
			var charOffsetY = (short)(reader.Read<ushort>() / 2);
			var charWidth = (ushort)(reader.Read<ushort>() / 2);
			var charHeight = (short)(reader.Read<ushort>() / 2);
			return new FontStruct(textureId, charOffsetY, charWidth, charHeight);
		}
	}
}
