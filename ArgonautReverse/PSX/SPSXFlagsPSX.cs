namespace ArgonautReverse.PSX
{
	[Flags]
	public enum SPSXFlagsPSX:uint
	{
		HasAmbient = 1 << 0,
		AmbientSeparate = 1 << 1,
		HasStreams = 1 << 2,
		HasLevelSfx = 1 << 3,//Extra data in END chunk. Only found in Harry Potter.
		UNKNOWN1 = 1 << 4,//TODO: Always mimics HasAmbient in Harry Potter? Unknown if it is used.
	}
}