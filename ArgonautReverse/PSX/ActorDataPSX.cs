using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public sealed class ActorDataPSX
	{
		public readonly byte[] data;
		public ActorDataPSX(byte[] data)
		{
			this.data = data;
		}

		public int Size => data.Length;

		public static ActorDataPSX Parse(WadReader data_in)
		{
			var size = 4 * data_in.Read<int>();
			return new ActorDataPSX(data_in.ReadArray<byte>(size));
		}
	}
}