using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGTE;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PSX
{
	public readonly struct POS:IReadable<POS>
	{
		public const int ByteSize = SVECTOR.ByteSize + VECTOR.ByteSize;//24

		public readonly SVECTOR rot;/* rotation */
		public readonly VECTOR trn;/* translation */

		private POS(SVECTOR rot, VECTOR trn)
		{
			this.rot = rot;
			this.trn = trn;
		}

		public static POS Parse(WadReader parser)
		{
			var rot = parser.Read<SVECTOR>();
			var trn = parser.Read<VECTOR>();

			return new POS(rot, trn);
		}
	}

	///<summary>Larger version of a POS with 32 bit rotation (4.20 << 16)</summary>
	public readonly struct BPOS:IReadable<BPOS>
	{
		public const int ByteSize = VECTOR.ByteSize + VECTOR.ByteSize;//32

		public readonly VECTOR brot;
		public readonly VECTOR btrn;

		private BPOS(VECTOR brot, VECTOR btrn)
		{
			this.brot = brot;
			this.btrn = btrn;
		}

		public static BPOS Parse(WadReader parser)
		{
			var brot = parser.Read<VECTOR>();
			var btrn = parser.Read<VECTOR>();

			return new BPOS(brot, btrn);
		}
	}

	public unsafe struct SMATRIX:IReadable<SMATRIX>,IConvertibleToOSE<Matrix4x4F>
	{
		public fixed short m[3*3];//Rotation
		public fixed short t[3];//Translation

		public static SMATRIX Parse(WadReader reader)
		{
			SMATRIX matrix;
			reader.ReadData(matrix.m, 3*3);
			reader.ReadData(matrix.t, 3);
			return matrix;
		}

		readonly Matrix4x4F IConvertibleToOSE<Matrix4x4F>.ToOSE()
		{
			const float fixedPointConversion = 1/4096f;
			Matrix4x4F matrix;
			matrix.rot0.X = m[0]*fixedPointConversion;
			matrix.rot0.Y = m[1]*fixedPointConversion;
			matrix.rot0.Z = m[2]*fixedPointConversion;
			matrix.data0 = 0;
			matrix.rot1.X = m[3]*fixedPointConversion;
			matrix.rot1.Y = m[4]*fixedPointConversion;
			matrix.rot1.Z = m[5]*fixedPointConversion;
			matrix.data1 = 0;
			matrix.rot2.X = m[6]*fixedPointConversion;
			matrix.rot2.Y = m[7]*fixedPointConversion;
			matrix.rot2.Z = m[8]*fixedPointConversion;
			matrix.data2 = 0;

			matrix.trans.X = t[0]*fixedPointConversion;
			matrix.trans.Y = t[1]*fixedPointConversion;
			matrix.trans.Z = t[2]*fixedPointConversion;
			matrix.scale = 0;//TODO:Should this be a 1 or 0?

			return matrix;
		}
	}
}
