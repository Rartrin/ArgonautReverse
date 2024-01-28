using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public readonly struct FontStructPC:IReadable<FontStructPC>
	{
		public readonly ushort TextureId;
		public readonly short CharOffsetY;
		public readonly ushort CharWidth;
		public readonly short CharHeight;

		public FontStructPC(ushort textureId, short charOffsetY, ushort charWidth, short charHeight)
		{
			TextureId = textureId;
			CharOffsetY = charOffsetY;
			CharWidth = charWidth;
			CharHeight = charHeight;
		}

		public static FontStructPC Parse(WadReader reader)
		{
			var textureId = reader.Read<ushort>();
			var charOffsetY = (short)(reader.Read<ushort>() / 2);
			var charWidth = (ushort)(reader.Read<ushort>() / 2);
			var charHeight = (short)(reader.Read<ushort>() / 2);
			return new FontStructPC(textureId, charOffsetY, charWidth, charHeight);
		}
	}
}
