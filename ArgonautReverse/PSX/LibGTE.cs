using ArgonautReverse.IO;

namespace ArgonautReverse.PSX.LibGTE
{
	public readonly struct MATRIX:IReadable<MATRIX>
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
	}

	/// <summary>Long (int32) word type 3D vector</summary>
	public readonly struct VECTOR:IReadable<VECTOR>
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
			var vx = parser.Read<int>();
			var vy = parser.Read<int>();
			var vz = parser.Read<int>();
			var pad = parser.Read<int>();

			if(pad != 0)
			{
				throw new Exception("VECTOR padding is not 0");
			}

			return new VECTOR(vx, vy, vz, pad);
		}
	}
	
	/// <summary>Short word type 3D vector</summary>
	public readonly struct SVECTOR:IReadable<SVECTOR>,IConvertibleOSE<Vector3I>
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
		
		/// <summary>Parse the data but allow the padding to have non-zero values</summary>
		public static SVECTOR ParseWithImportantPadding(WadReader parser)
		{
			var vx = parser.Read<short>();
			var vy = parser.Read<short>();
			var vz = parser.Read<short>();
			var pad = parser.Read<short>();
			
			return new SVECTOR(vx, vy, vz, pad);
		}

		readonly Vector3I IConvertibleOSE<Vector3I>.ToOSE()
		{
			if(pad != 0)
			{
				throw new Exception("SVECTOR padding is not 0");
			}
			return new Vector3I(vx, vy, vz);
		}
	}

	/// <summary>Color type Vector</summary>
	public readonly struct CVECTOR:IReadable<CVECTOR>
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
	}

}