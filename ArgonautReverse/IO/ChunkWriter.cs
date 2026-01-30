using ArgonautReverse.Engine;
using ArgonautReverse.Files;

namespace ArgonautReverse.IO
{
	public class ChunkWriter(WADFile wadFile, Configuration conf, WadVersion writeVersion, MemoryStream stream, int offset):WadWriter(wadFile, conf, writeVersion, stream, offset);
}