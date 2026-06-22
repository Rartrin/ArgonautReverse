using System.Text;

namespace ArgonautReverse.WadChunks
{
	public enum ChunkType:uint
	{
		Unknown = 0,
		#region PC Chunks
		ID_PC_INFO		= (('I' << 24) + ('N' << 16) + ('F' << 8) + 'O'),
		ID_PC_VERSION	= (('V' << 24) + ('E' << 16) + ('R' << 8) + 'S'),
		ID_PC_MAP		= (('M' << 24) + ('A' << 16) + ('P' << 8) + ' '),
		ID_PC_TRACK		= (('T' << 24) + ('R' << 16) + ('A' << 8) + 'K'),
		ID_PC_TEXT		= (('T' << 24) + ('E' << 16) + ('X' << 8) + 'T'),
		ID_PC_LIGHT		= (('L' << 24) + ('G' << 16) + ('H' << 8) + 'T'),
		ID_PC_STRAT		= (('S' << 24) + ('T' << 16) + ('P' << 8) + 'C'),
		ID_PC_WADFLAGS	= (('W' << 24) + ('F' << 16) + ('P' << 8) + 'C'),

		ID_PC_SAMPLE	= (('S' << 24) + ('M' << 16) + ('P' << 8) + 'C'),
		ID_PC_LANG		= (('L' << 24) + ('G' << 16) + ('P' << 8) + 'C'),

		ID_PC_AMPC		= (('A' << 24) + ('M' << 16) + ('P' << 8) + 'C'),
		ID_PC_FONT		= (('F' << 24) + ('O' << 16) + ('N' << 8) + 'T'),
		ID_PC_SPRITE	= (('S' << 24) + ('P' << 16) + ('R' << 8) + 'T'),
		ID_PC_RIMG		= (('R' << 24) + ('I' << 16) + ('M' << 8) + 'G'),

		//TENG = The Emperor's New Groove
		#region Found in: TENG, Aladdin
		ID_PC_NAME		= (('N' << 24) + ('A' << 16) + ('M' << 8) + 'E'),
		ID_PC_SRPC		= (('S' << 24) + ('R' << 16) + ('P' << 8) + 'C'),
		ID_PC_LNFO		= (('L' << 24) + ('N' << 16) + ('F' << 8) + 'O'),
		#endregion
		#region Found in: Aladdin
		ID_PC_PRLT		= (('P' << 24) + ('R' << 16) + ('L' << 8) + 'T'),
		#endregion
		#endregion

		#region PSX Chunks
		//CWAD indicated the wad is compressed
		ID_PSX_CWAD = (('C' << 24) + ('W' << 16) + ('A' << 8) + 'D'),
		
		ID_PSX_TEXT		= (('T' << 24) + ('P' << 16) + ('S' << 8) + 'X'),
		ID_PSX_SAMPLE	= (('S' << 24) + ('P' << 16) + ('S' << 8) + 'X'),
		ID_PSX_DATA		= (('D' << 24) + ('P' << 16) + ('S' << 8) + 'X'),

		#region Found in: Harry Potter 1 PSX
		ID_PSX_LANG		= (('L' << 24) + ('P' << 16) + ('S' << 8) + 'X'),
		ID_PSX_PORT		= (('P' << 24) + ('O' << 16) + ('R' << 8) + 'T'),
		ID_PSX_UNIF		= (('U' << 24) + ('N' << 16) + ('I' << 8) + 'F'),
		#endregion
		
		#endregion

		#region Universal Chunks
		ID_END			= (('E' << 24) + ('N' << 16) + ('D' << 8) + ' '),
		#endregion
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
				retBytes[3-i] = typeBytes[i];
			}

			return Encoding.ASCII.GetString(retBytes);
		}
	}
}
