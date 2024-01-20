using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public sealed class Serializer : BaseWriter
	{
		public readonly Configuration Configuration;
		public readonly DatVersion DatVersion;
		public WadVersion WriteVersion{get;set;}

		public Serializer(Configuration configuration, Stream stream):base(stream)
		{
			DatVersion = configuration.WriteVersion;
		}
	}
}
