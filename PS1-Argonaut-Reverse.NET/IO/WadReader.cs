using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public interface IReadable<T>
	{
		public static abstract T Parse(WadReader parser);
	}
	public sealed class WadReader : BaseReader
	{
		public readonly VersionInfo ReadVersion;
		public readonly Configuration Configuration;

		public WadReader(Configuration conf, Stream stream) : base(stream)
		{
			ReadVersion = conf.ReadVersion;
		}
	}

	//These are separate to prevent conflict with methods only differing by generic arguments
	public static class ParsableExtensions
	{
		public static T Read<T>(this WadReader that) where T : IReadable<T> => T.Parse(that);

		public static T[] ReadArray<T>(this WadReader that, int length) where T : IReadable<T>
		{
			var ret = new T[length];
			for (int i = 0; i < length; i++)
			{
				ret[i] = T.Parse(that);
			}
			return ret;
		}
	}
}
