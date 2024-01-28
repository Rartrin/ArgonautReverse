using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGTE;

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
}
