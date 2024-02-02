using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public class ChunkWriter:WadWriter
	{
		public ChunkWriter(Configuration conf, WadVersion writeVersion, Stream stream, int offset) : base(conf, stream, offset)
		{
			WriteVersion = writeVersion;
		}
	}
}
