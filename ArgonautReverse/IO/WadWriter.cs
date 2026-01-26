using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public class WadWriter:StreamWriter
	{
		public readonly Configuration Configuration;
		public readonly DatVersion DatVersion;
		public WadVersion WriteVersion{get;set;}

		public WadWriter(Configuration configuration, MemoryStream stream):base(stream, 0)
		{
			DatVersion = configuration.WriteVersion;
		}

		protected WadWriter(Configuration configuration, MemoryStream stream, int offset):base(stream, offset)
		{
			DatVersion = configuration.WriteVersion;
		}

		public ChunkWriter GetChunkWriter() => new ChunkWriter(Configuration, WriteVersion, Stream, Position);
	}
}