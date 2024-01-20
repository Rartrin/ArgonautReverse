using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.INFO
{
	public sealed class INFOChunkInfo:BaseWADChunkInfo
	{
		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "Debug chunk info (empty)";
		public override ChunkType ChunkType => ChunkType.ID_PC_INFO;

		public override BaseWadChunk Parse(WadReader data_in)
		{
			throw new NotImplementedException();
		}
	}
}
