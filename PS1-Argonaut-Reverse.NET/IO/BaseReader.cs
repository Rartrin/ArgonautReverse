using System.Numerics;

namespace ArgonautReverse.IO
{
	public class BaseReader:IDisposable
	{
		private Stream reader;

		public int Position
		{
			get => (int)reader.Position;
			set => reader.Position = value;
		}
		public int Length => (int)reader.Length;

		public BaseReader(Stream stream)
		{
			if (stream.Length > int.MaxValue)
			{
				throw new Exception("Unsupported file size. Files can not be greater than 2GB (int.MaxValue).");
			}
			reader = stream;
		}

		public void Seek(int offset, SeekOrigin origin = SeekOrigin.Begin) => reader.Seek(offset, origin);

		public sbyte ReadSByte() => Read<sbyte>();
		public short ReadInt16() => Read<short>();
		public int ReadInt32() => Read<int>();

		public byte ReadByte() => Read<byte>();
		public ushort ReadUInt16() => Read<ushort>();
		public uint ReadUInt32() => Read<uint>();

		public byte[] ReadBytes(int amount) => ReadArray<byte>(amount);

		public unsafe T Read<T>() where T : unmanaged, IBinaryInteger<T>
		{
			T ret;
			reader.ReadExactly(new Span<byte>((byte*)&ret, sizeof(T)));
			return ret;
		}

		public unsafe T[] ReadArray<T>(int length) where T : unmanaged, IBinaryInteger<T>
		{
			var ret = new T[length];
			fixed (T* ret0 = ret)
			{
				reader.ReadExactly(new Span<byte>((byte*)ret0, sizeof(T) * length));
			}
			return ret;
		}



		public int Read(byte[] dest, int index, int count) => reader.Read(dest, index, count);

		//BigEndian
		public unsafe uint ReadUInt32BE()
		{
			Span<byte> raw = stackalloc byte[4];
			byte* ret = stackalloc byte[4];
			reader.ReadExactly(raw);
			for (int i = 0; i < 4; i++)
			{
				ret[i] = raw[3 - i];
			}
			return *(uint*)ret;
		}

		public unsafe uint[] ReadUInt32Array(int length) => ReadArray<uint>(length);

		public unsafe int[] ReadInt32Array(int length) => ReadArray<int>(length);

		public unsafe byte[][] ReadArrayOfByteArrays(int byteCount, int arrayCount)
		{
			var ret = new byte[arrayCount][];
			for (int i = 0; i < arrayCount; i++)
			{
				ret[i] = ReadBytes(byteCount);
			}
			return ret;
		}

		public void Dispose()
		{
			reader?.Close();
			reader = null;
			GC.SuppressFinalize(this);
		}
	}
}
