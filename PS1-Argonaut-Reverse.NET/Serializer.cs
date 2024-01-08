namespace ArgonautReverse
{
	public sealed class Serializer:IDisposable
	{
		private BinaryWriter writer;

		public int Position
		{
			get => (int)writer.BaseStream.Position;
			set => writer.BaseStream.Position = value;
		}
		public int Length => (int)writer.BaseStream.Length;

		public Serializer(Stream stream)
		{
			writer = new BinaryWriter(stream);
		}

		public void Seek(int offset, SeekOrigin origin = SeekOrigin.Begin) => writer.BaseStream.Seek(offset, origin);

		public void WriteSByte(sbyte value) => writer.Write(value);
		public void WriteInt16(short value) => writer.Write(value);
		public void WriteInt32(int value) => writer.Write(value);

		public void WriteByte(byte value) => writer.Write(value);
		public void WriteUInt16(ushort value) => writer.Write(value);
		public void WriteUInt32(uint value) => writer.Write(value);

		public void WriteBytes(byte[] bytes) => writer.Write(bytes);

		//public int Read(byte[] dest, int index, int count) => writer.Read(dest, index, count);

		//BigEndian
		//public unsafe uint ReadUInt32BE()
		//{
		//	byte* ret = stackalloc byte[4];
		//	for(int i=3; i>=0; i--)
		//	{
		//		ret[i] = writer.ReadByte();
		//	}
		//	return *(uint*)ret;
		//}

		//public unsafe uint[] ReadUInt32Array(int length)
		//{
		//	var ret = new uint[length];
		//	for(int i=0; i<length; i++)
		//	{
		//		ret[i] = writer.ReadUInt32();
		//	}
		//	return ret;
		//}

		//public unsafe byte[][] ReadArrayOfByteArrays(int byteCount, int arrayCount)
		//{
		//	var ret = new byte[arrayCount][];
		//	for(int i=0; i<arrayCount; i++)
		//	{
		//		ret[i] = writer.ReadBytes(byteCount);
		//	}
		//	return ret;
		//}

		public void Dispose()
		{
			writer?.Close();
			writer = null;
			GC.SuppressFinalize(this);
		}
	}
}
