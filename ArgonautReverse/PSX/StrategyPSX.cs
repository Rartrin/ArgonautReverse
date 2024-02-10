using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	[Flags]
	public enum CollisionTypePSX:uint
	{
		//Also used in Strategy flags 1
		SIMPLEWALL = 1 << 13,
		ALWAYS = 1 << 14,
		FLOOR = 1 << 15,
		PLAYER = 1 << 16,
		OBJECT = 1 << 17,
		WALL = 1 << 18,
		PUSHABLE = 1 << 19,
		REBOUND = 1 << 20,
		CEILING = 1 << 21,
	}
	public sealed class MapStrategyPSX
	{
		public const int ByteSize = POS.ByteSize + 40;//64

		public POS Pos;

		public int AddrOffset;
		public ActorDataPSX Script;

		//public uint NumberParameters;
		public IReadOnlyList<int> ParamBlock;
		public uint NumberLocals;
		public uint NumberTriggers;
		public CollisionTypePSX Collision;
		public uint CollisionBones;
		public WaypointPSX FirstWP;
		public WaypointPSX LastWP;
		public ushort? Flag;
		public ushort? Wait;

#if STRAT_ARRAY_POOL
		Uint32	ArrayPoolSize;	// how many Sin32 does this strat have in the array pool
		Uint32	ArrayPool;		// err...... and where is the base address of this array
#endif

		private MapStrategyPSX() { }

		public static MapStrategyPSX Parse(WadReader reader, MapPSX map)
		{
			var mapStrat = new MapStrategyPSX();

			mapStrat.Pos = reader.Read<POS>();

			//TODO: Setup Addr
			//This offset is from the start of the data chunk
			mapStrat.AddrOffset = reader.Read<int>();

			var numberParameters = reader.Read<uint>();

			var paramBlockOffset = reader.Read<int>();

			//NumberParameters is 0 IFF ParamBlockOffset is -1
			Utils.Assert(numberParameters == 0 == (paramBlockOffset == -1));

			if(paramBlockOffset == -1)
			{
				mapStrat.ParamBlock = null;
			}
			else
			{
				mapStrat.ParamBlock = map.Params.Skip(paramBlockOffset / sizeof(int)).Take((int)numberParameters).ToArray();
			}

			mapStrat.NumberLocals = reader.Read<uint>();
			mapStrat.NumberTriggers = reader.Read<uint>();
			mapStrat.Collision = (CollisionTypePSX)reader.Read<uint>();
			mapStrat.CollisionBones = reader.Read<uint>();

			var firstWaypointOffset = reader.Read<int>();
			var lastWaypointValue = reader.Read<int>();
			if(lastWaypointValue != 0)
			{
				throw new Exception();
			}

			if(firstWaypointOffset == -1)
			{
				mapStrat.FirstWP = null;
			}
			else
			{
				Utils.Assert(firstWaypointOffset % WaypointPSX.ByteSize == 0);
				mapStrat.FirstWP = map.WPList[firstWaypointOffset / WaypointPSX.ByteSize];

				var cur = mapStrat.FirstWP;
				while(cur.Next != null)
				{
					cur = cur.Next;
				}
				mapStrat.LastWP = cur;
			}

			//Not entirely sure that these are the fields missing but it would match up
			if(reader.ReadVersion.IsNewerOrSame(CROC_2_DEMO_PS1_DUMMY.WadVersion_Latest))
			{
				mapStrat.Flag = reader.Read<ushort>();
				mapStrat.Wait = reader.Read<ushort>();
			}
			else
			{
				mapStrat.Flag = null;
				mapStrat.Wait = null;
			}

			return mapStrat;
		}
	}
}
