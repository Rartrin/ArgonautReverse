using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PC
{
	enum CameraType:ushort
	{
		Normal= 0,
		Repel = 1,
		Corridor = 2,
		CorridorEnd = 3,
		CorridorStart = 4,
		RopePoint = 5,
		AnyHeight = 6,
		Sticky = 7,
		Center = 8,
		Radius = 9,
		AnyHeightBelow = 10,
		OnlyThisHeight = 11,
		TestHeight = 12,
		Popup = 13,
		Pad0 = 14,
	}

	public enum CameraPointFlags:ushort
	{
		None = 0,
		Normal = 1<<CameraType.Normal,//0x1
		Repel = 1<<CameraType.Repel,//0x2
		Corridor = 1<<CameraType.Corridor,//0x4
		CorridorEnd = 1<<CameraType.CorridorEnd,//0x8
		CorridorStart = 1<<CameraType.CorridorStart,//0x10
		RopePoint = 1<<CameraType.RopePoint,//0x20
		AnyHeight = 1<<CameraType.AnyHeight,//0x40
		Sticky = 1<<CameraType.Sticky,//0x80
		Center = 1<<CameraType.Center,//0x100
		Radius = 1<<CameraType.Radius,//0x200
		AnyHeightBelow = 1<<CameraType.AnyHeightBelow,//0x400
		OnlyThisHeight = 1<<CameraType.OnlyThisHeight,//0x800
		TestHeight = 1<<CameraType.TestHeight,//0x1000
		Popup = 1<<CameraType.Popup,//0x2000
		Pad0 = 1<<CameraType.Pad0,//0x4000
	}

	public sealed class CameraPointPC:IReadable<CameraPointPC,WadFlagPC>,IWritable<WadFlagPC>
	{
		//public CameraPointPC Prev;
		//public CameraPointPC Next;
		public Fixed32 X;
		public Fixed32 Z;
		public ushort Type;
		public ushort Angle;
		public Fixed32 Y;
		//public int __padding;

		public static CameraPointPC Parse(WadReader reader, WadFlagPC wadFlags)
		{
			var cameraPoint = new CameraPointPC();
			cameraPoint.X = reader.Read<Fixed32>();
			cameraPoint.Z = reader.Read<Fixed32>();
			if((wadFlags & WadFlagPC.WAD_FLAG_4000000) != 0)
			{
				cameraPoint.Y = reader.Read<Fixed32>();
			}
			else
			{
				cameraPoint.Y = Fixed32.Zero;
			}
			cameraPoint.Type = reader.Read<ushort>();
			cameraPoint.Angle = reader.Read<ushort>();
			return cameraPoint;
		}

		public void Write(WadWriter writer, WadFlagPC wadFlags)
		{
			writer.Write<Fixed32>(X);
			writer.Write<Fixed32>(Z);
			if((wadFlags & WadFlagPC.WAD_FLAG_4000000) != 0)
			{
				writer.Write<Fixed32>(Y);
			}
			writer.Write<ushort>(Type);
			writer.Write<ushort>(Angle);
		}
	}

	public sealed class CameraLoopPC(CameraPointFlags flags, IReadOnlyList<CameraPointPC> points):IReadable<CameraLoopPC,WadFlagPC>,IWritable<WadFlagPC>
	{
		//public ushort pointCount;
		public readonly CameraPointFlags Flags = flags;
		public readonly IReadOnlyList<CameraPointPC> Points = points;

		public static CameraLoopPC Parse(WadReader reader, WadFlagPC wadFlags)
		{
			var pointCount = reader.Read<ushort>();
			var flags = (CameraPointFlags)reader.Read<ushort>();
			var points = reader.ReadArray<CameraPointPC, WadFlagPC>(wadFlags, pointCount);
			//for(int i=0; i<pointCount; i++)
			//{
			//	var point = points[i];
			//	switch(point.Type)
			//	{
			//		case 1:
			//		case 6:
			//		case 7:
			//		case 8:
			//		case 9:
			//		case 11:
			//		case 13:
			//		case 14:
			//			flags |= (CameraPointFlags)(1 << point.Type);
			//			break;
			//		case 2:
			//		case 3:
			//		case 4:
			//			flags |= CameraPointFlags.POLYGON_FLAG_4;
			//			break;
			//		case 10:
			//			flags |= CameraPointFlags.POLYGON_FLAG_400 | CameraPointFlags.POLYGON_FLAG_40;
			//			break;
			//		case 12:
			//			flags |= CameraPointFlags.POLYGON_FLAG_1000 | CameraPointFlags.POLYGON_FLAG_40;
			//			break;
			//	}
			//}
			return new CameraLoopPC(flags, points);
		}

		public void Write(WadWriter writer, WadFlagPC wadFlags)
		{
			writer.Write((ushort)Points.Count);
			writer.Write((ushort)Flags);
			writer.WriteArray(wadFlags, Points);
		}
	}

	public sealed class CameraEntryPC(IReadOnlyList<CameraLoopPC> loops):IReadable<CameraEntryPC, WadFlagPC>,IWritable<WadFlagPC>
	{
		//public int count;
		public readonly IReadOnlyList<CameraLoopPC> Loops = loops;

		public static CameraEntryPC Parse(WadReader reader, WadFlagPC wadFlags)
		{
			var loopCount = reader.Read<int>();
			var loops = reader.ReadArray<CameraLoopPC, WadFlagPC>(wadFlags, loopCount);
			return new CameraEntryPC(loops);
		}

		public void Write(WadWriter writer, WadFlagPC wadFlags)
		{
			writer.Write<int>(Loops.Count);
			writer.WriteArray(wadFlags, Loops);
		}
	}
}