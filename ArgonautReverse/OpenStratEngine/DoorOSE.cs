using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.OpenStratEngine
{
	public enum DoorRotOSE:uint
	{
		DOOR_DOWN = 0x1,
		DOOR_UP = 0x2,
		DOOR_LEVEL = 0x4,
		DOOR_START = 0x8,
	}
	public sealed class DoorOSE:IWritable
	{
		public Vector3I Position;
		/// <summary>
		/// When ThisRotY has DOOR_LEVEL set, the X,Y,Z instead corresponse to Tribe, Level, Map.
		/// </summary>
		public Vector3I Goto;
		public WadFileType LevelType;

		public DoorRotOSE GotoRotY;
		public DoorRotOSE ThisRotY;
		public uint Fade;

		//public int BackgroundObjectIndex;
		//public ObjectOSE Background;//Saved via object index
		
		public uint BackgroundAddYRotation;
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
		public uint MusicTrack;

		public void Write(WadWriter writer)
		{
			writer.Write(Position);
			writer.Write(Goto);
			writer.Write((uint)LevelType);
			writer.Write((uint)GotoRotY);
			writer.Write((uint)ThisRotY);
			writer.Write(Fade);

			//writer.Write((int)Background.ObjectIndex);

			writer.Write(BackgroundAddYRotation);
			writer.Write(BackgroundHeightAdjust);

			writer.Write(DrawMode);
			writer.Write(MoveForward);
			writer.Write(rt_from);
			writer.Write(rt_val);
			writer.Write(rt_obj_from);
			writer.Write(rt_obj_val);
			
			writer.Write(AmbientLight);
			writer.Write(BackColor);

			writer.Write(EffectFlags);
			writer.Write(ReverbType);
			writer.Write(MusicTrack);

			
			throw new NotImplementedException("Background");
		}
	}
}
