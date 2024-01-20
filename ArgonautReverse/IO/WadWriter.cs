using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public class WadWriter:BaseWriter
	{
		public readonly Configuration Configuration;
		public readonly DatVersion DatVersion;
		public readonly WadVersion WriteVersion;

		public WadWriter(Configuration configuration, WadVersion wadVersion, Stream stream):base(stream)
		{
			DatVersion = configuration.WriteVersion;
			WriteVersion = wadVersion;
		}
	}
}
