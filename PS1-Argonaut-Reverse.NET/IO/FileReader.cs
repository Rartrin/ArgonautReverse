namespace ArgonautReverse.IO
{
	public sealed class FileReader:BaseReader,IDisposable
	{
		public Stream Stream{get;private set;}

		public override int Position
		{
			get => (int)Stream.Position;
			set => Stream.Position = value;
		}
		public override int Length => (int)Stream.Length;

		private readonly bool handleStreamDisposal;

		public FileReader(Stream stream, bool handleStreamDisposal = true)
		{
			if (stream.Length > int.MaxValue)
			{
				throw new Exception("Unsupported file size. Files can not be greater than 2GB (int.MaxValue).");
			}
			this.handleStreamDisposal = handleStreamDisposal;
			this.Stream = stream;
		}

		protected override void ReadRawData(Span<byte> array) => Stream.ReadExactly(array);

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
