using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine
{
	public sealed class FontOSE:IWritable
	{
		public readonly ushort TextureId;
		public readonly ushort CharOffsetY;
		public readonly ushort CharWidth;
		public readonly ushort CharHeight;

		public FontOSE(ushort textureId, ushort charOffsetY, ushort charWidth, ushort charHeight)
		{
			TextureId = textureId;
			CharOffsetY = charOffsetY;
			CharWidth = charWidth;
			CharHeight = charHeight;
		}

		public void Write(WadWriter writer)
		{
			writer.Write<ushort>(TextureId);

			//Croc 2 difference
			writer.Write<ushort>(CharOffsetY);
			writer.Write<ushort>(CharWidth);
			writer.Write<ushort>(CharHeight);
		}
	}
}
