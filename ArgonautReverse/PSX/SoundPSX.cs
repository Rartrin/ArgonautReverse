using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGTE;

namespace ArgonautReverse.PSX
{
	public sealed class ALAmbiencePSX:IReadable<ALAmbiencePSX>
	{
		public readonly VECTOR trn; // Position
		public readonly int start;  // Fade from distance
		public readonly int end;    // Fade to distance
		public readonly uint seqId;
		public readonly int maxVolume;
		public readonly uint Status;
		public readonly uint curVolume;

		private ALAmbiencePSX(VECTOR trn, int start, int end, uint seqId, int maxVolume, uint status, uint curVolume)
		{
			this.trn = trn;
			this.start = start;
			this.end = end;
			this.seqId = seqId;
			this.maxVolume = maxVolume;
			Status = status;
			this.curVolume = curVolume;
		}

		public static ALAmbiencePSX Parse(WadReader reader)
		{
			var trn = reader.Read<VECTOR>();
			var start = reader.Read<int>();
			var end = reader.Read<int>();
			var seqId = reader.Read<uint>();
			var maxVolume = reader.Read<int>();
			var status = reader.Read<uint>();
			var curVolume = reader.Read<uint>();

			return new ALAmbiencePSX(trn, start, end, seqId, maxVolume, status, curVolume);
		}
	}
}
