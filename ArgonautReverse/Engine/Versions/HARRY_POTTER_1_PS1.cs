using ArgonautReverse.Universal;

namespace ArgonautReverse.Engine.Versions
{
	public static class HARRY_POTTER_1_PS1
	{
		public static DatVersion DatVersion => HARRY_POTTER_1_PS1_Dat.Instance;
		public static WadVersion WadVersion => HARRY_POTTER_1_PS1_Wad.Instance;
		public static DirFormat DirFormat => CROC_2_PS1.DirFormat;

		private sealed class HARRY_POTTER_1_PS1_Dat:DatVersionPSX
		{
			public static readonly DatVersion Instance = new HARRY_POTTER_1_PS1_Dat();

			public override string Title => "Harry Potter 1 PS1";
			public override string FilenameDAT => "POTTER.DAT";
			public override string FilenameDIR => "POTTER.DIR";
			public override DirFormat DirFormat => HARRY_POTTER_1_PS1.DirFormat;

			public override WadVersion GetWadVersion(string wadName) => WadVersion;

			public override IReadOnlyList<WadVersion> WadVersions{get;} = new[]{WadVersion};
		}

		private sealed class HARRY_POTTER_1_PS1_Wad:WadVersion
		{
			public static readonly WadVersion Instance = new HARRY_POTTER_1_PS1_Wad();

			public override DateTime BuildDate => new DateTime(2001, 12, 1);
			
			public override bool NEW_COLLISION => true;
			public override bool KEYFRAME_STUFF => true;

			public override InstructionOpcode MapOpcode(int value) => throw new NotImplementedException();
		}
	}
}