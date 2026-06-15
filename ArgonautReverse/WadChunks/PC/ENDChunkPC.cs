using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class ENDChunkPCInfo:BaseWADChunkInfo<ENDChunkPC>
	{
		public static readonly ENDChunkPCInfo Instance = new ENDChunkPCInfo();

		public override ChunkType ChunkType => ChunkType.ID_END;
		public override string ChunkDescription => "END";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.ParsableWadsPC;

		public override ENDChunkPC Parse(WadReader reader)
		{
			reader.AssertEndOfChunk(ChunkType);
			return new ENDChunkPC();
		}
	}
	public sealed class ENDChunkPC():BaseWadChunk(ENDChunkPCInfo.Instance)
	{
		protected override void WriteData(ChunkWriter writer){}
	}
}