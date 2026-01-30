using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	public sealed class WadFlagsChunkOSE:ChunkOSE
	{
		public override ChunkTypeOSE ChunkType => ChunkTypeOSE.WadFlags;

		public WadFlagsOSE WadFlags{get;}

		public WadFlagsChunkOSE(WadFlagsOSE wadFlags)
		{
			WadFlags = wadFlags;
		}

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write((uint)WadFlags);
		}
	}
}
