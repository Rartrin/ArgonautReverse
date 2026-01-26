using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PC
{
	public sealed class WaypointPC : IReadable<WaypointPC>
	{
		//union
		public WaypointPC Next;
		public int NextRawValue;

		//union
		public WaypointPC Prev;
		public int PrevRawValue;

		public RotPos3I Pos;

		public uint LinkFlag => (Prev==null?2u:0u) | (Next==null?1u:0u);
		public uint Value;

		public static WaypointPC Parse(WadReader reader)
		{
			var waypoint = new WaypointPC();
			waypoint.NextRawValue = reader.Read<int>();
			reader.AssertRead<uint>(0);//waypoint.PrevRawValue
			var rot = reader.Read<Vector3<ushort>>();
			waypoint.Pos.Rotation.X = rot.X;
			waypoint.Pos.Rotation.Y = rot.Y;
			waypoint.Pos.Rotation.Z = rot.Z;
			waypoint.Pos.Position = reader.Read<Vector3I>();
			waypoint.Value = reader.Read<uint>();
			return waypoint;
		}

		public static IReadOnlyList<WaypointPC> ParseWaypoints(WadReader reader)
		{
			var count = reader.Read<int>();
			var waypoints = reader.ReadArray<WaypointPC>(count);

			for (int i = 0; i < count; i++)
			{
				var waypoint = waypoints[i];
				if (waypoint.NextRawValue == 0)
				{
					waypoint.Next = null;
				}
				else
				{
					waypoint.Next = waypoints[i+1];
					waypoint.Next.Prev = waypoint;
				}
			}
			return waypoints;
		}
	}
}
