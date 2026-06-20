using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
	public sealed class Zone(uint infoIndex):IReadable<Zone>,IWritable
	{
		public readonly uint InfoIndex = infoIndex;//Index in ZoneTable
		//public uint ViewZone;//Removed in NEWZONES

		public static Zone Parse(WadReader reader)
		{
			var infoIndex = reader.Read<uint>();
			return new Zone(infoIndex);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<uint>(InfoIndex); 
		}
	}

	public sealed class ZoneInfo(uint viewZone, Fixed32 waterLevel):IReadable<ZoneInfo>,IWritable
	{
		public readonly uint ViewZone = viewZone;
		public readonly Fixed32	WaterLevel = waterLevel;

		public static ZoneInfo Parse(WadReader reader)
		{
			var viewZone = reader.Read<uint>();
			var waterLevel = reader.Read<Fixed32>();
			return new ZoneInfo(viewZone, waterLevel);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<uint>(ViewZone);
			writer.Write<Fixed32>(WaterLevel);
		}
	}
}