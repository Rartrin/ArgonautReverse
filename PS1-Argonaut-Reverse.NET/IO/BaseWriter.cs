using System.Numerics;

namespace ArgonautReverse.IO
{
	public class BaseWriter:IDisposable
	{
		private Stream writer;

		public int Position
		{
			get => (int)writer.Position;
			set => writer.Position = value;
		}
		public int Length => (int)writer.Length;

		public BaseWriter(Stream stream)
		{
			writer = stream;
		}

		public void Seek(int offset, SeekOrigin origin = SeekOrigin.Begin) => writer.Seek(offset, origin);

		public void WriteSByte(sbyte value) => Write(value);
		public void WriteInt16(short value) => Write(value);
		public void WriteInt32(int value) => Write(value);

		public void WriteByte(byte value) => Write(value);
		public void WriteUInt16(ushort value) => Write(value);
		public void WriteUInt32(uint value) => Write(value);

		public void WriteBytes(byte[] bytes) => writer.Write(bytes);

		public unsafe void Write<T>(T value) where T : unmanaged, IBinaryInteger<T>
		{
			writer.Write(new ReadOnlySpan<byte>((byte*)&value, sizeof(T)));
		}

		public void Dispose()
		{
			writer?.Close();
			writer = null;
			GC.SuppressFinalize(this);
		}
	}
}
