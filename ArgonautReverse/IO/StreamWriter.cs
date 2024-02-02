namespace ArgonautReverse.IO
{
	public class StreamWriter:BaseWriter,IDisposable
	{
		public Stream Stream{get;private set;}

		public override int Position
		{
			get => ((int)Stream.Position)-Offset;
			set => Stream.Position = value+Offset;
		}
		public override int Length => (int)Stream.Length-Offset;

		public int Offset{get;}

		private readonly bool handleStreamDisposal;

		public StreamWriter(Stream stream, int offset, bool handleStreamDisposal)
		{
			Stream = stream;
			Offset = offset;
			this.handleStreamDisposal = handleStreamDisposal;
		}

		protected override void WriteRawData(ReadOnlySpan<byte> array) => Stream.Write(array);

		public void Dispose()
		{
			if(handleStreamDisposal)
			{
				Stream?.Close();
				Stream = null;
				GC.SuppressFinalize(this);
			}
		}
	}
}
