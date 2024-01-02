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

		public static Font Parse(BinaryReader reader)
		{
			return new Font
			(
				texture:reader.ReadUInt16(),
				baseLine:reader.ReadUInt16(),
				width:reader.ReadUInt16(),
				height:reader.ReadUInt16()
			);
		}
	}
}
