using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
    public sealed class TRAKChunkInfo : BaseWADChunkInfo
    {
        public static TRAKChunkInfo Instance = new TRAKChunkInfo();

        public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
        public override string ChunkDescription => "Track data";
        public override ChunkType ChunkType => ChunkType.ID_PC_TRACK;

        public override BaseWadChunk Parse(WadReader reader)
        {
            var modelCount = reader.Read<int>();

            var models = reader.ReadArrayMultipass<StratObjectPC>(modelCount);

            reader.AssertEndOfChunk(ChunkType);
            return new TRAKChunk(this, models, reader.GetAllWadData());
        }

        private TRAKChunkInfo() { }
    }
    public sealed class TRAKChunk : BaseWadChunk
    {
        public IReadOnlyList<StratObjectPC> Models { get; }

        public TRAKChunk(BaseWADChunkInfo info, IReadOnlyList<StratObjectPC> models, byte[] data = null) : base(info, data)
        {
            Models = models;
        }
    }
}
