using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.FONT
{
	public sealed class FONTChunkInfo:BaseWADChunkInfo
	{
		public static readonly FONTChunkInfo Instance = new FONTChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "Font lookup table";
		public override ChunkType ChunkType => ChunkType.ID_PC_FONT;

		public override BaseWadChunk Parse(WadReader reader)
		{
			var fontLookup = reader.ReadArray<FontStruct>(256);
			reader.AssertEndOfChunk(ChunkType);
			return new FONTChunk(this, fontLookup, reader.GetAllWadData());
		}
	}
	public sealed class FONTChunk:BaseWadChunk
	{
		public IReadOnlyList<FontStruct> FontLookup{get;}

		public FONTChunk(BaseWADChunkInfo info, IReadOnlyList<FontStruct> fontLookup, byte[] data = null):base(info, data)
		{
			FontLookup = fontLookup;
		}
	}
}
