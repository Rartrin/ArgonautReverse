using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	public sealed class WaypointChunkOSE:ChunkOSE
	{
		public override ChunkTypeOSE ChunkType => ChunkTypeOSE.Waypoints;

		public IReadOnlyList<WaypointOSE> Waypoints{get;}

		public WaypointChunkOSE(IReadOnlyList<WaypointOSE> waypoints)
		{
			Waypoints = waypoints;
		}

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write(Waypoints.Count);
			writer.WriteArray(Waypoints);
		}
	}
}
