using ArgonautReverse.Engine;
using ArgonautReverse.Files;

namespace ArgonautReverse.IO
{
	public interface IReadable<T>
	{
		public static abstract T Parse(WadReader reader);
	}
	public class WadReader:BaseReader
	{
		public readonly WADFile WadFile;
		
		public readonly Configuration Configuration;
		public readonly DatVersion DatVersion;
		public readonly WadVersion ReadVersion;

		public WadReader(WADFile wadFile, Configuration conf, WadVersion wadVersion, Stream stream, bool handleStreamDisposal = true) : base(stream, handleStreamDisposal)
		{
			WadFile = wadFile;
			Configuration = conf;
			DatVersion = conf.ReadVersion;
			ReadVersion = wadVersion;
		}

		public ChunkReader ReadChunk(int start, int length)
		{
			this.Position = start;
			return new ChunkReader(this, start, length);
		}
	}

	public sealed class ChunkReader:WadReader
	{
		public readonly int Start;
		public readonly int ChunkLength;

		public int Remaining => ChunkLength - (Position-Start);

		public ChunkReader(WadReader wadReader, int start, int length) : base(wadReader.WadFile, wadReader.Configuration, wadReader.ReadVersion, wadReader.Stream, false)
		{
			Start = start;
			ChunkLength = length;
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
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = T.Parse(that);
			}
		}
	}
}
