namespace ArgonautReverse.IO
{
	public class StreamWriter(MemoryStream stream, int offset):BaseWriter
	{
		public MemoryStream Stream{get;private set;} = stream;

		public override int Position
		{
			get => ((int)Stream.Position)-Offset;
			set => Stream.Position = value+Offset;
		}
		public override int Length => (int)Stream.Length-Offset;

		public int Offset{get;} = offset;

		protected override void WriteRawData(ReadOnlySpan<byte> array) => Stream.Write(array);
	}
}