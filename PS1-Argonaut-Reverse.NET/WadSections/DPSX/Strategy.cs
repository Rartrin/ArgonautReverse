using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadSections.DPSX
{
	[Flags]
	public enum CollisionType:uint
	{
		//Also used in Strategy flags 1
		SIMPLEWALL	= (1 << 13),
		ALWAYS		= (1 << 14),
		FLOOR		= (1 << 15),
		PLAYER		= (1 << 16),
		OBJECT		= (1 << 17),
		WALL		= (1 << 18),
		PUSHABLE	= (1 << 19),
		REBOUND		= (1 << 20),
		CEILING		= (1 << 21),
	}
	public sealed class MapStrategy
	{
		public const int ByteSize = POS.ByteSize + 40;//64

		public POS Pos;
		
		public int AddrOffset;
		public UIntPtr Addr;

		//public uint NumberParameters;
		public IReadOnlyList<int> ParamBlock;
		public uint NumberLocals;
		public uint NumberTriggers;
		public CollisionType Collision;
		public uint CollisionBones;
		public Waypoint FirstWP;
		public Waypoint LastWP;
		public ushort? Flag;
		public ushort? Wait;

		#if STRAT_ARRAY_POOL
		Uint32	ArrayPoolSize;	// how many Sin32 does this strat have in the array pool
		Uint32	ArrayPool;		// err...... and where is the base address of this array
		#endif

		private MapStrategy(){}

		public static MapStrategy Parse(WadReader reader, Map map)
		{
			var mapStrat = new MapStrategy();

			mapStrat.Pos = reader.Read<POS>();

			//TODO: Setup Addr
			mapStrat.AddrOffset = reader.Read<int>();
			
			var numberParameters = reader.Read<uint>();

			var paramBlockOffset = reader.Read<int>();

			//NumberParameters is 0 IFF ParamBlockOffset is -1
			Utils.Assert((numberParameters==0) == (paramBlockOffset==-1));

			if(paramBlockOffset == -1)
			{
				mapStrat.ParamBlock = null;
			}
			else
			{
				mapStrat.ParamBlock = map.Params.Skip(paramBlockOffset/sizeof(int)).Take((int)numberParameters).ToArray();
			}

			mapStrat.NumberLocals = reader.Read<uint>();
			mapStrat.NumberTriggers = reader.Read<uint>();
			mapStrat.Collision = (CollisionType)reader.Read<uint>();
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
				Utils.Assert((firstWaypointOffset%Waypoint.ByteSize)==0);
				mapStrat.FirstWP = map.WPList[firstWaypointOffset/Waypoint.ByteSize];
				
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
