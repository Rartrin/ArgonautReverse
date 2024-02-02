namespace ArgonautReverse.IO
{
	public class DataReader:BaseReader
	{
		public override int Position{get;set;} = 0;

		[Obsolete]
		public int AbsolutePosition
		{
			get => Position+Offset;
			set
			{
				Position = value - Offset;
			}
		}
		public override int Length{get;}

		public int Offset{get;}

		public readonly byte[] Data;

		public DataReader(byte[] data, int offset, int length)
		{
			Data = data;
			Offset = offset;
			Length = length;
		}

		protected override void ReadRawData(Span<byte> data)
		{
			Data.AsSpan(Offset+Position, data.Length).CopyTo(data);
			Position += data.Length;
		}
	}
}
