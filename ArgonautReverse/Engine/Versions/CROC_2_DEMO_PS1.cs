using ArgonautReverse.Engine.Mappings;
using ArgonautReverse.Universal;

namespace ArgonautReverse.Engine.Versions
{
	//Croc 2 PS1 US Demo
	public static class CROC_2_DEMO_PS1
	{
		public static DatVersion DatVersion => CROC_2_DEMO_PS1_Dat.Instance;
		public static WadVersion WadVersion => CROC_2_DEMO_PS1_Wad.Instance;
		public static DirFormat DirFormat => CROC_2_PS1.DirFormat;

		private sealed class CROC_2_DEMO_PS1_Dat:DatVersionPSX
		{
			public static readonly DatVersion Instance = new CROC_2_DEMO_PS1_Dat();

			public override string Title => "Croc 2 Demo PS1";
			public override string FilenameDAT => "CROCII.DAT";
			public override string FilenameDIR => "CROCII.DIR";
			public override DirFormat DirFormat => CROC_2_DEMO_PS1.DirFormat;

			public override WadVersion GetWadVersion(string wadName) => WadVersion;

			public override IReadOnlyList<WadVersion> WadVersions{get;} = new[]{WadVersion};
		}

		private sealed class CROC_2_DEMO_PS1_Wad:WadVersion
		{
			public static readonly WadVersion Instance = new CROC_2_DEMO_PS1_Wad();

			public override DateTime BuildDate => new DateTime(1999, 3, 4);
			
			public override bool NEW_COLLISION => false;
			public override bool KEYFRAME_STUFF => false;

			public override InstructionOpcode MapOpcode(int value) => MapperCroc2.OpcodeMapper(value);
		}
	}
}