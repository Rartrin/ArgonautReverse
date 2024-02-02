using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.OpenStratEngine
{
    public sealed class WaypointOSE:IWritable
	{
		//public WaypointPSX Next{get;private set;}
		public int NextRawValue;

		//public WaypointPSX Prev{get;private set;}
		public int PrevRawValue;

		public RotPos3I Position;
		public int Value;

		public void Write(WadWriter writer)
		{
			writer.Write<int>(NextRawValue);
			writer.Write<int>(PrevRawValue);
			writer.Write<RotPos3I>(Position);//Croc 2 difference
			writer.Write<int>(Value);
		}
	}
}
