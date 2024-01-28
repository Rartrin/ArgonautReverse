using System.Numerics;
using System.Text;

namespace ArgonautReverse.IO
{
	public abstract class BaseReader
	{
		public abstract int Position{get;set;}
		public abstract int Length{get;}

		public int Remaining => Length-Position;

		public void SkipBytes(int count)
		{
			Position += count;
		}

		public unsafe void Read<T>(out T value) where T : unmanaged, IBinaryNumber<T>
		{
			ReadData(out value);
		}

		public unsafe T Read<T>() where T : unmanaged, IBinaryNumber<T>
		{
			ReadData(out T ret);
			return ret;
		}

		public unsafe void ReadArray<T>(Span<T> array) where T : unmanaged, IBinaryNumber<T>
		{
			ReadData(array);
		}

		public unsafe T[] ReadArray<T>(int length) where T : unmanaged, IBinaryNumber<T>
		{
			if(length == 0){return Array.Empty<T>();}

			var ret = new T[length];
			ReadData<T>(ret);
			return ret;
		}

		protected abstract void ReadRawData(Span<byte> data);

		public unsafe void ReadData<T>(out T data) where T : unmanaged
		{
			fixed(T* data0 = &data)
			{
				ReadRawData(new Span<byte>((byte*)data0, sizeof(T)));
			}
		}
		public unsafe void ReadData<T>(Span<T> array) where T : unmanaged
		{
			fixed (T* ret0 = array)
			{
				ReadRawData(new Span<byte>((byte*)ret0, sizeof(T) * array.Length));
			}
		}
		public unsafe void ReadData<T>(T* array, int count = 1) where T : unmanaged
		{
			ReadRawData(new Span<byte>((byte*)array, sizeof(T) * count));
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

		//public T ReadEmptyReference<T>() where T:class
		//{
		//	int value = Read<int>();
		//	if(value != 0)
		//	{
		//		throw new Exception();
		//	}
		//	return null;
		//}

		public void AssertRead<T>(T expected) where T : unmanaged, IBinaryNumber<T>
		{
			var value = Read<T>();
			if(value != expected)
			{
				throw new Exception($"Expected {expected} but read {value}");
			}
		}

		public unsafe void AssertEmptyReadData<T>(int elementCount) where T : unmanaged,IEquatable<T>
		{
			Span<T> data = stackalloc T[elementCount];
			ReadData(data);
			for(int i=0; i<data.Length; i++)
			{
				if(!data[i].Equals(default))
				{
					throw new Exception($"Data read is not empty");
				}
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
	}
}
