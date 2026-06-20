using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
	public readonly struct Fixed32:IReadable<Fixed32>,IWritable//Signed 1.19.12
	{
		public static Fixed32 Zero => new(0);
		public static Fixed32 One => new(4096);
		private readonly int raw;

		private Fixed32(int raw)
		{
			this.raw = raw;
		}

		public int Raw => raw;

		public float Float => raw/4096f;
		public double Double => raw/4096.0;

		//This is NOT the same as getting the raw bits.
		public int Int => raw/4096;

		public static Fixed32 operator +(Fixed32 a, Fixed32 b) => new Fixed32(a.raw + b.raw);
		public static Fixed32 operator -(Fixed32 a, Fixed32 b) => new Fixed32(a.raw - b.raw);

		//Cast to Int64 is necessary
		//Integer division by 4096 is the same as arithmetic right shift by 12
		//public static Fixed32 operator *(Fixed32 a, Fixed32 b) => new Fixed32((int)(((long)a.raw * b.raw) / 4096));

		public static Fixed32 FromRaw(int raw) => new Fixed32(raw);
		public static Fixed32 FromInt32(int value) => new Fixed32(value << 12);

		public static Fixed32 Parse(WadReader reader)
		{
			var raw = reader.Read<int>();
			return new Fixed32(raw);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write<int>(raw);
		}

        public override string ToString() => $"Fx{ToHexString()}";
		public string ToHexString() => $"{(raw >> 12):X1}.{(raw & 0xFFF):X3}";
	}

	public readonly struct UFx32:IReadable<UFx32>,IWritable//Unsigned (20.12)
	{
		private readonly uint raw;

		private UFx32(uint raw)
		{
			this.raw = raw;
		}

		public uint Raw => raw;

		public float Float => raw/4096f;
		public double Double => raw/4096.0;

		//This is NOT the same as getting the raw bits.
		public uint UInt => raw/4096;

		public static UFx32 FromRaw(uint raw) => new UFx32(raw);
		public static UFx32 FromUInt(uint value) => new UFx32(value << 12);

		public static UFx32 Parse(WadReader reader)
		{
			var raw = reader.Read<uint>();
			return new UFx32(raw);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write<uint>(raw);
		}

        public override string ToString() => $"Fx{ToHexString()}";
		public string ToHexString() => $"{(raw >> 12):X1}.{(raw & 0xFFF):X3}";
	}

	public readonly struct Fx16:IReadable<Fx16>,IWritable//Signed 1.3.12
	{
		private readonly short raw;

		private Fx16(short raw)
		{
			this.raw = raw;
		}

		public short Raw => raw;

		public float Float => raw/4096f;
		public double Double => raw/4096.0;

		//This is NOT the same as getting the raw bits.
		public int Int => raw/4096;

		public static Fx16 FromRaw(short raw) => new Fx16(raw);
		public static Fx16 FromInt(short value) => new Fx16((short)(value << 12));

		public static Fx16 Parse(WadReader reader)
		{
			var raw = reader.Read<short>();
			return new Fx16(raw);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write<short>(raw);
		}

        public override string ToString() => $"Fx{ToHexString()}";
		public string ToHexString() => $"{(raw >> 12):X1}.{(raw & 0xFFF):X3}";
	}

	public readonly struct UFx16:IReadable<UFx16>,IWritable//Unsigned 4.12
	{
		private readonly ushort raw;

		private UFx16(ushort raw)
		{
			this.raw = raw;
		}

		public ushort Raw => raw;

		public float Float => raw/4096f;
		public double Double => raw/4096.0;

		//This is NOT the same as getting the raw bits.
		public uint UInt => (uint)(raw/4096);

		public static UFx16 FromRaw(ushort raw) => new UFx16(raw);
		public static UFx16 FromInt(ushort value) => new UFx16((ushort)(value << 12));

		public static UFx16 Parse(WadReader reader)
		{
			var raw = reader.Read<ushort>();
			return new UFx16(raw);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write<ushort>(raw);
		}

        public override string ToString() => $"Fx{ToHexString()}";
		public string ToHexString() => $"{(raw >> 12):X1}.{(raw & 0xFFF):X3}";
	}

	//TODO: PSX has 32bit padding? Maybe just Croc 2 demo build.
	public struct Vector3Fx(Fixed32 x, Fixed32 y, Fixed32 z):IReadable<Vector3Fx>,IWritable
	{
		public Fixed32 X = x;
		public Fixed32 Y = y;
		public Fixed32 Z = z;

		public static Vector3Fx Parse(WadReader reader)
		{
			var x = reader.Read<Fixed32>();
			var y = reader.Read<Fixed32>();
			var z = reader.Read<Fixed32>();
			return new Vector3Fx(x, y, z);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write<Fixed32>(X);
			writer.Write<Fixed32>(Y);
			writer.Write<Fixed32>(Z);
		}
	}

	public struct RotPos3Fx(Vector3Fx rotation, Vector3Fx position):IReadable<RotPos3Fx>,IWritable
	{
		public Vector3Fx Rotation = rotation;
		public Vector3Fx Position = position;

		public static RotPos3Fx Parse(WadReader reader)
		{
			var rotation = reader.Read<Vector3Fx>();
			var position = reader.Read<Vector3Fx>();
			return new RotPos3Fx(rotation, position);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write<Vector3Fx>(Rotation);
			writer.Write<Vector3Fx>(Position);
		}
	}
}