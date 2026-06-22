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

		public unsafe void Read<T>(out T value) where T : unmanaged, INumber<T>
		{
			ReadData(out value);
		}

		public unsafe T Read<T>() where T : unmanaged, INumber<T>
		{
			ReadData(out T ret);
			return ret;
		}

		public unsafe void ReadArray<T>(Span<T> array) where T : unmanaged, INumber<T>
		{
			ReadData(array);
		}

		public unsafe T[] ReadArray<T>(int length) where T : unmanaged, INumber<T>
		{
			if(length == 0){return [];}

			var ret = new T[length];
			ReadData(ret);
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
			ReadData(str);
			return Encoding.ASCII.GetString(str);
		}

		public string ReadStringUTF16(int length)
		{
			Span<byte> str = stackalloc byte[length];
			ReadData(str);
			return Encoding.Unicode.GetString(str);
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

		public void AssertRead<T>(T expected, bool warn = false) where T : unmanaged, INumber<T>
		{
			var value = Read<T>();
			if(value != expected)
			{
				string message = $"Assertion failed. Expected {expected} but read {value}.";
				if(warn)
				{
					Console.WriteLine($"WARNING: {message}");
				}
				else
				{
					throw new Exception(message);
				}
			}
		}

		public unsafe void AssertEmptyReadData<T>(int elementCount) where T : unmanaged,INumber<T>
		{
			if(elementCount == 0){return;}

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
	}
}
