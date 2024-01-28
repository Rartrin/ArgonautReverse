using System.Runtime.InteropServices;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGPU;
using ArgonautReverse.PSX.LibGTE;

namespace ArgonautReverse.PSX
{
	public sealed class OmniLightPSX:IReadable<OmniLightPSX>//OMNI_LIGHT
	{
		//32 bytes for finished version
		//28 bytes in demo

		public readonly VECTOR trn;     /* omni position */
		public readonly CVECTOR col;    /* light colour */
		public readonly int fade_from;  /* fade from distance */
		public readonly int fade_to;    /* fade to distance */

		//Didn't exist in Demo
		public readonly uint? level;     /* Light level (0 = off, 1 = full). (20.12 FP) */

		private OmniLightPSX(VECTOR trn, CVECTOR col, int fade_from, int fade_to, uint? level)
		{
			this.trn = trn;
			this.col = col;
			this.fade_from = fade_from;
			this.fade_to = fade_to;
			this.level = level;
		}

		public static OmniLightPSX Parse(WadReader reader)
		{
			var trn = reader.Read<VECTOR>();
			var col = reader.Read<CVECTOR>();
			var fade_from = reader.Read<int>();
			var fade_to = reader.Read<int>();
			uint? level = null;
			if(reader.ReadVersion.IsNewerOrSame(CROC_1_PS1.WadVersion))
			{
				level = reader.Read<uint>();
			}
			return new OmniLightPSX(trn, col, fade_from, fade_to, level);
		}
	}

	public abstract class BoundPSX:IReadable<BoundPSX>
	{
		public static BoundPSX Parse(WadReader reader)
		{
			if(reader.ReadVersion.NEW_COLLISION)
			{
				return BoundPSX_NEW_COLLISION.Parse(reader);
			}
			else
			{
				return BoundPSX_OLD_COLLISION.Parse(reader);
			}
		}
	}

	public sealed class BoundPSX_NEW_COLLISION:BoundPSX
	{
		public const int ByteSize = 6;

		public sbyte nx;    // Normal X (1.0.7 FP)
		public sbyte ny;    // Normal Y (1.0.7 FP)
		public sbyte nz;    // Normal Z (1.0.7 FP)
		public byte flags;  // flags
		public short d;     // dist from object origin (1.3.12 FP)

		private BoundPSX_NEW_COLLISION(sbyte nx, sbyte ny, sbyte nz, byte flags, short d)
		{
			this.nx = nx;
			this.ny = ny;
			this.nz = nz;
			this.flags = flags;
			this.d = d;
		}

		new public static BoundPSX_NEW_COLLISION Parse(WadReader reader)
		{
			var nx = reader.Read<sbyte>();
			var ny = reader.Read<sbyte>();
			var nz = reader.Read<sbyte>();
			var flags = reader.Read<byte>();
			var d = reader.Read<short>();
			return new BoundPSX_NEW_COLLISION(nx, ny, nz, flags, d);
		}
	}
	public sealed class BoundPSX_OLD_COLLISION:BoundPSX
	{
		public const int ByteSize = 8;

		public short xm;    /* 4.12 x multiplier */
		public short zm;    /* 4.12 y multiplier */
		public int c;       /* 20.12 constant */

		private BoundPSX_OLD_COLLISION(short xm, short zm, int c)
		{
			this.xm = xm;
			this.zm = zm;
			this.c = c;
		}

		new public static BoundPSX_OLD_COLLISION Parse(WadReader reader)
		{
			var xm = reader.Read<short>();
			var zm = reader.Read<short>();
			var c = reader.Read<int>();
			return new BoundPSX_OLD_COLLISION(xm, zm, c);
		}
	}

	public sealed class FaceCollPSX:IReadable<FaceCollPSX>//FACE_COLL
	{
		public const byte COLL_QUAD = 1 << 0;
		public const byte COLL_EQN = 1 << 1;
		public const byte COLL_CEIL = 1 << 2;
		public const byte COLL_XXXX = 1 << 3;//not used (was _WALL)
		public const byte COLL_STICK2FLOOR = 1 << 4;
		public const byte COLL_SLIDE = 1 << 5;
		public const byte COLL_NOHANG = 1 << 6;

		public byte flags;/* COLL_FLAT, COLL_EQN, COLL_QUAD etc. */
		public byte surface;


		#region OLD_COLLISION
		public ushort? eflags;/* edge flags for vertical surfaces */
		#endregion

		//These field were in opposite order on OLD_COLLISION
		public BoundPSX plane;/* plane equation */
		public IReadOnlyList<BoundPSX> boundary;//[4];/* for face boundary check */

		private FaceCollPSX(byte flags, byte surface, ushort? eflags, BoundPSX plane, IReadOnlyList<BoundPSX> boundary)
		{
			this.flags = flags;
			this.surface = surface;
			this.eflags = eflags;
			this.plane = plane;
			this.boundary = boundary;
		}

		public static FaceCollPSX Parse(WadReader reader)
		{
			var flags = reader.Read<byte>();
			var surface = reader.Read<byte>();
			ushort? eflags;
			BoundPSX plane;
			IReadOnlyList<BoundPSX> boundary;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				eflags = null;
				plane = reader.Read<BoundPSX>();
				boundary = reader.ReadArray<BoundPSX>(4);
			}
			else
			{
				eflags = reader.Read<ushort>();
				boundary = reader.ReadArray<BoundPSX>(4);
				plane = reader.Read<BoundPSX>();
			}
			return new FaceCollPSX(flags, surface, eflags, plane, boundary);
		}
	}

	public readonly struct TVectorPSX:IReadable<TVectorPSX>
	{
		public readonly ushort tex_no;
		public readonly ushort flags;

		private TVectorPSX(ushort tex_no, ushort flags)
		{
			this.tex_no = tex_no;
			this.flags = flags;
		}

		public static TVectorPSX Parse(WadReader reader)
		{
			var tex_no = reader.Read<ushort>();
			var flags = reader.Read<ushort>();
			return new TVectorPSX(tex_no, flags);
		}
	}

	public unsafe struct FacePSX:IReadable<FacePSX>
	{
		public SVECTOR normal;/* face normal for flat polys + face count */

		//TODO: These may be in a different order
		public fixed ushort vertex[4];/* vertex indices */

		public TVectorPSX texture;

		public static FacePSX Parse(WadReader reader)
		{
			var face = new FacePSX();
			face.normal = reader.Read(SVECTOR.ParseWithImportantPadding);
			reader.ReadData(face.vertex, 4);
			reader.Read(out face.texture);
			return face;
		}
	}

	public unsafe struct TFacePSX:IReadable<TFacePSX>
	{
		//Normal field existed in the dummy but was removed in all later version
		public SVECTOR? normal;/* face normal for flat polys + face count */

		//TODO: These may be in a different order
		public fixed ushort vertex[4];/* vertex indices */

		public TVectorPSX texture;

		public static TFacePSX Parse(WadReader reader)
		{
			var face = new TFacePSX();
			if(reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				face.normal = reader.Read(SVECTOR.ParseWithImportantPadding);
			}
			reader.ReadData(face.vertex, 4);
			reader.Read(out face.texture);
			return face;
		}
	}
	public abstract class BaseObjectPSX<FaceType> where FaceType : IReadable<FaceType>
	{
		public IReadOnlyList<SVECTOR> bbox;//[9];	/* bounding box */
		public uint nvert;                          /* number of vertices */
		public IReadOnlyList<SVECTOR> lvert;        /* list of vertices */
		public IReadOnlyList<SVECTOR> lnorm;        /* list of normals */
		public uint nface;                          /* number of faces */
		public IReadOnlyList<FaceType> lface;       /* list of faces */
		public ushort nfloor;                       /* number of floor collision faces */
		public ushort nceil;                        /* number of ceiling collision faces */
		#region NEW_COLLISION
		public ushort? nwall;                       /* number of wall collision faces */
		public ushort? pad;
		#endregion
		public IReadOnlyList<FaceCollPSX> lcoll;        /* list of collision faces */

		protected void BaseParse(WadReader reader)
		{
			bbox = reader.ReadArray<SVECTOR>(9);
			nvert = reader.Read<uint>();
			var lvertPlaceholder = reader.Read<int>();
			if(lvertPlaceholder != 0) { throw new Exception(); }
			var lnormPlaceholder = reader.Read<int>();
			if(lnormPlaceholder != 0) { throw new Exception(); }
			nface = reader.Read<uint>();
			var lfacePlaceholder = reader.Read<int>();
			if(lfacePlaceholder != 0) { throw new Exception(); }
			nfloor = reader.Read<ushort>();
			nceil = reader.Read<ushort>();

			if(nvert > 1000 || nface > 1000)
			{
				if(reader.Configuration.IgnoreWarnings)
				{
					Models3DWarning.Warn((int)nvert, (int)nface);
				}
				else
				{
					throw new Models3DWarning(reader.AbsolutePosition, (int)nvert, (int)nface);
				}
			}

			if(reader.ReadVersion.NEW_COLLISION)
			{
				nwall = reader.Read<ushort>();
				pad = reader.Read<ushort>();
			}
			else
			{
				nwall = null;
				pad = null;
			}
			var lcollPlaceholder = reader.Read<int>();
			if(lcollPlaceholder != 0) { throw new Exception(); }
		}

		public void ParseSetupData(WadReader reader, bool track)
		{
			var vertices = new SVECTOR[nvert];
			for(int v = 0; v < nvert; v++)
			{
				vertices[v] = reader.Read(SVECTOR.ParseWithImportantPadding);
			}
			lvert = vertices;

			if(!track || reader.ReadVersion != HARRY_POTTER_1_PS1.WadVersion && reader.ReadVersion != HARRY_POTTER_2_PS1.WadVersion)
			{
				var normals = new SVECTOR[nvert];
				for(int v = 0; v < nvert; v++)
				{
					normals[v] = reader.Read(SVECTOR.ParseWithImportantPadding);
				}
				lnorm = normals;
			}
			else
			{
				lnorm = null;
			}

			lface = reader.ReadArray<FaceType>((int)nface);

			int ncoll = nfloor + nceil;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				ncoll += nwall.Value;
			}
			lcoll = reader.ReadArray<FaceCollPSX>(ncoll);
		}
	}

	public sealed class ObjectPSX:BaseObjectPSX<FacePSX>, IReadable<ObjectPSX>
	{
		public static ObjectPSX Parse(WadReader reader)
		{
			var ret = new ObjectPSX();
			ret.BaseParse(reader);
			return ret;
		}
	}

	public sealed class TObjectPSX:BaseObjectPSX<TFacePSX>, IReadable<TObjectPSX>
	{
		public static TObjectPSX Parse(WadReader reader)
		{
			var ret = new TObjectPSX();
			ret.BaseParse(reader);
			return ret;
		}
	}

	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	public struct PolyAllPSX:IReadable<PolyAllPSX>//POLY_ALL//union
	{
		[FieldOffset(0)] public uint Tag;

		[FieldOffset(0)] public TILE_1 tile1;
		[FieldOffset(0)] public TILE tile;
		[FieldOffset(0)] public SPRT sprt;
		[FieldOffset(0)] public POLY_F3 pf3;
		[FieldOffset(0)] public POLY_FT3 pft3;
		[FieldOffset(0)] public POLY_F4 pf4;
		[FieldOffset(0)] public POLY_FT4 pft4;
		[FieldOffset(0)] public POLY_G3 pg3;
		[FieldOffset(0)] public POLY_GT3 pgt3;
		[FieldOffset(0)] public POLY_G4 pg4;
		[FieldOffset(0)] public POLY_GT4 pgt4;
		[FieldOffset(0)] public DR_TPAGE drtpage;
		[FieldOffset(0)] public DR_MODE drmode;
		[FieldOffset(0)] public DR_MOVE drmove;
		//DR_LOAD  drload;

		public static unsafe PolyAllPSX Parse(WadReader reader)
		{
			if(sizeof(PolyAllPSX) != sizeof(int) * 13)
			{
				throw new NotImplementedException();
			}
			return reader.ReadData<PolyAllPSX>();
		}
	}

	public struct PreLitPSX//PRE_LIT
	{
		public IReadOnlyList<PolyAllPSX> lface;
	}
}
