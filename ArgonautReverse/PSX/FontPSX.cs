using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public sealed class FontPSX
	{
		public ushort Texture{get;}
		public ushort BaseLine{get;}
		public ushort Width{get;}
		public ushort Height{get;}

		private FontPSX(ushort texture, ushort baseLine, ushort width, ushort height)
		{
			Texture = texture;
			BaseLine = baseLine;
			Width = width;
			Height = height;
		}

		public static FontPSX Parse(WadReader parser)
		{
			var texture = parser.Read<ushort>();
			var baseLine = parser.Read<ushort>();
			var width = parser.Read<ushort>();
			var height = parser.Read<ushort>();
			return new FontPSX(texture, baseLine, width, height);
		}
	}
}
