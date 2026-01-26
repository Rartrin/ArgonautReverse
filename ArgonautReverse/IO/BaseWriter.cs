using System.Numerics;

namespace ArgonautReverse.IO
{
	public readonly struct WriterHold<T>(BaseWriter writer, int index) where T : unmanaged
	{
		public readonly void Set(in T value) => writer.SetData(value, index);
	}
	public abstract class BaseWriter
	{
		public abstract int Position{get;set;}
		public abstract int Length{get;}

		public void SkipBytes(int offset)
		{
			Position += offset;
		}

		public unsafe WriterHold<T> WriteHold<T>() where T : unmanaged
		{
			var holder = new WriterHold<T>(this, Position);
			Position += sizeof(T);
			return holder;
		}

		public void WriteSByte(sbyte value) => Write(value);
		public void WriteInt16(short value) => Write(value);
		public void WriteInt32(int value) => Write(value);

		public void WriteByte(byte value) => Write(value);
		public void WriteUInt16(ushort value) => Write(value);
		public void WriteUInt32(uint value) => Write(value);

		public void WriteBytes(byte[] bytes) => WriteArray(bytes);

		public unsafe void Write<T>(in T value) where T : unmanaged, IBinaryNumber<T>
		{
			WriteData(value);
		}

		public unsafe void WriteArray<T>(IReadOnlyList<T> array) where T : unmanaged, IBinaryNumber<T>
		{
			for(int i=0; i<array.Count; i++)
			{
				WriteData(array[i]);
			}
		}

		protected void SetRawData(ReadOnlySpan<byte> data, int index)
		{
			var oldPos = Position;
			Position = index;
			WriteRawData(data);
			Position = oldPos;
		}
		protected abstract void WriteRawData(ReadOnlySpan<byte> data);

		public unsafe void WriteData<T>(in T data) where T : unmanaged
		{
			fixed(T* data0 = &data)
			{
				WriteRawData(new ReadOnlySpan<byte>((byte*)data0, sizeof(T)));
			}
		}
		public unsafe void WriteData<T>(ReadOnlySpan<T> array) where T : unmanaged
		{
			fixed(T* ret0 = array)
			{
				WriteRawData(new ReadOnlySpan<byte>((byte*)ret0, sizeof(T) * array.Length));
			}
		}
		public unsafe void WriteData<T>(T* array, int count = 1) where T : unmanaged
		{
			WriteRawData(new ReadOnlySpan<byte>((byte*)array, sizeof(T) * count));
		}

		public unsafe void WriteEmptyArray<T>(int count) where T : unmanaged, IBinaryNumber<T>
		{
			var empty = T.Zero;
			for(int i=0; i<count; i++)
			{
				Write(empty);
			}
		}

		public unsafe void SetData<T>(in T data, int index) where T : unmanaged
		{
			fixed(T* data0 = &data)
			{
				SetRawData(new ReadOnlySpan<byte>((byte*)data0, sizeof(T)), index);
			}
		}

		///// <summary>Writes a fixed length ASCII string</summary>
		//public string WriteString(string str)
		//{
		//	ReadArray(str);
		//	return Encoding.Latin1.GetString(str);
		//}
	}
}
