namespace Compat
{
	public static class Compat
	{
		public const SeekOrigin SEEK_CUR = SeekOrigin.Current;

		public static void seek(this BinaryReader that, int amount, SeekOrigin origin = SeekOrigin.Begin) => that.BaseStream.Seek(amount, origin);

		public static int tell(this BinaryReader that) => (int)that.BaseStream.Position;

		//BigEndian
		public static unsafe uint ReadUInt32BE(this BinaryReader that)
		{
			byte* ret = stackalloc byte[4];
			for(int i=3; i>=0; i--)
			{
				ret[i] = that.ReadByte();
			}
			return *(uint*)ret;
		}

		public static unsafe uint[] ReadUInt32Array(this BinaryReader that, int length)
		{
			var ret = new uint[length];
			for(int i=0; i<length; i++)
			{
				ret[i] = that.ReadUInt32();
			}
			return ret;
		}

		public static unsafe byte[][] ReadArrayOfByteArrays(this BinaryReader that, int byteCount, int arrayCount)
		{
			var ret = new byte[arrayCount][];
			for(int i=0; i<arrayCount; i++)
			{
				ret[i] = that.ReadBytes(byteCount);
			}
			return ret;
		}
	}
}
