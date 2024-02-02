using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public class WadWriter:StreamWriter
	{
		public readonly Configuration Configuration;
		public readonly DatVersion DatVersion;
		public WadVersion WriteVersion{get;set;}

		public WadWriter(Configuration configuration, Stream stream, bool handleStreamDisposal):base(stream, 0, handleStreamDisposal)
		{
			DatVersion = configuration.WriteVersion;
		}

		protected WadWriter(Configuration configuration, Stream stream, int offset):base(stream, offset, false)
		{
			DatVersion = configuration.WriteVersion;
		}

		public ChunkWriter GetChunkWriter()
		{
			return new ChunkWriter(Configuration, WriteVersion, Stream, Position);
		}
	}
}
