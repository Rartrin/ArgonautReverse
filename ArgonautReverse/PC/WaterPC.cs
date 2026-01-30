using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public readonly struct WaterLevelStructPC(int mask, int field1):IReadable<WaterLevelStructPC>,IWritable
	{
		public readonly int mask = mask;
		public readonly int field1 = field1;

		public static WaterLevelStructPC Parse(WadReader reader)
		{
			var mask = reader.Read<int>();
			var field1 = reader.Read<int>();
			return new WaterLevelStructPC(mask, field1);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<int>(mask);
			writer.Write<int>(field1);
		}
	}
}