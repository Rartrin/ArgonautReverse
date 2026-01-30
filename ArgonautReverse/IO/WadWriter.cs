using ArgonautReverse.Engine;
using ArgonautReverse.Files;

namespace ArgonautReverse.IO
{
	public class WadWriter(WADFile wadFile, Configuration configuration, WadVersion writeVersion, MemoryStream stream, int offset = 0):StreamWriter(stream, offset)
	{
		public readonly WADFile WadFile = wadFile;

		public readonly Configuration Configuration = configuration;
		public readonly DatVersion DatVersion = configuration.WriteVersion!;
		public readonly WadVersion WriteVersion = writeVersion;

		public ChunkWriter GetChunkWriter() => new ChunkWriter(WadFile, Configuration, WriteVersion, Stream, Position);
	}
}