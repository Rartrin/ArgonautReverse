using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class SMPCChunkInfo:BaseWADChunkInfo<SMPCChunk>
	{
		public static readonly SMPCChunkInfo Instance = new SMPCChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PC_SAMPLE;
		public override string ChunkDescription => "Audio Sample data";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.PC_PARSABLE_WADS;

		public override SMPCChunk Parse(WadReader reader)
		{
			var adsResourceCount = reader.Read<int>();
			var adsResources = reader.ReadArray<ADSResourceSMPC>(adsResourceCount);
			reader.AssertEndOfChunk(ChunkType);
			return new(adsResources);
		}
	}
	public sealed class SMPCChunk(ADSResourceSMPC[] adsResources):BaseWadChunk(SMPCChunkInfo.Instance)
	{
		public readonly ADSResourceSMPC[] Resources = adsResources;

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write<int>(Resources.Length);
			writer.WriteArray(Resources);
		}
	}
}