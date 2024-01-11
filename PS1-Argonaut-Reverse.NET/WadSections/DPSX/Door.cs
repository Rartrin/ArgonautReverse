using ArgonautReverse.LibGTE;

namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class Door
	{
		public uint x, y, z;
		public uint Gotox, Gotoy, Gotoz, LevelType;
		public ushort GotoRotY;
		public ushort ThisRotY;
		public uint Fade;

		public /*OBJECT*/object Background;

		public uint BackgroundAddYRotation;  //NEW
		public int BackgroundHeightAdjust;  //NEW

		public ushort DrawMode;
		public short MoveForward;

		public int rt_from;
		public int rt_val;
		public int rt_obj_from;
		public int rt_obj_val;
		public CVECTOR AmbientLight;
		public CVECTOR BackColor;
		public ushort EffectFlags;
		public ushort ReverbType;
		public uint MusicTrack;
	}
}
