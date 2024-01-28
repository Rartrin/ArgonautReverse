using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGTE;

namespace ArgonautReverse.WadChunks.DPSX
{
	public sealed class Door:IReadable<Door>
	{
		public uint X, Y, Z;
		public uint GotoX, GotoY, GotoZ, LevelType;
		public ushort GotoRotY;
		public ushort ThisRotY;
		public uint Fade;

		public int BackgroundObjectOffset;
		public OBJECT Background;

		public uint BackgroundAddYRotation;
		public int BackgroundHeightAdjust;

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

		private Door(){}

		public static Door Parse(WadReader parser)
		{
			var door = new Door();

			door.X = parser.Read<uint>();
			door.Y = parser.Read<uint>();
			door.Z = parser.Read<uint>();
			door.GotoX = parser.Read<uint>();
			door.GotoY = parser.Read<uint>();
			door.GotoZ = parser.Read<uint>();
			door.LevelType = parser.Read<uint>();
			door.GotoRotY = parser.Read<ushort>();
			door.ThisRotY = parser.Read<ushort>();
			door.Fade = parser.Read<uint>();

			door.BackgroundObjectOffset = parser.Read<int>();
			
			door.BackgroundAddYRotation = parser.Read<uint>();
			door.BackgroundHeightAdjust = parser.Read<int>();

			door.DrawMode = parser.Read<ushort>();
			door.MoveForward = parser.Read<short>();

			door.rt_from = parser.Read<int>();
			door.rt_val = parser.Read<int>();
			door.rt_obj_from = parser.Read<int>();
			door.rt_obj_val = parser.Read<int>();
			door.AmbientLight = parser.Read<CVECTOR>();
			door.BackColor = parser.Read<CVECTOR>();
			door.EffectFlags = parser.Read<ushort>();
			door.ReverbType = parser.Read<ushort>();
			door.MusicTrack = parser.Read<uint>();

			return door;
		}
	}
}
