using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	public sealed class DoorChunkOSE:ChunkOSE
	{
		public override ChunkTypeOSE ChunkType => ChunkTypeOSE.Doors;

		public IReadOnlyList<DoorOSE> Doors{get;}

		public DoorChunkOSE(IReadOnlyList<DoorOSE> doors)
		{
			Doors = doors;
		}

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write(Doors.Count);
			writer.WriteArray(Doors);
		}
	}
}
