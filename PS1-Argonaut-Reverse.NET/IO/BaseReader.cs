using System.Numerics;
using System.Text;

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

		[Obsolete]public short ReadInt16() => Read<short>();
		[Obsolete]public int ReadInt32() => Read<int>();

		[Obsolete]public byte ReadByte() => Read<byte>();
		[Obsolete]public ushort ReadUInt16() => Read<ushort>();
		[Obsolete]public uint ReadUInt32() => Read<uint>();

		[Obsolete]public byte[] ReadBytes(int amount) => ReadArray<byte>(amount);

		public unsafe void Read<T>(out T value) where T : unmanaged, IBinaryInteger<T>
		{
			fixed(T* value0 = &value)
			{
				reader.ReadExactly(new Span<byte>((byte*)value0, sizeof(T)));
			}
		}

		public unsafe T Read<T>() where T : unmanaged, IBinaryInteger<T>
		{
			Read(out T ret);
			return ret;
		}

		public unsafe void ReadArray<T>(Span<T> array) where T : unmanaged, IBinaryInteger<T>
		{
			fixed (T* ret0 = array)
			{
				reader.ReadExactly(new Span<byte>((byte*)ret0, sizeof(T) * array.Length));
			}
		}

		public unsafe void ReadData<T>(T* array, int count) where T : unmanaged, IBinaryInteger<T>
		{
			reader.ReadExactly(new Span<byte>((byte*)array, sizeof(T) * count));
		}

		public unsafe T[] ReadArray<T>(int length) where T : unmanaged, IBinaryInteger<T>
		{
			if(length == 0){return Array.Empty<T>();}

			var ret = new T[length];
			ReadArray<T>(ret);
			return ret;
		}

		/// <summary>Reads a fixed length ASCII string</summary>
		public string ReadString(int length)
		{
			Span<byte> str = stackalloc byte[length];
			ReadArray(str);
			return Encoding.ASCII.GetString(str);
		}

		public T ReadEmptyReference<T>() where T:class
		{
			int value = Read<int>();
			if(value != 0)
			{
				throw new Exception();
			}
			return null;
		}

		public void AssertRead<T>(T expected) where T : unmanaged, IBinaryInteger<T>
		{
			var value = Read<T>();
			if(value != expected)
			{
				throw new Exception($"Expected {expected} but read {value}");
			}
		}

		public unsafe uint[] ReadUInt32Array(int length) => ReadArray<uint>(length);

		public unsafe int[] ReadInt32Array(int length) => ReadArray<int>(length);

		public unsafe byte[][] ReadArrayOfByteArrays(int byteCount, int arrayCount)
		{
			var ret = new byte[arrayCount][];
			for (int i = 0; i < arrayCount; i++)
			{
				ret[i] = ReadArray<byte>(byteCount);
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
