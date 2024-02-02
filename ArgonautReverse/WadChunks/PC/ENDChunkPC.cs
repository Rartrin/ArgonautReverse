using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PC
{
    public sealed class ENDChunkPCInfo:BaseWADChunkInfo<ENDChunkPC>
	{
		public static ENDChunkPCInfo Instance{get;} = new ENDChunkPCInfo();

		public override ChunkType ChunkType => ChunkType.ID_END;
		public override string ChunkDescription => "END";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.PC_PARSABLE_WADS;

		public override ENDChunkPC Parse(WadReader data_in)
		{
			data_in.AssertEndOfChunk(ChunkType);
			return new ENDChunkPC();
		}
	}
	public sealed class ENDChunkPC:BaseWadChunk
	{
		public ENDChunkPC():base(ENDChunkPCInfo.Instance){}
		
		public override void Serialize(WadWriter data_out)
		{
			var start = base.SerializeHeader(data_out);
			SerializeChunkSize(data_out, start);
		}
	}
}