namespace ArgonautReverse.IO
{
	public class DataReader(byte[] data, int offset, int length):BaseReader
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
		public override int Length{get;} = length;

		public int Offset{get;} = offset;

		public readonly byte[] Data = data;

		protected override void ReadRawData(Span<byte> data)
		{
			Data.AsSpan(Offset+Position, data.Length).CopyTo(data);
			Position += data.Length;
		}
	}
}