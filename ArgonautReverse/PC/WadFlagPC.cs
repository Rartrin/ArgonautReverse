namespace ArgonautReverse.PC
{
	[Flags]
	public enum WadFlagPC:uint
	{
		Map = 1 << 0,
		Background = 1 << 1,
		WAD_FLAG_4 = 1 << 2,
		MapPrelit = 1 << 3,
		ParticleSize = 1 << 4,
		WAD_FLAG_20 = 1 << 5,
		WAD_FLAG_40 = 1 << 6,
		HasLanguages = 1 << 7,
		HasCutscenes = 1 << 8,
		WAD_FLAG_200 = 1 << 9,
		WAD_FLAG_400 = 1 << 10,
		WAD_FLAG_800 = 1 << 11,
		WAD_FLAG_1000 = 1 << 12,
		WAD_FLAG_2000 = 1 << 13,
		WAD_FLAG_4000 = 1 << 14,
		WAD_FLAG_8000 = 1 << 15,
		HasOtherPieces = 1 << 16,
		WAD_FLAG_20000 = 1 << 17,
		WAD_FLAG_40000 = 1 << 18,
		WAD_FLAG_80000 = 1 << 19,
		WAD_FLAG_100000 = 1 << 20,//Used, still unknown
		CameraPoints = 1 << 21,//Used, still unknown
		WAD_FLAG_400000 = 1 << 22,
		WAD_FLAG_800000 = 1 << 23,
		WAD_FLAG_1000000 = 1 << 24,
		WAD_FLAG_2000000 = 1 << 25,
		WAD_FLAG_4000000 = 1 << 26,//Used, still unknown
		WAD_FLAG_8000000 = 1 << 27,
		WAD_FLAG_10000000 = 1 << 28,
		WAD_FLAG_20000000 = 1 << 29,
		WAD_FLAG_40000000 = 1 << 30,
		WAD_FLAG_80000000 = 1u << 31,
	};
}