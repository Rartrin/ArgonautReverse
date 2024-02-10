using ArgonautReverse.Universal;

namespace ArgonautReverse.Engine.Mappings
{
	public static class MapperCroc2
	{
		public static InstructionOpcode OpcodeMapper(int value) => (InstructionOpcode)value;
	}
}
