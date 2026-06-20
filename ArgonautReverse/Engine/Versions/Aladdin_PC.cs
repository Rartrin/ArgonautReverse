using ArgonautReverse.Engine.Mappings;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine.Versions
{
	public static class Aladdin_PC
	{
		public static DatVersion DatVersion => Aladdin_PC_Dat.Instance;
		public static WadVersion WadVersion => Aladdin_PC_Wad.Instance;

		private sealed class Aladdin_PC_Dat:DatVersionPC
		{
			public static readonly DatVersion Instance = new Aladdin_PC_Dat();

			public override string Title => "Aladdin PC";

			public override WadVersion WadVersion => Aladdin_PC.WadVersion;
		}

		private sealed class Aladdin_PC_Wad:WadVersion
		{
			public static readonly WadVersion Instance = new Aladdin_PC_Wad();

			public override DateTime BuildDate => new DateTime(2000, 1, 1);//TODO: Find actual build date.
			
			public override bool NEW_COLLISION => true;
			public override bool KEYFRAME_STUFF => false;//TODO: This?
            public override bool HAS_SPLINE_POINTS => true;
            public override bool HAS_STRAT_ARRAY_POOL => true;

			public override InstructionOpcode MapOpcode(int value) => MapperCroc2.OpcodeMapper(value);
			public override TriggerType MapTriggerType(int value) => MapperCroc2.TriggerTypeMapper(value);
		}
	}
}