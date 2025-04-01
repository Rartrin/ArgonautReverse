using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	//OSE chunks exist within the OSE Chunk
	public enum ChunkTypeOSE:uint
	{
		Unknown			= 0,
		Wadflags		= ('F' << 24) + ('L' << 16) + ('A' << 8) + 'G',
		Fonts			= ('F' << 24) + ('O' << 16) + ('N' << 8) + 'T',
		Objects			= ('O' << 24) + ('B' << 16) + ('J' << 8) + ' ',
		Animations		= ('A' << 24) + ('N' << 16) + ('I' << 8) + 'M',
		//Cutscenes
		//Heads
		//Actors

		MapPieceData	= ('M' << 24) + ('P' << 16) + ('C' << 8) + 'E',

		TrackObjects	= ('T' << 24) + ('O' << 16) + ('B' << 8) + 'J',
		//
		Doors			= ('D' << 24) + ('O' << 16) + ('O' << 8) + 'R',
		//
		Waypoints		= ('W' << 24) + ('Y' << 16) + ('P' << 8) + 'T',
	}
	public abstract class ChunkOSE:IWritable
	{
		public abstract ChunkTypeOSE ChunkType{get;}

		protected abstract void WriteData(ChunkWriter writer);

		public void Write(WadWriter writer)
		{
			//Write header
			writer.WriteData(ChunkType);
			var sizePlaceholderPos = writer.Position;
			writer.Write<uint>(0);//Size placeholder

			var chunkDataStart = writer.Position;
			WriteData(writer.GetChunkWriter());
			var chunkDataEnd = writer.Position;
			
			//Fix size in header
			int size = chunkDataEnd-chunkDataStart;
			writer.Position = sizePlaceholderPos;
			writer.Write(size);
			writer.Position = chunkDataEnd;
		}
	}
}
