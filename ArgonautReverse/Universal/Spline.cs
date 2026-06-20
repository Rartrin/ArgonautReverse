using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
	public sealed class SplineControlPoint:IReadable<SplineControlPoint>,IWritable
	{
		public int[] unknownData;//12

		public static SplineControlPoint Parse(WadReader reader)
		{
			var controlPoint = new SplineControlPoint();
			controlPoint.unknownData = reader.ReadArray<int>(12);
			return controlPoint;
		}

		public void Write(WadWriter writer)
		{
			writer.WriteSizedArray<int>(12, unknownData);
		}
	}
	public sealed class Spline(int offset, ushort flags, SplineControlPoint[] controlPoints):IReadable<Spline,int>,IWritable
	{
		public int Offset;
		//ushort ControlPointCount;
		public readonly ushort Flags = flags;
		public readonly SplineControlPoint[] ControlPoints = controlPoints;

		public static Spline Parse(WadReader reader, int baseOffset)
		{
			var offset = reader.Offset - baseOffset;
			ushort controlPointCount = reader.Read<ushort>();
			ushort flags = reader.Read<ushort>();
			var controlPoints = reader.ReadArray<SplineControlPoint>(controlPointCount);
			return new(offset, flags, controlPoints);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<ushort>((ushort)ControlPoints.Length);
			writer.Write<ushort>(Flags);
			writer.WriteArray<SplineControlPoint>(ControlPoints);
		}
	}
}