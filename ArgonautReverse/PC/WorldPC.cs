using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public enum PolygonFlagsPC:ushort
	{
		POLYGON_FLAG_NONE = 0x0,
		POLYGON_FLAG_1 = 0x1,
		POLYGON_FLAG_2 = 0x2,
		POLYGON_FLAG_4 = 0x4,
		POLYGON_FLAG_8 = 0x8,
		POLYGON_FLAG_10 = 0x10,
		POLYGON_FLAG_20 = 0x20,
		POLYGON_FLAG_40 = 0x40,
		POLYGON_FLAG_80 = 0x80,
		POLYGON_FLAG_100 = 0x100,
		POLYGON_FLAG_200 = 0x200,
		POLYGON_FLAG_400 = 0x400,
		POLYGON_FLAG_800 = 0x800,
		POLYGON_FLAG_1000 = 0x1000,
		POLYGON_FLAG_2000 = 0x2000,
		POLYGON_FLAG_4000 = 0x4000,
		POLYGON_FLAG_8000 = 0x8000,
	}

	public sealed class PolygonPointPC:IReadable<PolygonPointPC,WadFlagPC>
	{
		//public PolygonPoint prev;
		//public PolygonPoint next;
		public int X;
		public int Z;
		public ushort wField2;
		public short wField3;
		public int someY;
		public int gapField1;

		public static PolygonPointPC Parse(WadReader reader, WadFlagPC wadFlags)
		{
			var polygonPoint = new PolygonPointPC();
			polygonPoint.X = reader.Read<int>();
			polygonPoint.Z = reader.Read<int>();
			if((wadFlags & WadFlagPC.WAD_FLAG_4000000) != 0)
			{
				polygonPoint.someY = reader.Read<int>();
			}
			else
			{
				polygonPoint.someY = 0;
			}
			polygonPoint.wField2 = reader.Read<ushort>();
			polygonPoint.wField3 = reader.Read<short>();
			return polygonPoint;
		}
	}

	public sealed class PolygonStructPC:IReadable<PolygonStructPC, WadFlagPC>
	{
		//public ushort pointCount;
		public PolygonFlagsPC Flags{get;}
		public IReadOnlyList<PolygonPointPC> Points{get;}

		public PolygonStructPC(PolygonFlagsPC flags, IReadOnlyList<PolygonPointPC> points)
		{
			Flags = flags;
			Points = points;
		}

		public static PolygonStructPC Parse(WadReader reader, WadFlagPC wadFlags)
		{
			var polygonPointCount = reader.Read<ushort>();
			var flags = (PolygonFlagsPC)reader.Read<ushort>();
			var points = reader.ReadArray<PolygonPointPC, WadFlagPC>(wadFlags, polygonPointCount);
			//for(int pointIndex=0; pointIndex<polygonPointCount; pointIndex++)
			//{
			//	var polygonPoint = points[pointIndex];
			//	switch(polygonPoint.wField2)
			//	{
			//		case 1:
			//		case 6:
			//		case 7:
			//		case 8:
			//		case 9:
			//		case 11:
			//		case 13:
			//		case 14:
			//			flags |= (PolygonFlags)(1 << polygonPoint.wField2);
			//			break;
			//		case 2:
			//		case 3:
			//		case 4:
			//			flags |= PolygonFlags.POLYGON_FLAG_4;
			//			break;
			//		case 10:
			//			flags |= PolygonFlags.POLYGON_FLAG_400 | PolygonFlags.POLYGON_FLAG_40;
			//			break;
			//		case 12:
			//			flags |= PolygonFlags.POLYGON_FLAG_1000 | PolygonFlags.POLYGON_FLAG_40;
			//			break;
			//	}
			//}
			return new PolygonStructPC(flags, points);
		}
	}

	public sealed class PolygonArrayPC:IReadable<PolygonArrayPC, WadFlagPC>
	{
		//public int count;
		public IReadOnlyList<PolygonStructPC> Polygons{get;}

		public PolygonArrayPC(IReadOnlyList<PolygonStructPC> polygons)
		{
			Polygons = polygons;
		}

		public static PolygonArrayPC Parse(WadReader reader, WadFlagPC wadFlags)
		{
			var polygonArrayCount = reader.Read<int>();
			var polygons = reader.ReadArray<PolygonStructPC, WadFlagPC>(wadFlags, polygonArrayCount);
			return new PolygonArrayPC(polygons);
		}
	}
}
