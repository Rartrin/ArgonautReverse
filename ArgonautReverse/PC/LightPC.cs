using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PC
{
	public enum LightType:int
	{
		Ambient = 0,
		Directional = 1,
		Point = 2,
	}
	public sealed class LightPC:IReadable<LightPC>,IWritable
	{
		public LightType Type;
		public Vector3<byte> Color;
		public Vector3F Vector;
		public float FalloffStart;
		public float Range;
		public byte maybeDepth;//Probably not depth but leaving for consistency with decompile.

		public static LightPC Parse(WadReader reader)
		{
			var type = (LightType)reader.Read<byte>();
			var color = reader.Read<Vector3<byte>>();

			Vector3F vector;
			float falloffStart;
			float range;
			byte unknown;
			switch(type)
			{
				case LightType.Directional:
				{
					vector = reader.Read<Vector3F>();
					//vector.Y = -vector.Y;
					falloffStart = 0;
					range = 0;
					unknown = 0;
					break;
				}
				case LightType.Point:
				{
					vector = reader.Read<Vector3F>();
					//vector.Z = -vector.Z;
					falloffStart = reader.Read<float>();
					range = reader.Read<float>();
					unknown = reader.Read<byte>();
					break;
				}
				default:throw new NotImplementedException($"Unknown light type: {type}");
			}
			return new LightPC
			{
				Type = type,
				Color = color,
				Vector = vector,
				FalloffStart = falloffStart,
				Range = range,
				maybeDepth = unknown,
			};
		}

		public void Write(WadWriter writer)
		{
			writer.Write((byte)Type);
			writer.Write<Vector3<byte>>(Color);
			switch(Type)
			{
				case LightType.Directional:
				{
					//vector.Y = -vector.Y;
					writer.Write<Vector3F>(Vector);
					break;
				}
				case LightType.Point:
				{
					//vector.Z = -vector.Z;
					writer.Write<Vector3F>(Vector);
					writer.Write<float>(FalloffStart);
					writer.Write<float>(Range);
					writer.Write<byte>(maybeDepth);
					break;
				}
				default:throw new NotImplementedException($"Unknown light type: {Type}");
			}
		}
	}
}