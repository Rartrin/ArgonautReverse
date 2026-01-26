using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class TRAKChunkInfo:BaseWADChunkInfo<TRAKChunk>
	{
		public static readonly TRAKChunkInfo Instance = new TRAKChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "Track data";
		public override ChunkType ChunkType => ChunkType.ID_PC_TRACK;

		public override TRAKChunk Parse(WadReader reader)
		{
			var modelCount = reader.Read<int>();

			var models = reader.ReadArrayMultipass<StratObjectPC>(modelCount);

			reader.AssertEndOfChunk(ChunkType);
			return new TRAKChunk(this, models, reader.GetAllWadData());
		}

		private TRAKChunkInfo(){}
	}
	public sealed class TRAKChunk(BaseWADChunkInfo info, IReadOnlyList<StratObjectPC> models, byte[]? data = null):BaseWadChunk(info, data)
	{
		public readonly IReadOnlyList<StratObjectPC> Models = models;

		protected override void WriteData(WadWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}