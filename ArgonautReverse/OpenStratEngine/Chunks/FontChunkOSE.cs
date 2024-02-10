using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	public sealed class FontChunkOSE:ChunkOSE
	{
		public override ChunkTypeOSE ChunkType => ChunkTypeOSE.Fonts;

		public IReadOnlyList<FontOSE> Fonts{get;}

		public FontChunkOSE(IReadOnlyList<FontOSE> fonts)
		{
			Fonts = fonts;
		}

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write(Fonts.Count);
			writer.WriteArray(Fonts);
		}
	}
}