using ArgonautReverse.IO;
using ArgonautReverse.WadChunks;
using ArgonautReverse.WadChunks.STPC;

namespace ArgonautReverse.PC
{
	public enum DoorFlags0:uint
	{
		DOOR0_1 = 0x1,
		DOOR0_2 = 0x2,
	}

	public enum DoorFlags1:uint
	{
		DOOR_DOWN = 0x1,
		DOOR_UP = 0x2,
		DOOR_LEVEL = 0x4,
		DOOR_START = 0x8,
	}

	public sealed class DoorPC:IReadable<DoorPC>
	{
		public Vector3I position;
		public int GotoX;
		public int GotoY;
		public int GotoZ;
		public WadFileType type;
		public DoorFlags0 GotoRotY;
		public DoorFlags1 ThisRotY;
		public int Fade;
		public StratObjectPC Background;
		public int field1;
		public int width;
		public int field3;
		public short rotation;
		public short wField1;
		public int minVisableRangle;
		public int maxVisableRange;
		public int field5;
		public int field6;
		public Color32 color;
		public ColorBGRA32 d3dColor;
		public ushort field9;
		public ushort field10;
		public int musicId;

		public static DoorPC Parse(WadReader reader)
		{
			var door = new DoorPC();

			door.position = reader.Read<Vector3I>();
			door.GotoX = reader.Read<int>();
			door.GotoY = reader.Read<int>();
			door.GotoZ = reader.Read<int>();
			door.type = (WadFileType)reader.Read<int>();
			door.GotoRotY = (DoorFlags0)reader.Read<uint>();
			door.ThisRotY = (DoorFlags1)reader.Read<uint>();
			door.Fade = reader.Read<int>();
			int backgroundOffset = reader.Read<int>();
			if(backgroundOffset != 0)
			{
				//TODO: Not sure if this will work.
				//This is the byte offset within the STPC chunk data.
				var stpcChunk = reader.WadFile.GetChunk<STPCChunk>(ChunkType.ID_PC_STRAT);

				door.Background = stpcChunk.GetStratObject(backgroundOffset).model;
				//door.Background = (StratObject)&data.fileData[backgroundOffset];
			}
			else
			{
				door.Background = null;
			}
			door.field1 = reader.Read<int>();
			door.width = reader.Read<int>();
			door.field3 = reader.Read<int>();
			door.rotation = reader.Read<short>();
			door.minVisableRangle = reader.Read<int>();
			door.maxVisableRange = reader.Read<int>();
			door.field5 = reader.Read<int>();
			door.field6 = reader.Read<int>();
			door.color = reader.Read<Color32>();
			door.d3dColor = reader.Read<ColorBGRA32>();
			door.field9 = reader.Read<ushort>();
			door.field10 = reader.Read<ushort>();
			door.musicId = reader.Read<int>();

			return door;
		}
	}
}
