namespace ArgonautReverse.OpenStratEngine
{
	public sealed class MapPieceOSE(in Vector3F pos, int rotYFx, int n)
	{
		public Vector3F Pos = pos;
		public int RotYFx = rotYFx;//Value is 0-1 in 24bit (0 to 0xFF0). Also, the lowest nibble is ignored.
		public int CellIndex = n;
		public bool bVisible = false;
		public int gapField6;
		public int field7 = 0;
	}

	public sealed class MapPieceListOSE(MapPieceOSE piece)
	{
		public MapPieceOSE Piece = piece;
		public MapPieceListOSE? Next = null;
	}
}