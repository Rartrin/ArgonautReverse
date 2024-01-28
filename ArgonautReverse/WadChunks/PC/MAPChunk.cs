using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
    public sealed class MAPChunkInfo : BaseWADChunkInfo
    {
        public static MAPChunkInfo Instance = new MAPChunkInfo();

        public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
        public override string ChunkDescription => "Map data";
        public override ChunkType ChunkType => ChunkType.ID_PC_MAP;

        private MAPChunkInfo() { }

        public override BaseWadChunk Parse(WadReader reader)
        {
            var map = MapPC.Parse(reader);

            reader.AssertEndOfChunk(ChunkType);
            return new MAPChunk(this, map, reader.GetAllWadData());
        }

        public static void ReadWadChunkMAP_InitCells(MapPieceListPC cell, MapPiecePC info)
        {
            var currCell = cell;
            while (info.Pos.Y <= (double)currCell.Piece.Pos.Y)
            {
                if (currCell.Next == null)
                {
                    currCell.Next = new MapPieceListPC(info);
                    return;
                }
                currCell = currCell.Next;
            }
            if (currCell.Next != null)
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
    public sealed class MAPChunk : BaseWadChunk
    {
        public MapPC Map { get; }

        public MAPChunk(BaseWADChunkInfo info, MapPC map, byte[] data = null) : base(info, data)
        {
            Map = map;
        }
    }
}
