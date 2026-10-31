using ArgonautReverse.IO;

namespace ArgonautReverse.PSX.LibGTE
{
	public readonly struct MATRIX:IReadable<MATRIX>,IWritable
	{
		public const int ByteSize = 32;

		//3x3 Rotation matrix
		public readonly short[][] m;//[3][3];
		//2 bytes of padding here
		//Translation vector
		public readonly int[] t;//[3];

		private MATRIX(short[][] m, int[] t)
		{
			this.m = m;
			this.t = t;
		}

		public static MATRIX Parse(WadReader parser)
		{
			var m = new short[3][];
			for(int i=0; i < 3; i++)
			{
				m[i] = parser.ReadArray<short>(3);
			}
			var padding = parser.Read<short>();
			if(padding != 0)
			{
				throw new Exception("Matrix padding is not 0");
			}
			var t = parser.ReadArray<int>(3);

			return new MATRIX(m, t);
		}

		void IWritable.Write(WadWriter writer)
		{
			for(int i=0; i < 3; i++)
			{
				writer.WriteSizedArray<short>(3, m[i]);
			}
			writer.Write<short>(0);
			writer.WriteSizedArray<int>(3, t);
		}
	}

	/// <summary>Long (int32) word type 3D vector</summary>
	public readonly struct VECTOR:IReadable<VECTOR>,IWritable
	{
		public const int ByteSize = 16;

		public readonly int vx, vy;
		public readonly int vz, pad;

		private VECTOR(int vx, int vy, int vz, int pad)
		{
			this.vx=vx;
			this.vy=vy;
			this.vz=vz;
			this.pad=pad;
		}

		public static VECTOR Parse(WadReader parser)
		{
			var ret = ParseWithImportantPadding(parser);
			if(ret.pad != 0)
			{
				throw new Exception("VECTOR padding is not 0");
			}
			return ret;
		}

		public void Write(WadWriter writer)
		{
			if(pad!=0)
			{
				throw new Exception("VECTOR padding is not 0");
			}
			WriteWithImportantPadding(writer, this);
		}

		/// <summary>Parse the data but allow the padding to have non-zero values</summary>
		public static VECTOR ParseWithImportantPadding(WadReader parser)
		{
			var vx = parser.Read<int>();
			var vy = parser.Read<int>();
			var vz = parser.Read<int>();
			var pad = parser.Read<int>();
			
			return new VECTOR(vx, vy, vz, pad);
		}

		/// <summary>Write the data but allow the padding to have non-zero values</summary>
		public static void WriteWithImportantPadding(WadWriter writer, VECTOR v)
		{
			writer.Write<int>(v.vx);
			writer.Write<int>(v.vy);
			writer.Write<int>(v.vz);
			writer.Write<int>(v.pad);
		}
	}
	
	/// <summary>Short word type 3D vector</summary>
	public readonly struct SVECTOR:IReadable<SVECTOR>,IWritable,IConvertibleToOSE<Vector3I>
	{
		public const int ByteSize = 8;

		public readonly short vx, vy;
		public readonly short vz, pad;

		public short X => vx;
		public short Y => vy;
		public short Z => vz;
		public short PAD => pad;

		private SVECTOR(short vx, short vy, short vz, short pad)
		{
			this.vx=vx;
			this.vy=vy;
			this.vz=vz;
			this.pad=pad;
		}

		/// <summary>Parse the data. The Padding is required to be zero.</summary>
		public static SVECTOR Parse(WadReader parser)
		{
			var ret = ParseWithImportantPadding(parser);
			if(ret.pad!=0)
			{
				throw new Exception("SVECTOR padding is not 0");
			}
			return ret;
		}

		/// <summary>Write the data. The Padding is required to be zero.</summary>
		public void Write(WadWriter writer)
		{
			if(PAD!=0)
			{
				throw new Exception("SVECTOR padding is not 0");
			}
			WriteWithImportantPadding(writer, this);
		}
		
		/// <summary>Parse the data but allow the padding to have non-zero values</summary>
		public static SVECTOR ParseWithImportantPadding(WadReader parser)
		{
			var vx = parser.Read<short>();
			var vy = parser.Read<short>();
			var vz = parser.Read<short>();
			var pad = parser.Read<short>();
			
			return new SVECTOR(vx, vy, vz, pad);
		}

		/// <summary>Write the data but allow the padding to have non-zero values</summary>
		public static void WriteWithImportantPadding(WadWriter writer, SVECTOR v)
		{
			writer.Write<short>(v.X);
			writer.Write<short>(v.Y);
			writer.Write<short>(v.Z);
			writer.Write<short>(v.PAD);
		}

		readonly Vector3I IConvertibleToOSE<Vector3I>.ToOSE()
		{
			if(pad != 0)
			{
				throw new Exception("SVECTOR padding is not 0");
			}
			return new Vector3I(vx, vy, vz);
		}
	}

	/// <summary>Color type Vector</summary>
	public readonly struct CVECTOR:IReadable<CVECTOR>,IWritable
	{
		public const int ByteSize = 4;

		public readonly byte R,G,B,CD;

		private CVECTOR(byte r, byte g, byte b, byte cd)
		{
			R = r;
			G = g;
			B = b;
			CD = cd;
		}

		public static CVECTOR Parse(WadReader parser)
		{
			var r = parser.Read<byte>();
			var g = parser.Read<byte>();
			var b = parser.Read<byte>();
			var cd = parser.Read<byte>();

			return new CVECTOR(r, g, b, cd);
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<byte>(R);
			writer.Write<byte>(G);
			writer.Write<byte>(B);
			writer.Write<byte>(CD);
		}
	}
}