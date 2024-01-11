namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class MapStrategy
	{
		public POS Pos;
		public UIntPtr Addr;
		public uint NumberParameters;
		public int[] ParamBlock;
		public uint NumberLocals;
		public uint NumberTriggers;
		public uint Collision;
		public uint CollisionBones;
		public Waypoint FirstWP;
		public Waypoint LastWP;
		public ushort Flag;
		public ushort Wait;
		#if STRAT_ARRAY_POOL
			Uint32	ArrayPoolSize;					// how many Sin32 does this strat have in the array pool
			Uint32	ArrayPool;						// err...... and where is the base address of this array
		#endif	
	}
}
