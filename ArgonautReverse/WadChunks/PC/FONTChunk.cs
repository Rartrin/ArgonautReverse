using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine;
using ArgonautReverse.OpenStratEngine.Chunks;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class FONTChunkInfo:BaseWADChunkInfo<FONTChunk>
	{
		public static readonly FONTChunkInfo Instance = new FONTChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.ParsableWadsPC;
		public override string ChunkDescription => "Font lookup table";
		public override ChunkType ChunkType => ChunkType.ID_PC_FONT;

		public override FONTChunk Parse(WadReader reader)
		{
			var fontLookup = reader.ReadArray<FontStructPC>(256);
			reader.AssertEndOfChunk(ChunkType);
			return new FONTChunk(this, fontLookup, reader.GetAllWadData());
		}
	}
	public sealed class FONTChunk(BaseWADChunkInfo info, IReadOnlyList<FontStructPC> fontLookup, byte[]? data = null):BaseWadChunk(info, data),IConvertibleFromOSE<FontChunkOSE,FONTChunk>
	{
		public IReadOnlyList<FontStructPC> FontLookup{get;} = fontLookup;

		protected override void WriteData(ChunkWriter writer)
		{
			writer.WriteArray(FontLookup);
		}

		public static FONTChunk FromOSE(FontChunkOSE ose) => new
		(
			FONTChunkInfo.Instance,
			ose.Fonts.FromOSE<FontOSE,FontStructPC>()
		);
	}
}