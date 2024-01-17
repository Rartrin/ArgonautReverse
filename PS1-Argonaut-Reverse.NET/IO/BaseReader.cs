using System.Numerics;
using System.Text;

namespace ArgonautReverse.IO
{
	public class BaseReader:IDisposable
	{
		public Stream Stream{get;private set;}

		public int Position
		{
			get => (int)Stream.Position;
			set => Stream.Position = value;
		}
		public int Length => (int)Stream.Length;

		private readonly bool handleStreamDisposal;

		public BaseReader(Stream stream, bool handleStreamDisposal = true)
		{
			if (stream.Length > int.MaxValue)
			{
				throw new Exception("Unsupported file size. Files can not be greater than 2GB (int.MaxValue).");
			}
			this.handleStreamDisposal = handleStreamDisposal;
			this.Stream = stream;
		}

		public void Seek(int offset, SeekOrigin origin) => Stream.Seek(offset, origin);

		public unsafe void Read<T>(out T value) where T : unmanaged, IBinaryInteger<T>
		{
			ReadData(out value);
		}

		public unsafe T Read<T>() where T : unmanaged, IBinaryInteger<T>
		{
			ReadData(out T ret);
			return ret;
		}

		public unsafe void ReadArray<T>(Span<T> array) where T : unmanaged, IBinaryInteger<T>
		{
			ReadData(array);
		}

		public unsafe T[] ReadArray<T>(int length) where T : unmanaged, IBinaryInteger<T>
		{
			if(length == 0){return Array.Empty<T>();}

			var ret = new T[length];
			ReadData<T>(ret);
			return ret;
		}


		public unsafe void ReadData<T>(out T data) where T : unmanaged
		{
			fixed(T* data0 = &data)
			{
				Stream.ReadExactly(new Span<byte>((byte*)data0, sizeof(T)));
			}
		}
		public unsafe void ReadData<T>(Span<T> array) where T : unmanaged
		{
			fixed (T* ret0 = array)
			{
				Stream.ReadExactly(new Span<byte>((byte*)ret0, sizeof(T) * array.Length));
			}
		}
		public unsafe void ReadData<T>(T* array, int count = 1) where T : unmanaged
		{
			Stream.ReadExactly(new Span<byte>((byte*)array, sizeof(T) * count));
		}
		public unsafe T ReadData<T>() where T : unmanaged
		{
			ReadData(out T ret);
			return ret;
		}

		

		/// <summary>Reads a fixed length ASCII string</summary>
		public string ReadString(int length)
		{
			Span<byte> str = stackalloc byte[length];
			ReadArray(str);
			return Encoding.Latin1.GetString(str);
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
			if(handleStreamDisposal)
			{
				Stream?.Close();
				Stream = null;
				GC.SuppressFinalize(this);
			}
		}
	}
}
