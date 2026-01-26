using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class INFOChunkInfo:BaseWADChunkInfo<INFOChunk>
	{
		public static readonly INFOChunkInfo Instance = new INFOChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "Debug chunk info (empty)";
		public override ChunkType ChunkType => ChunkType.ID_PC_INFO;

		public override INFOChunk Parse(WadReader reader)
		{
			if(reader.Remaining != 4)
			{
				throw new Exception("INFO chunk is not an int32");
			}
			var value = reader.Read<int>();
			if(value != 1)
			{
				throw new Exception("INFO chunk value is not 1");
			}
			return new INFOChunk(this, reader.GetAllWadData());
		}
	}
	public sealed class INFOChunk(BaseWADChunkInfo info, byte[]? data = null):BaseWadChunk(info, data)
	{
		protected override void WriteData(WadWriter writer) => writer.WriteBytes(Data!);
	}
}
