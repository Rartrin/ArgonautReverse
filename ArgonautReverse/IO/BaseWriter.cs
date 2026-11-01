using System.Numerics;
using System.Text;

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

		//This doesn't make much sense in a writer.
		[Obsolete]public void SkipBytes(int offset)
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

		public unsafe void Write<T>(in T value) where T : unmanaged, INumber<T>
		{
			WriteData(value);
		}

		public unsafe void WriteArray<T>(IReadOnlyList<T> array) where T : unmanaged, INumber<T>
		{
			for(int i=0; i<array.Count; i++)
			{
				WriteData(array[i]);
			}
		}

		public unsafe void WriteSizedArray<T>(int size, IReadOnlyList<T> array) where T : unmanaged, INumber<T>
		{
			if(size != array.Count){throw new Exception();}

			WriteArray(array);
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

		public unsafe void WriteEmptyArray<T>(int count) where T : unmanaged, INumber<T>
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

		/// <summary>Writes a fixed length ASCII string. Does not null terminate.</summary>
		public void WriteStringData(int length, string str)
		{
			if(length != str.Length){throw new Exception();}

			Span<byte> bytes = stackalloc byte[length];
			Encoding.ASCII.GetBytes(str, bytes);
			WriteData(bytes);
		}

		/// <summary>Writes a fixed length UTF16 string. Does not null terminate.</summary>
		public void WriteStringDataUTF16(int length, string str)
		{
			if(length != str.Length){throw new Exception();}

			Span<byte> bytes = stackalloc byte[2 * length];
			Encoding.Unicode.GetBytes(str, bytes);
			WriteData(bytes);
		}

		/// <summary>Writes a fixed length ASCII string with null terminator.</summary>
		public void WriteCString(string str)
		{
			Span<byte> bytes = stackalloc byte[str.Length+1];
			bytes[^1] = 0;
			Encoding.ASCII.GetBytes(str, bytes);
			WriteData(bytes);
		}

		/// <summary>Writes a fixed length ASCII string with null terminator and pads the remaining space.</summary>
		public void WriteCStringPadded(int length, string str)
		{
			//String must be less than to account for the null terminator.
			if(str.Length >= length){throw new Exception("String is too long.");}

			Span<byte> bytes = stackalloc byte[length];
			bytes.Slice(str.Length).Clear();
			Encoding.ASCII.GetBytes(str, bytes);
			WriteData(bytes);
		}
	}
}
