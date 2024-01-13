using ArgonautReverse.Engine;

namespace ArgonautReverse.IO
{
	public interface IReadable<T>
	{
		public static abstract T Parse(WadReader reader);
	}
	public sealed class WadReader : BaseReader
	{
		public readonly DatVersion DatVersion;
		public readonly WadVersion ReadVersion;
		public readonly Configuration Configuration;

		public WadReader(Configuration conf, WadVersion wadVersion, Stream stream) : base(stream)
		{
			DatVersion = conf.ReadVersion;
			ReadVersion = wadVersion;
		}
	}

	//These are separate to prevent conflict with methods only differing by generic arguments
	public static class ParsableExtensions
	{
		public static T Read<T>(this WadReader that) where T : IReadable<T>
		{
			return T.Parse(that);
		}

		public static T[] ReadArray<T>(this WadReader that, int length) where T : IReadable<T>
		{
			var ret = new T[length];
			that.ReadArray<T>(ret);
			return ret;
		}

		public static void Read<T>(this WadReader that, out T value) where T : IReadable<T>
		{
			value = T.Parse(that);
		}

		public static void ReadArray<T>(this WadReader that, Span<T> array) where T : IReadable<T>
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = T.Parse(that);
			}
		}
	}
}
