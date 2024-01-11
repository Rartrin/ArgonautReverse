using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public sealed class Serializer : BaseWriter
	{
		public readonly Configuration Configuration;
		public readonly VersionInfo WriteVersion;

		public Serializer(Configuration configuration, Stream stream):base(stream)
		{
			WriteVersion = configuration.WriteVersion;
		}
	}
}
