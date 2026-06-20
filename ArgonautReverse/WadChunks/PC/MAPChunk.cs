using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class MAPChunkInfo:BaseWADChunkInfo<MAPChunk>
	{
		public static readonly MAPChunkInfo Instance = new MAPChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.ParsableWadsPC;
		public override string ChunkDescription => "Map data";
		public override ChunkType ChunkType => ChunkType.ID_PC_MAP;

		private MAPChunkInfo(){}

		public override MAPChunk Parse(WadReader reader)
		{
			var map = reader.Read<MapPC>();
			reader.AssertEndOfChunk(ChunkType, true);
			return new MAPChunk(this, map, reader.GetAllWadData());
		}

		public static void ReadWadChunkMAP_InitCells(MapPieceListPC cell, MapPiecePC info)
		{
			var currCell = cell;
			while(info.Pos.Y <= (double)currCell.Piece.Pos.Y)
			{
				if(currCell.Next == null)
				{
					currCell.Next = new MapPieceListPC(info);
					return;
				}
				currCell = currCell.Next;
			}
			if(currCell.Next != null)
			{
				ReadWadChunkMAP_InitCells(currCell.Next, currCell.Piece);
				currCell.Piece = info;
			}
			else
			{
				currCell.Next = new MapPieceListPC(currCell.Piece);
				currCell.Piece = info;
			}
		}
	}
	public sealed class MAPChunk(BaseWADChunkInfo info, MapPC map, byte[]? data = null):BaseWadChunk(info, data)
	{
		public readonly MapPC Map = map;

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write(Map);
		}
	}
}