using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public class WadWriter:BaseWriter
	{
		public readonly Configuration Configuration;
		public readonly VersionInfo WriteVersion;

		public WadWriter(Configuration configuration, Stream stream):base(stream)
		{
			WriteVersion = configuration.WriteVersion;
		}
	}
}
