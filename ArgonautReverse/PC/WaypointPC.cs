using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PC
{
	public sealed class WaypointPC:IReadable<WaypointPC>,IWritable
	{
		//union
		public WaypointPC? Next;
		public int NextRawValue;

		//union
		public WaypointPC? Prev;
		public int PrevRawValue;

		public RotPos3I Pos;

		public uint LinkFlag => (Prev==null ? 2u : 0u) | (Next==null ? 1u : 0u);
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

		public void Write(WadWriter writer)
		{
			writer.Write<int>(NextRawValue);
			if(PrevRawValue != 0){throw new Exception();}
			writer.Write<int>(PrevRawValue);
			writer.Write<Vector3<ushort>>(new((ushort)Pos.Rotation.X, (ushort)Pos.Rotation.Y, (ushort)Pos.Rotation.Z));
			writer.Write<Vector3I>(Pos.Position);
			writer.Write<uint>(Value);
		}

		public static IReadOnlyList<WaypointPC> ParseWaypoints(WadReader reader)
		{
			var count = reader.Read<int>();
			var waypoints = reader.ReadArray<WaypointPC>(count);

			for(int i = 0; i < count; i++)
			{
				var waypoint = waypoints[i];
				if(waypoint.NextRawValue == 0)
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

		public static void WriteWaypoints(WadWriter writer, IReadOnlyList<WaypointPC> waypoints)
		{
			writer.Write<int>(waypoints.Count);
			writer.WriteArray(waypoints);
			//TODO: Rebuild NextRawValue? May not be needed. PrevRawValue should always be 0.
			for(int i = 0; i < waypoints.Count; i++)
			{
				var waypoint = waypoints[i];
				if(waypoint.NextRawValue == 0 && waypoint.Next != null)
				{
					throw new Exception();
				}
			}
		}
	}
}
