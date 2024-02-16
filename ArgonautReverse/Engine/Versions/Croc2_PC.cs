using ArgonautReverse.Engine.Mappings;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine.Versions
{
    public static class Croc2_PC
	{
		public static DatVersion DatVersion => Croc2_PC_Dat.Instance;
		public static WadVersion WadVersion => Croc2_PC_Wad.Instance;

		private sealed class Croc2_PC_Dat:DatVersionPC
		{
			public static readonly DatVersion Instance = new Croc2_PC_Dat();

			public override string Title => "Croc 2 PC";

			public override WadVersion WadVersion => Croc2_PC.WadVersion;
		}

		private sealed class Croc2_PC_Wad:WadVersion
		{
			public static readonly WadVersion Instance = new Croc2_PC_Wad();

			public override DateTime BuildDate => new DateTime(1999, 6, 30);
			
			public override bool NEW_COLLISION => false;
			public override bool KEYFRAME_STUFF => false;

			public override InstructionOpcode MapOpcode(int value) => MapperCroc2.OpcodeMapper(value);
		}
	}
}
