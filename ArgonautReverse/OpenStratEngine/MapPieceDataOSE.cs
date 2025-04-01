namespace ArgonautReverse.OpenStratEngine
{
	public sealed class MapPieceOSE
	{
		public Vector3F Pos;
		public int RotYFx;//Value is 0-1 in 24bit (0 to 0xFF0). Also, the lowest nibble is ignored.
		public int CellIndex;
		public bool bVisible = false;
		public int gapField6;
		public int field7 = 0;

		public MapPieceOSE(in Vector3F pos, int rotYFx, int n)
		{
			Pos = pos;
			RotYFx = rotYFx;
			CellIndex = n;
		}
	}

	public sealed class MapPieceListOSE
	{
		public MapPieceOSE Piece;
		public MapPieceListOSE Next;

		public MapPieceListOSE(MapPieceOSE piece)
		{
			Piece = piece;
		}
	}
}
