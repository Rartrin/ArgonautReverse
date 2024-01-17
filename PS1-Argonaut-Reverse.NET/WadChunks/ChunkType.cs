using System.Text;

namespace ArgonautReverse.WadChunks
{
	public enum ChunkType:uint
	{
		ID_TEXTPSX		=(('T' << 24) + ('P' << 16) + ('S' << 8) + 'X'),
		ID_SAMPLEPSX	=(('S' << 24) + ('P' << 16) + ('S' << 8) + 'X'),
		ID_DATAPSX		=(('D' << 24) + ('P' << 16) + ('S' << 8) + 'X'),
		ID_END			=(('E' << 24) + ('N' << 16) + ('D' << 8) + ' '),

		//Unknown Chunks
		ID_PORT			=(('P' << 24) + ('O' << 16) + ('R' << 8) + 'T'),
	}

	public static class ChunkTypeExtensions
	{
		public static unsafe string GetRawName(this ChunkType type)
		{
			Span<byte> retBytes = stackalloc byte[4];
			
			byte* typeBytes = (byte*)&type;
			//Names are in reverse order
			for(int i=0; i<4; i++)
			{
				retBytes[i-3] = typeBytes[i];
			}

			return Encoding.ASCII.GetString(retBytes);
		}
	}
}
