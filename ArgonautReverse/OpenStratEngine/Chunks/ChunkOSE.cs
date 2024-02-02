using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	//OSE chunks exist within the OSE Chunk
	public enum ChunkTypeOSE:uint
	{
		Unknown			= 0,
		Wadflags		= ('F' << 24) + ('L' << 16) + ('A' << 8) + 'G',
		//Fonts
		//Objects
		Animations		= ('A' << 24) + ('N' << 16) + ('I' << 8) + 'M',
		//Cutscenes
		//Heads
		//Actors
		//Track Objects
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
