using ArgonautReverse.IO;
using ArgonautReverse.PC;
using ArgonautReverse.WadChunks.WFPC;

namespace ArgonautReverse.WadChunks.MAP
{
	public enum PolygonFlags:ushort
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

	public sealed class PolygonPoint:IReadable<PolygonPoint,WadFlagsPC>
	{
		//public PolygonPoint prev;
		//public PolygonPoint next;
		public int X;
		public int Z;
		public ushort wField2;
		public short wField3;
		public int someY;
		public int gapField1;

		public static PolygonPoint Parse(WadReader reader, WadFlagsPC wadFlags)
		{
			var polygonPoint = new PolygonPoint();
			polygonPoint.X = reader.Read<int>();
			polygonPoint.Z = reader.Read<int>();
			if((wadFlags & WadFlagsPC.WAD_FLAG_4000000) != 0)
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

	public sealed class PolygonStruct:IReadable<PolygonStruct,WadFlagsPC>
	{
		//public ushort pointCount;
		public PolygonFlags Flags{get;}
		public IReadOnlyList<PolygonPoint> Points{get;}

		public PolygonStruct(PolygonFlags flags, IReadOnlyList<PolygonPoint> points)
		{
			Flags = flags;
			Points = points;
		}

		public static PolygonStruct Parse(WadReader reader, WadFlagsPC wadFlags)
		{
			var polygonPointCount = reader.Read<ushort>();
			var flags = (PolygonFlags)reader.Read<ushort>();
			var points = reader.ReadArray<PolygonPoint,WadFlagsPC>(wadFlags, polygonPointCount);
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
			return new PolygonStruct(flags, points);
		}
	}

	public sealed class PolygonArray:IReadable<PolygonArray,WadFlagsPC>
	{
		//public int count;
		public IReadOnlyList<PolygonStruct> Polygons{get;}

		public PolygonArray(IReadOnlyList<PolygonStruct> polygons)
		{
			Polygons = polygons;
		}

		public static PolygonArray Parse(WadReader reader, WadFlagsPC wadFlags)
		{
			var polygonArrayCount = reader.Read<int>();
			var polygons = reader.ReadArray<PolygonStruct,WadFlagsPC>(wadFlags, polygonArrayCount);
			return new PolygonArray(polygons);
		}
	}
}
