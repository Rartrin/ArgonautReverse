using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
    public sealed class FONTChunkInfo : BaseWADChunkInfo
    {
        public static readonly FONTChunkInfo Instance = new FONTChunkInfo();

        public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
        public override string ChunkDescription => "Font lookup table";
        public override ChunkType ChunkType => ChunkType.ID_PC_FONT;

        public override BaseWadChunk Parse(WadReader reader)
        {
            var fontLookup = reader.ReadArray<FontStructPC>(256);
            reader.AssertEndOfChunk(ChunkType);
            return new FONTChunk(this, fontLookup, reader.GetAllWadData());
        }
    }
    public sealed class FONTChunk : BaseWadChunk
    {
        public IReadOnlyList<FontStructPC> FontLookup { get; }

        public FONTChunk(BaseWADChunkInfo info, IReadOnlyList<FontStructPC> fontLookup, byte[] data = null) : base(info, data)
        {
            FontLookup = fontLookup;
        }
    }
}
