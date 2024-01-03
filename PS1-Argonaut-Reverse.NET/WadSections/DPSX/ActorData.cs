namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class ActorData:BaseDataClass
	{
		public readonly byte[] data;
		public ActorData(byte[] data)
		{
			this.data = data;
		}

		public int Size => this.data.Length;

		public static ActorData parse(Parser data_in, Configuration conf)
		{
			//base.parse(data_in, conf);
			var size = 4 * data_in.ReadInt32();
			return new ActorData(data_in.ReadBytes(size));
		}
	}
}