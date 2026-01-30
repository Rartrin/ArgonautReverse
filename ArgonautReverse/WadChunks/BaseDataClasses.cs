using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine.Chunks;

namespace ArgonautReverse.WadChunks
{
	public abstract class BaseWADChunkInfo
	{
		//TODO: Separate into Read and Write
		public abstract WadVersion[] SupportedWadVersions{get;}
		public abstract string ChunkDescription{get;}
		public abstract ChunkType ChunkType{get;}

		public abstract BaseWadChunk Parse(WadReader data_in);
	}

	public abstract class BaseWADChunkInfo<T>:BaseWADChunkInfo where T:BaseWadChunk;

	public abstract class BaseWadChunk(BaseWADChunkInfo info, byte[]? data = null):IConvertibleToOSE<IReadOnlyList<ChunkOSE>>
	{
		public readonly BaseWADChunkInfo Info = info;

		public readonly byte[]? Data = data;

		public virtual void PostParseSetup(WADFile wadFile){}

		public void Write(ChunkWriter writer)
		{
			writer.Write((uint)Info.ChunkType);
			var sizeHold = writer.WriteHold<int>();
			var startPosition = writer.Position;
			WriteData(writer);
			var endPosition = writer.Position;
			sizeHold.Set(endPosition-startPosition);
		}

		protected abstract void WriteData(ChunkWriter writer);

		public virtual void Serialize(WadWriter data_out)
		{
			data_out.Write((uint)Info.ChunkType);
			data_out.Write((uint)Data!.Length);
			data_out.WriteBytes(Data);
		}
		protected int SerializeHeader(WadWriter data_out)
		{
			if(!Info.SupportedWadVersions.Contains(data_out.WriteVersion))
			{
				throw new UnsupportedSerialization(Info.ChunkDescription);
			}
			data_out.Write((uint)Info.ChunkType);
			data_out.Write<uint>(0);// Placeholder for the chunk's size
			return data_out.Position;
		}

		protected static void SerializeChunkSize(WadWriter data_out, int start)
		{
			var end = data_out.Position;
			var size = end - start;
			data_out.Position = start - 4;
			data_out.WriteInt32(size);
			data_out.Position = end;
		}

		public virtual IReadOnlyList<ChunkOSE> ToOSE() => throw new NotImplementedException();
	}

	public sealed class UnsupportedChunkInfo(BaseWADChunkInfo unsuppportedType):BaseWADChunkInfo
	{
		public override WadVersion[] SupportedWadVersions => Configuration.ALL_WADS;

		public override string ChunkDescription => "(Unsupported for the game) " + UnsuppportedType.ChunkDescription;

		public BaseWADChunkInfo UnsuppportedType{get;} = unsuppportedType;

		public override ChunkType ChunkType => UnsuppportedType.ChunkType;

		public override UnsupportedChunk Parse(WadReader reader)
		{
			var data = reader.ReadArray<byte>(reader.Remaining);
			return new UnsupportedChunk(this, data);
		}
	}

	public sealed class UnsupportedChunk(BaseWADChunkInfo info, byte[]? data = null):BaseWadChunk(info, data)
	{
		protected override void WriteData(ChunkWriter writer) => writer.WriteBytes(Data!);
	}

	public sealed class UnknownChunkInfo(ChunkType chunkType):BaseWADChunkInfo
	{
		public override WadVersion[] SupportedWadVersions => Configuration.ALL_WADS;

		public override string ChunkDescription => $"{ChunkType.GetRawName()} chunk";

		public override ChunkType ChunkType{get;} = chunkType;

		public override UnknownChunk Parse(WadReader reader)
		{
			var data = reader.ReadArray<byte>(reader.Remaining);
			return new UnknownChunk(this, data);
		}
	}

	public sealed class UnknownChunk(BaseWADChunkInfo info, byte[]? data = null):BaseWadChunk(info, data)
	{
		protected override void WriteData(ChunkWriter writer) => writer.WriteBytes(Data!);
	}
}