using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.WadChunks;

namespace ArgonautReverse.IO
{
	public class WadReader:DataReader
	{
		public readonly WADFile WadFile;
		
		public readonly Configuration Configuration;
		public readonly DatVersion DatVersion;
		public readonly WadVersion ReadVersion;

		public WadReader(WADFile wadFile, Configuration conf, WadVersion wadVersion, byte[] data, int offset = 0, int? length = null) : base(data, offset, length ?? data.Length)
		{
			WadFile = wadFile;
			Configuration = conf;
			DatVersion = conf.ReadVersion;
			ReadVersion = wadVersion;
		}

		public ChunkReader ReadChunk(int length)
		{
			return new ChunkReader(this, Position, length);
		}

		public byte[] GetAllWadData() => Data.AsSpan(Offset, Length).ToArray();


		public unsafe void AssertChunkType(ChunkType chunkType)
		{
			var readChunkType = (ChunkType)Read<uint>();
			if(readChunkType != chunkType)
			{
				throw new ChunkNameError(AbsolutePosition, chunkType.ToString(), readChunkType.GetRawName());
			}
		}

		public void AssertEndOfChunk(ChunkType chunkType)
		{
			if(Remaining != 0)
			{
				throw new ChunkSizeMismatch(AbsolutePosition, chunkType.ToString(), Remaining);
			}
		}
	}
}
