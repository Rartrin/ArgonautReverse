using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine
{
	public sealed class FontOSE(ushort textureId, ushort charOffsetY, ushort charWidth, ushort charHeight):IWritable
	{
		public readonly ushort TextureId = textureId;
		public readonly ushort CharOffsetY = charOffsetY;
		public readonly ushort CharWidth = charWidth;
		public readonly ushort CharHeight = charHeight;

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