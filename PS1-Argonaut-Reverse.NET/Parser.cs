namespace ArgonautReverse
{
	public sealed class Parser:IDisposable
	{
		private BinaryReader reader;

		public int Position
		{
			get => (int)reader.BaseStream.Position;
			set => reader.BaseStream.Position = value;
		}
		public int Length => (int)reader.BaseStream.Length;

		public Parser(Stream stream)
		{
			if(stream.Length > int.MaxValue)
			{
				throw new Exception("Unsupported file size. Files can not be greater than 2GB (int.MaxValue).");
			}
			reader = new BinaryReader(stream);
		}

		public void Seek(int offset, SeekOrigin origin = SeekOrigin.Begin) => reader.BaseStream.Seek(offset, origin);

		public sbyte ReadSByte() => reader.ReadSByte();
		public short ReadInt16() => reader.ReadInt16();
		public int ReadInt32() => reader.ReadInt32();

		public byte ReadByte() => reader.ReadByte();
		public ushort ReadUInt16() => reader.ReadUInt16();
		public uint ReadUInt32() => reader.ReadUInt32();

		public byte[] ReadBytes(int amount) => reader.ReadBytes(amount);

		public int Read(byte[] dest, int index, int count) => reader.Read(dest, index, count);

		//BigEndian
		public unsafe uint ReadUInt32BE()
		{
			byte* ret = stackalloc byte[4];
			for(int i=3; i>=0; i--)
			{
				ret[i] = reader.ReadByte();
			}
			return *(uint*)ret;
		}

		public unsafe uint[] ReadUInt32Array(int length)
		{
			var ret = new uint[length];
			for(int i=0; i<length; i++)
			{
				ret[i] = reader.ReadUInt32();
			}
			return ret;
		}

		public unsafe byte[][] ReadArrayOfByteArrays(int byteCount, int arrayCount)
		{
			var ret = new byte[arrayCount][];
			for(int i=0; i<arrayCount; i++)
			{
				ret[i] = reader.ReadBytes(byteCount);
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
