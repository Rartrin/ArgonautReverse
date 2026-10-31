using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine;
using ArgonautReverse.PSX.LibGTE;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PSX
{
	public sealed class DoorPSX:IReadable<DoorPSX>,IWritable,IConvertibleToOSE<DoorOSE>
	{
		public uint X, Y, Z;
		public uint GotoX, GotoY, GotoZ, LevelType;
		public ushort GotoRotY;
		public ushort ThisRotY;
		public uint Fade;

		public int BackgroundObjectOffset;
		public ObjectPSX Background;

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

		private DoorPSX(){}

		public static DoorPSX Parse(WadReader parser)
		{
			var door = new DoorPSX();

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

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<uint>(X);
			writer.Write<uint>(Y);
			writer.Write<uint>(Z);
			writer.Write<uint>(GotoX);
			writer.Write<uint>(GotoY);
			writer.Write<uint>(GotoZ);
			writer.Write<uint>(LevelType);
			writer.Write<ushort>(GotoRotY);
			writer.Write<ushort>(ThisRotY);
			writer.Write<uint>(Fade);

			writer.Write<int>(BackgroundObjectOffset);

			writer.Write<uint>(BackgroundAddYRotation);
			writer.Write<int>(BackgroundHeightAdjust);

			writer.Write<ushort>(DrawMode);
			writer.Write<short>(MoveForward);

			writer.Write<int>(rt_from);
			writer.Write<int>(rt_val);
			writer.Write<int>(rt_obj_from);
			writer.Write<int>(rt_obj_val);
			writer.Write<CVECTOR>(AmbientLight);
			writer.Write<CVECTOR>(BackColor);
			writer.Write<ushort>(EffectFlags);
			writer.Write<ushort>(ReverbType);
			writer.Write<uint>(MusicTrack);
		}

		public DoorOSE ToOSE()
		{
			var door = new DoorOSE();
			door.Position = new Vector3I((int)X, (int)Y, (int)Z);
			door.Goto = new Vector3I((int)GotoX, (int)GotoY, (int)GotoZ);
			door.LevelType = (WadFileType)LevelType;
			door.GotoRotY = (DoorRotOSE)GotoRotY;
			door.ThisRotY = (DoorRotOSE)ThisRotY;
			door.Fade = Fade;

			//door.BackgroundObjectIndex = ;

			door.BackgroundAddYRotation = BackgroundAddYRotation;
			door.BackgroundHeightAdjust = BackgroundHeightAdjust;

			door.DrawMode = DrawMode;
			door.MoveForward = MoveForward;

			door.rt_from = rt_from;
			door.rt_val = rt_val;
			door.rt_obj_from = rt_obj_from;
			door.rt_obj_val = rt_obj_val;
			door.AmbientLight = new ColorBGRA32
			(
				AmbientLight.R,
				AmbientLight.G,
				AmbientLight.B,
				AmbientLight.CD
			);
			door.BackColor = new ColorBGRA32
			(
				AmbientLight.R,
				AmbientLight.G,
				AmbientLight.B,
				AmbientLight.CD
			);
			door.EffectFlags = EffectFlags;
			door.ReverbType = ReverbType;
			door.MusicTrack = MusicTrack;

			return door;
		}
	}
}
