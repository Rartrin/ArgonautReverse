namespace ArgonautReverse.PC
{
	//These are also part of Strategy Flags 0
	public enum CollisionTypePC
	{
		ALWAYS = 1 << 14,
		FLOOR = 1 << 15,
		PLAYER = 1 << 16,
		OBJECT = 1 << 17,
		WALL = 1 << 18,
		PUSHABLE = 1 << 19,
		REBOUND = 1 << 20,
	}
}