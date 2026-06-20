using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.WadChunks;

namespace ArgonautReverse.IO
{
	public class WadReader(WADFile wadFile, Configuration conf, WadVersion wadVersion, byte[] data, int offset = 0, int? length = null):DataReader(data, offset, length ?? data.Length)
	{
		public readonly WADFile WadFile = wadFile;
		
		public readonly Configuration Configuration = conf;
		public readonly DatVersion DatVersion = conf.ReadVersion;
		public readonly WadVersion ReadVersion = wadVersion;

		public ChunkReader ReadChunk(int length)
		{
			return new ChunkReader(this, Position, length);
		}

		public byte[] GetAllWadData() => Data.AsSpan(Offset, Length).ToArray();

		public void AssertEndOfChunk(ChunkType chunkType, bool warn = false)
		{
			if(Remaining != 0)
			{
				string message = $"Chunk {chunkType} is longer than expected. Total unread bytes: {Remaining}";
				if(!warn)
				{
					throw new Exception(message);
				}
				else
				{
					Console.WriteLine($"WARNING: {message}");
				}
			}
		}
	}
}