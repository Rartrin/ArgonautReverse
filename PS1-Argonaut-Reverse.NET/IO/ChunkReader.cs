namespace ArgonautReverse.IO
{
	public sealed class ChunkReader:WadReader
	{
		public ChunkReader(WadReader wadReader, int start, int length) : base(wadReader.WadFile, wadReader.Configuration, wadReader.ReadVersion, wadReader.Data, start, length){}
	}
}
