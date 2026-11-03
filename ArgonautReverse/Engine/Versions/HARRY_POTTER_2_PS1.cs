using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine.Versions
{
	public static class HARRY_POTTER_2_PS1
	{
		public static readonly DatVersion DatVersion = new HARRY_POTTER_2_PS1_Dat();
		public static readonly WadVersion WadVersion = new HARRY_POTTER_2_PS1_Wad(DatVersion);
		public static DirFormat DirFormat => CROC_2_PS1.DirFormat;

		private sealed class HARRY_POTTER_2_PS1_Dat:DatVersionPSX
		{
			public override string Title => "Harry Potter 2 PS1";
			public override string FilenameDAT => "POTTER.DAT";
			public override string FilenameDIR => "POTTER.DIR";
			public override DirFormat DirFormat => HARRY_POTTER_2_PS1.DirFormat;

			public override WadVersion GetWadVersion(string? wadName) => WadVersion;

			public override IReadOnlyList<WadVersion> WadVersions{get;} = [WadVersion];
		}

		private sealed class HARRY_POTTER_2_PS1_Wad(DatVersion datVersion):WadVersion(datVersion)
		{
			public override DateTime BuildDate => new DateTime(2002, 11, 5);
			
			public override bool NEW_COLLISION => true;
			public override bool KEYFRAME_STUFF => true;
			public override bool HAS_SPLINE_POINTS => throw new NotImplementedException();
			public override bool HAS_STRAT_ARRAY_POOL => throw new NotImplementedException();

			public override InstructionOpcode MapOpcode(int value) => throw new NotImplementedException();
			public override TriggerType MapTriggerType(int value) => throw new NotImplementedException();
		}
	}
}