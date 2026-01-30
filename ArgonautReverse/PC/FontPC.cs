using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine;

namespace ArgonautReverse.PC
{
	public readonly record struct FontStructPC(ushort TextureId, short CharOffsetY, ushort CharWidth, ushort CharHeight):IReadable<FontStructPC>,IWritable,IConvertibleFromOSE<FontOSE,FontStructPC>
	{
		public static FontStructPC Parse(WadReader reader)
		{
			var textureId = reader.Read<ushort>();
			var charOffsetY = (short)(reader.Read<ushort>() / 2);
			var charWidth = (ushort)(reader.Read<ushort>() / 2);
			var charHeight = (ushort)(reader.Read<ushort>() / 2);
			return new(textureId, charOffsetY, charWidth, charHeight);
		}

		public static FontStructPC FromOSE(FontOSE ose) => new
		(
			TextureId: ose.TextureId,
			CharOffsetY: (short)ose.CharOffsetY,
			CharWidth: ose.CharWidth,
			CharHeight: ose.CharHeight
		);

		public void Write(WadWriter writer)
		{
			writer.Write<ushort>(TextureId);
			writer.Write<short>((short)(CharOffsetY * 2));
			writer.Write<ushort>((ushort)(CharWidth * 2));
			writer.Write<ushort>((ushort)(CharHeight * 2));
		}
	}
}
