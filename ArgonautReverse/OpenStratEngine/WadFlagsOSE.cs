namespace ArgonautReverse.OpenStratEngine
{
	[Flags]
	public enum WadFlagsOSE:uint
	{
		Background = 1<<1,

		ParticleSize = 1<<4,

		HasOtherPieces = 1<<16,
	}
}
