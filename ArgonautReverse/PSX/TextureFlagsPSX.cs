namespace ArgonautReverse.PSX
{
	public enum TextureFlagsPSX
	{
		HAS_256_COLORS_PALETTE = 0x80,
		IS_NOT_PALETTED = 0x100,
	}

	public static class TextureFlagsPSXExtensions
	{
		extension(TextureFlagsPSX that)
		{
			public int n_row() => (((int)that & 4) >> 1) + (((int)that & 16) >> 4);

			public int n_column() => (int)that & 3;

			///<summary>Correction Ratio, needed for non-4bit/pixel textures to be correctly positioned</summary>
			public int correction_ratio()
			{
				return (that & TextureFlagsPSX.IS_NOT_PALETTED) != 0 ? 4 : (that & TextureFlagsPSX.HAS_256_COLORS_PALETTE) != 0 ? 2 : 1;
			}
		}
	}
}