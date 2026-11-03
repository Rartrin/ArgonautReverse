using ArgonautReverse.Engine.Mappings;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine.Versions
{
	public static class Aladdin_PC
	{
		public static readonly DatVersion DatVersion = new Aladdin_PC_Dat();
		public static readonly WadVersion WadVersion = new Aladdin_PC_Wad(DatVersion);

		private sealed class Aladdin_PC_Dat:DatVersionPC
		{
			public override string Title => "Aladdin PC";

			public override WadVersion WadVersion => Aladdin_PC.WadVersion;
		}

		private sealed class Aladdin_PC_Wad(DatVersion datVersion):WadVersion(datVersion)
		{
			public override DateTime BuildDate => new DateTime(2000, 1, 1);//TODO: Find actual build date.
			
			public override bool NEW_COLLISION => true;
			public override bool KEYFRAME_STUFF => false;//TODO: This?
			public override bool HAS_SPLINE_POINTS => true;
			public override bool HAS_STRAT_ARRAY_POOL => true;
			public override bool AssertReadWarns => true;

			public override InstructionOpcode MapOpcode(int value) => MapperCroc2.OpcodeMapper(value);
			public override TriggerType MapTriggerType(int value) => MapperCroc2.TriggerTypeMapper(value);
		}
	}
}