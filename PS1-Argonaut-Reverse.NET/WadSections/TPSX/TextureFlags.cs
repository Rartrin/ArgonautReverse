namespace ArgonautReverse.WadSections.TPSX
{
	public enum TextureFlags
	{
		HAS_256_COLORS_PALETTE = 0x80,
		IS_NOT_PALETTED = 0x100,
	}

	public static class TextureFlagExtensions
	{
		public static int n_row(this TextureFlags that) => (((int)that & 4) >> 1) + (((int)that & 16) >> 4);

		public static int n_column(this TextureFlags that) => (int)that & 3;

		///<summary>Correction Ratio, needed for non-4bit/pixel textures to be correctly positioned</summary>
		public static int correction_ratio(this TextureFlags that)
		{
			return ((that&TextureFlags.IS_NOT_PALETTED)!=0) ? 4 : (((that&TextureFlags.HAS_256_COLORS_PALETTE)!=0) ? 2 : 1);
		}
	}
}