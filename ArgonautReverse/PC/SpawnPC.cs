namespace ArgonautReverse.PC
{
	public enum SpawnFlags : uint
	{
		SPAWN_FLAG_8000 = 0x8000,
		SPAWN_FLAG_40000 = 0x40000,
		SPAWN_FLAG_80000 = 0x80000,
	}

	public sealed class SpawnStructPC
	{
		public StratEntityPC Parent;
		public int count2;//Number of LocalVars to pull from the stack
		public int count1;//Number of Triggers
		public SpawnFlags flags;
		public int boneCount0;
		public int field5;
		public MapStratPC map;
	}

}
