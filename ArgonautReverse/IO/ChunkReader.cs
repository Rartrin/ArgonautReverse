namespace ArgonautReverse.IO
{
	public sealed class ChunkReader(WadReader wadReader, int start, int length):WadReader(wadReader.WadFile, wadReader.Configuration, wadReader.ReadVersion, wadReader.Data, start, length);
}
