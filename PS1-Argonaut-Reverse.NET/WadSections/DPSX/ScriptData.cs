namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class ScriptData:BaseDataClass
	{
		public readonly byte[] data;
		public ScriptData(byte[] data)
		{
			this.data = data;
		}

		//@property
		public int size => this.data.Length;

		//@classmethod
		public static ScriptData parse(Parser data_in, Configuration conf)
		{
			//base.parse(data_in, conf);
			var size = 4 * data_in.ReadInt32();
			return new ScriptData(data_in.ReadBytes(size));
		}
	}
}