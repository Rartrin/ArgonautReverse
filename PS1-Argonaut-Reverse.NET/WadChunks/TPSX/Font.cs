using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.TPSX
{
	public sealed class Font
	{
		public ushort Texture{get;}
		public ushort BaseLine{get;}
		public ushort Width{get;}
		public ushort Height{get;}

		private Font(ushort texture, ushort baseLine, ushort width, ushort height)
		{
			Texture = texture;
			BaseLine = baseLine;
			Width = width;
			Height = height;
		}

		public static Font Parse(WadReader parser)
		{
			var texture = parser.Read<ushort>();
			var baseLine = parser.Read<ushort>();
			var width = parser.Read<ushort>();
			var height = parser.Read<ushort>();
			return new Font(texture, baseLine, width, height);
		}
	}
}
