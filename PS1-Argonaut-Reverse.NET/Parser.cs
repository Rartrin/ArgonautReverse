namespace ArgonautReverse
{
	public sealed class Parser:IDisposable
	{
		private BinaryReader reader;

		public long Position
		{
			get => reader.BaseStream.Position;
			set => reader.BaseStream.Position = value;
		}

		public Parser(Stream stream)
		{
			reader = new BinaryReader(stream);
		}

		public void seek(int offset, SeekOrigin origin = SeekOrigin.Begin) => reader.BaseStream.Seek(offset, origin);
		public int tell() => (int)Position;

		public sbyte ReadSByte() => reader.ReadSByte();
		public short ReadInt16() => reader.ReadInt16();
		public int ReadInt32() => reader.ReadInt32();

		public byte ReadByte() => reader.ReadByte();
		public ushort ReadUInt16() => reader.ReadUInt16();
		public uint ReadUInt32() => reader.ReadUInt32();

		public byte[] ReadBytes(int amount) => reader.ReadBytes(amount);

		public void Dispose()
		{
			reader?.Close();
			reader = null;
			GC.SuppressFinalize(this);
		}
	}
}
