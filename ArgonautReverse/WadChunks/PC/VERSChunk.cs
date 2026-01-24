using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PC
{
    public sealed class VERSChunkInfo:BaseWADChunkInfo<VERSChunk>
	{
		public static readonly VERSChunkInfo Instance = new VERSChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "Debug chunk version (empty)";
		public override ChunkType ChunkType => ChunkType.ID_PC_VERSION;

		public override BaseWadChunk Parse(WadReader reader)
		{
			if(reader.Remaining != 4)
			{
				throw new Exception("VERS chunk is not an int32");
			}
			var value = reader.Read<int>();
			if(value != 1)
			{
				throw new Exception("VERS chunk value is not 1");
			}
			return new VERSChunk(this, reader.GetAllWadData());
		}
	}
	public sealed class VERSChunk:BaseWadChunk
	{
		public VERSChunk(BaseWADChunkInfo info, byte[]? data = null) : base(info, data) { }
	}
}
