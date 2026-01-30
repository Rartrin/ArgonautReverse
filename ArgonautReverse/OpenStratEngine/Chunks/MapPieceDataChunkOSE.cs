using ArgonautReverse.IO;
using ArgonautReverse.PC;
using ArgonautReverse.Universal;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	public sealed class MapPieceDataChunkOSE:ChunkOSE
	{
		public override ChunkTypeOSE ChunkType => ChunkTypeOSE.MapPieceData;

		public int NumPieces;
		public int MapWidth;
		public int MapHeight;

		public IReadOnlyList<IReadOnlyList<MapPieceListOSE>> MapPieceArray;
		
		public IReadOnlyList<RotPos3Fx> Positions;
		public IReadOnlyList<IReadOnlyList<ColorBGRA32>> ColorArray;

		public MapPieceDataChunkOSE()
		{
			throw new NotImplementedException();
		}

		protected override void WriteData(ChunkWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}