using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.WadChunks;
using ArgonautReverse.WadChunks.PC;

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
		public Vector3I Position;
		public Vector3I Goto;
		public WadFileType LevelType;

		public DoorFlags0 GotoRotY;
		public DoorFlags1 ThisRotY;
		public int Fade;

		public int BackgroundOffset;
		public StratObjectPC Background;
		
		public int BackgroundAddYRotation;
		public int BackgroundHeightAdjust;

		public int DrawMode;

		public short MoveForward;

		public int rt_from;
		public int rt_val;
		public int rt_obj_from;
		public int rt_obj_val;

		public Color32 AmbientLight;
		public ColorBGRA32 BackColor;
		
		public ushort EffectFlags;
		public ushort ReverbType;
		public int MusicTrack;

		public static DoorPC Parse(WadReader reader)
		{
			var door = new DoorPC();

			door.Position = reader.Read<Vector3I>();
			door.Goto = reader.Read<Vector3I>();
			door.LevelType = (WadFileType)reader.Read<int>();
			door.GotoRotY = (DoorFlags0)reader.Read<uint>();
			door.ThisRotY = (DoorFlags1)reader.Read<uint>();
			door.Fade = reader.Read<int>();
			door.BackgroundOffset = reader.Read<int>();
			if(door.BackgroundOffset != 0)
			{
				var stpcChunk = reader.WadFile.GetChunk(STPCChunkInfo.Instance);
				door.Background = stpcChunk.GetStratObject(door.BackgroundOffset).model;
			}
			else
			{
				door.Background = null;
			}
			door.BackgroundAddYRotation = reader.Read<int>();
			door.BackgroundHeightAdjust = reader.Read<int>();
			door.DrawMode = reader.Read<int>();
			door.MoveForward = reader.Read<short>();
			door.rt_from = reader.Read<int>();
			door.rt_val = reader.Read<int>();
			door.rt_obj_from = reader.Read<int>();
			door.rt_obj_val = reader.Read<int>();
			door.AmbientLight = reader.Read<Color32>();
			door.BackColor = reader.Read<ColorBGRA32>();
			door.EffectFlags = reader.Read<ushort>();
			door.ReverbType = reader.Read<ushort>();
			door.MusicTrack = reader.Read<int>();

			return door;
		}
	}
}
