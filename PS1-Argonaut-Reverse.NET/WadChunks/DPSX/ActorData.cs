using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.DPSX
{
	public sealed class ActorData
	{
		public readonly byte[] data;
		public ActorData(byte[] data)
		{
			this.data = data;
		}

		public int Size => this.data.Length;

		public static ActorData Parse(WadReader data_in)
		{
			var size = 4 * data_in.Read<int>();
			return new ActorData(data_in.ReadArray<byte>(size));
		}
	}
}