using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.WadChunks;

namespace ArgonautReverse
{
	public abstract class BaseWADChunkInfo
	{
		//TODO: Separate into Read and Write
		public abstract WadVersion[] SupportedWadVersions{get;}
		public abstract string ChunkDescription{get;}
		public abstract ChunkType ChunkType{get;}

		public BaseWADChunkInfo(){}

		public unsafe void CheckChunkType(WadReader data_in)
		{
			var readChunkType = (ChunkType)data_in.Read<uint>();
			if(readChunkType != ChunkType)
			{
				throw new ChunkNameError(data_in.AbsolutePosition, ChunkType.ToString(), readChunkType.GetRawName());
			}
		}

		public void CheckSize(int expectedSize, int chunkStart, int currentPosition)
		{
			var calculated_size = currentPosition - chunkStart;
			if(expectedSize != calculated_size)
			{
				throw new ChunkSizeMismatch(currentPosition, ChunkType.ToString(), expectedSize, calculated_size);
			}
		}

		protected void ParseHeader(WadReader data_in, out int size, out int start)
		{
			if(!SupportedWadVersions.Contains(data_in.ReadVersion))
			{
				throw new UnsupportedParsing(ChunkDescription);
			}
			CheckChunkType(data_in);
			size = data_in.Read<int>();
			start = data_in.AbsolutePosition;
		}
		public abstract BaseWadChunk Parse(WadReader data_in);

		protected static byte[] GetChunkData(WadReader data_in)
		{
			var start = data_in.Position;
			var chunkType = (ChunkType)data_in.Read<uint>();
			var size = data_in.Read<int>();

			data_in.Position = start;
			var data = data_in.ReadArray<byte>(sizeof(ChunkType) + sizeof(int) + size);
			data_in.Position = start;
			return data;
		}
	}

	public abstract class BaseWadChunk
	{
		public readonly BaseWADChunkInfo Info;

		public readonly byte[] _data;

		public BaseWadChunk(BaseWADChunkInfo info, byte[] data = null)
		{
			Info = info;
			if(data is not null)
			{
				this._data = data;
			}
		}

	
		public virtual void Serialize(Serializer data_out)
		{
			data_out.WriteBytes(this._data);
		}
		protected int SerializeHeader(Serializer data_out)
		{
			if(!Info.SupportedWadVersions.Contains(data_out.WriteVersion))
			{
				throw new UnsupportedSerialization(Info.ChunkDescription);
			}
			data_out.Write((uint)Info.ChunkType);
			data_out.Write<uint>(0);// Placeholder for the chunk's size
			return data_out.Position;
		}

		protected static void SerializeChunkSize(Serializer data_out, int start)
		{
			var end = data_out.Position;
			var size = end - start;
			data_out.Position = start - 4;
			data_out.WriteInt32(size);
			data_out.Position = end;
		}
	}

	public sealed class UnsupportedChunkInfo:BaseWADChunkInfo
	{
		public override WadVersion[] SupportedWadVersions => Configuration.ALL_WADS;

		public override string ChunkDescription => "(Unsupported for the game) " + UnsuppportedType.ChunkDescription;

		public BaseWADChunkInfo UnsuppportedType{get;}
		public override ChunkType ChunkType => UnsuppportedType.ChunkType;

		public unsafe UnsupportedChunkInfo(BaseWADChunkInfo unsuppportedType)
		{
			UnsuppportedType = unsuppportedType;
		}

		public override UnsupportedChunk Parse(WadReader data_in)
		{
			return new UnsupportedChunk(this, GetChunkData(data_in));
		}
	}

	public sealed class UnsupportedChunk:BaseWadChunk
	{
		public UnsupportedChunk(BaseWADChunkInfo info, byte[] data = null) : base(info, data){}
	}

	public sealed class UnknownChunkInfo:BaseWADChunkInfo
	{
		public override WadVersion[] SupportedWadVersions => Configuration.ALL_WADS;

		public override string ChunkDescription => $"{ChunkType.GetRawName()} chunk";

		public override ChunkType ChunkType{get;}

		public unsafe UnknownChunkInfo(ChunkType chunkType)
		{
			ChunkType = chunkType;
		}

		public override UnknownChunk Parse(WadReader data_in)
		{
			return new UnknownChunk(this, GetChunkData(data_in));
		}
	}

	public sealed class UnknownChunk:BaseWadChunk
	{
		public UnknownChunk(BaseWADChunkInfo info, byte[] data = null) : base(info, data){}
	}
}