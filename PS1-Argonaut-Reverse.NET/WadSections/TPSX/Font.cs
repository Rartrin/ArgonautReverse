namespace ArgonautReverse.WadSections.TPSX
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

		public static Font Parse(Parser parser)
		{
			return new Font
			(
				texture:parser.ReadUInt16(),
				baseLine:parser.ReadUInt16(),
				width:parser.ReadUInt16(),
				height:parser.ReadUInt16()
			);
		}
	}
}
