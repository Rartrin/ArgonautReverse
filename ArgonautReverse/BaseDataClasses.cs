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

		public void CheckSize(WadReader reader)
		{
			if(reader.Remaining != 0)
			{
				throw new ChunkSizeMismatch(reader.AbsolutePosition, ChunkType.ToString(), reader.Remaining);
			}
		}
		public abstract BaseWadChunk Parse(WadReader data_in);
	}

	public abstract class BaseWadChunk
	{
		public readonly BaseWADChunkInfo Info;

		public readonly byte[] Data;

		public BaseWadChunk(BaseWADChunkInfo info, byte[] data = null)
		{
			Info = info;
			if(data is not null)
			{
				Data = data;
			}
		}

	
		public virtual void Serialize(Serializer data_out)
		{
			data_out.Write((uint)Info.ChunkType);
			data_out.Write((uint)Data.Length);
			data_out.WriteBytes(Data);
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
			return new UnsupportedChunk(this, data_in.GetAllWadData());
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
			return new UnknownChunk(this, data_in.GetAllWadData());
		}
	}

	public sealed class UnknownChunk:BaseWadChunk
	{
		public UnknownChunk(BaseWADChunkInfo info, byte[] data = null) : base(info, data){}
	}
}