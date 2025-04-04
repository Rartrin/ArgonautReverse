using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
    public sealed class WFPCChunkInfo:BaseWADChunkInfo<WFPCChunk>
	{
		public static readonly WFPCChunkInfo Instance = new WFPCChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "WadFlags";
		public override ChunkType ChunkType => ChunkType.ID_PC_WADFLAGS;

		private WFPCChunkInfo(){}

		public override BaseWadChunk Parse(WadReader reader)
		{
			var wadFlags = (WadFlagPC)reader.Read<uint>();
			reader.AssertEndOfChunk(ChunkType);
			return new WFPCChunk(this, wadFlags);
		}

	}
	public sealed class WFPCChunk(BaseWADChunkInfo info, WadFlagPC wadFlags):BaseWadChunk(info)
	{
		public WadFlagPC WadFlags{get;} = wadFlags;
	}
}