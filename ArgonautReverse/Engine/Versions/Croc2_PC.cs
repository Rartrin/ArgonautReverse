using ArgonautReverse.Engine.Mappings;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine.Versions
{
	public static class Croc2_PC
	{
		public static readonly DatVersion DatVersion = new Croc2_PC_Dat();
		public static readonly WadVersion WadVersion = new Croc2_PC_Wad(DatVersion);

		private sealed class Croc2_PC_Dat:DatVersionPC
		{
			public override string Title => "Croc 2 PC";

			public override WadVersion WadVersion => Croc2_PC.WadVersion;
		}

		private sealed class Croc2_PC_Wad(DatVersion datVersion):WadVersion(datVersion)
		{
			public override DateTime BuildDate => new DateTime(1999, 6, 30);
			
			public override bool NEW_COLLISION => false;
			public override bool KEYFRAME_STUFF => false;
			public override bool HAS_SPLINE_POINTS => false;
			public override bool HAS_STRAT_ARRAY_POOL => false;

			public override InstructionOpcode MapOpcode(int value) => MapperCroc2.OpcodeMapper(value);
			public override TriggerType MapTriggerType(int value) => MapperCroc2.TriggerTypeMapper(value);
		}
	}
}