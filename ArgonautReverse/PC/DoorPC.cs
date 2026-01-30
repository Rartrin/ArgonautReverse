using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.Universal;
using ArgonautReverse.WadChunks.PC;

namespace ArgonautReverse.PC
{
	public enum DoorRotYPC:uint
	{
		DOOR_DOWN = 0x1,
		DOOR_UP = 0x2,
		DOOR_LEVEL = 0x4,
		DOOR_START = 0x8,
	}

	public sealed class DoorPC:IReadable<DoorPC>,IWritable
	{
		public Vector3I Position;
		/// <summary>
		/// When ThisRotY has DOOR_LEVEL set, the X,Y,Z instead corresponse to Tribe, Level, Map.
		/// </summary>
		public Vector3I Goto;
		public WadFileType LevelType;

		public DoorRotYPC GotoRotY;
		public DoorRotYPC ThisRotY;
		public int Fade;

		public int BackgroundOffset;
		public StratObjectPC? Background;
		
		public int BackgroundAddYRotation;
		public int BackgroundHeightAdjust;

		public int DrawMode;

		public short MoveForward;

		public int rt_from;
		public int rt_val;
		public int rt_obj_from;
		public int rt_obj_val;

		public ColorBGRA32 AmbientLight;
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
			door.GotoRotY = (DoorRotYPC)reader.Read<uint>();
			door.ThisRotY = (DoorRotYPC)reader.Read<uint>();
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
			door.AmbientLight = reader.Read<ColorBGRA32>();
			door.BackColor = reader.Read<ColorBGRA32>();
			door.EffectFlags = reader.Read<ushort>();
			door.ReverbType = reader.Read<ushort>();
			door.MusicTrack = reader.Read<int>();

			return door;
		}

		public void Write(WadWriter writer)
		{
			writer.Write<Vector3I>(Position);
			writer.Write<Vector3I>(Goto);
			writer.Write((int)LevelType);
			writer.Write((int)GotoRotY);
			writer.Write((int)ThisRotY);
			writer.Write<int>(Fade);
			
			if(Background != null && BackgroundOffset == 0)
			{
				throw new NotImplementedException();
			}

			if(BackgroundOffset != 0)
			{
				if(Background == null)
				{
					throw new Exception();
				}
				writer.Write<int>(BackgroundOffset);
			}
			writer.Write<int>(BackgroundAddYRotation);
			writer.Write<int>(BackgroundHeightAdjust);
			writer.Write<int>(DrawMode);
			writer.Write<short>(MoveForward);
			writer.Write<int>(rt_from);
			writer.Write<int>(rt_val);
			writer.Write<int>(rt_obj_from);
			writer.Write<int>(rt_obj_val);
			writer.Write<ColorBGRA32>(AmbientLight);
			writer.Write<ColorBGRA32>(BackColor);
			writer.Write<ushort>(EffectFlags);
			writer.Write<ushort>(ReverbType);
			writer.Write<int>(MusicTrack);
		}
	}
}