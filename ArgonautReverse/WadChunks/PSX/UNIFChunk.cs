using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PSX
{
	public sealed class UNIFChunkInfo:BaseWADChunkInfo<UNIFChunk>
	{
		public static readonly UNIFChunkInfo Instance = new UNIFChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PSX_UNIF;
		public override WadVersion[] SupportedWadVersions{get;} = [HARRY_POTTER_1_PS1.WadVersion, HARRY_POTTER_2_PS1.WadVersion];
		public override string ChunkDescription => "Language";

		public override UNIFChunk Parse(WadReader reader)
		{
			//reader.AssertEndOfChunk(ChunkType);
			return new(reader.GetAllWadData());
		}
	}
	public sealed class UNIFChunk(byte[]? data = null):BaseWadChunk(UNIFChunkInfo.Instance, data)
	{
		protected override void WriteData(ChunkWriter writer)
		{
			writer.WriteData(Data);
		}
	}
}