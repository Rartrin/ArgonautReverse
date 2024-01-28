using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public struct WaterLevelStructPC:IReadable<WaterLevelStructPC>
	{
		public int mask;
		public int field1;

		public WaterLevelStructPC(int mask, int field1)
		{
			this.mask = mask;
			this.field1 = field1;
		}

		public static WaterLevelStructPC Parse(WadReader reader)
		{
			var mask = reader.Read<int>();
			var field1 = reader.Read<int>();
			return new WaterLevelStructPC(mask, field1);
		}
	}
}
