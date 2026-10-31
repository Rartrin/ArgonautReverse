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

		public uint ParameterCount;
		public int ParamBlockOffset;
		public IReadOnlyList<int> ParamBlock;
		public uint NumberLocals;
		public uint NumberTriggers;
		public CollisionTypePSX Collision;
		public uint CollisionBones;
		public int FirstWaypointOffset;
		//public int LastWaypointOffset;
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

			mapStrat.ParameterCount = reader.Read<uint>();

			mapStrat.ParamBlockOffset = reader.Read<int>();

			//ParameterCount is 0 IFF ParamBlockOffset is -1
			Utils.Assert((mapStrat.ParameterCount == 0) == (mapStrat.ParamBlockOffset == -1));

			if(mapStrat.ParamBlockOffset == -1)
			{
				mapStrat.ParamBlock = null;
			}
			else
			{
				mapStrat.ParamBlock = map.Params.Skip(mapStrat.ParamBlockOffset / sizeof(int)).Take((int)mapStrat.ParameterCount).ToArray();
			}

			mapStrat.NumberLocals = reader.Read<uint>();
			mapStrat.NumberTriggers = reader.Read<uint>();
			mapStrat.Collision = (CollisionTypePSX)reader.Read<uint>();
			mapStrat.CollisionBones = reader.Read<uint>();

			mapStrat.FirstWaypointOffset = reader.Read<int>();
			reader.AssertRead<int>(0);//LastWaypointOffset

			if(mapStrat.FirstWaypointOffset == -1)
			{
				mapStrat.FirstWP = null;
			}
			else
			{
				Utils.Assert(mapStrat.FirstWaypointOffset % WaypointPSX.ByteSize == 0);
				mapStrat.FirstWP = map.WPList[mapStrat.FirstWaypointOffset / WaypointPSX.ByteSize];

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

		public void Write(WadWriter writer)
		{
			writer.Write<POS>(Pos);

			//TODO: Setup Addr
			//This offset is from the start of the data chunk
			writer.Write<int>(AddrOffset);

			//ParameterCount is 0 IFF ParamBlockOffset is -1
			Utils.Assert((ParameterCount == 0) == (ParamBlockOffset == -1));

			writer.Write<uint>(ParameterCount);

			writer.Write<int>(ParamBlockOffset);

			writer.Write<uint>(NumberLocals);
			writer.Write<uint>(NumberTriggers);
			writer.Write<uint>((uint)Collision);
			writer.Write<uint>(CollisionBones);

			writer.Write<int>(FirstWaypointOffset);
			writer.Write<int>(0);//LastWaypointOffset

			//Not entirely sure that these are the fields missing but it would match up
			if(writer.WriteVersion.IsNewerOrSame(CROC_2_DEMO_PS1_DUMMY.WadVersion_Latest))
			{
				writer.Write<ushort>(Flag!.Value);
				writer.Write<ushort>(Wait!.Value);
			}
		}
	}
}