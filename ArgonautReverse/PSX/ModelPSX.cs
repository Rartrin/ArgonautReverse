using System.Runtime.InteropServices;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGPU;
using ArgonautReverse.PSX.LibGTE;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PSX
{
	public sealed class OmniLightPSX:IReadable<OmniLightPSX>//OMNI_LIGHT
	{
		//32 bytes for finished version
		//28 bytes in demo

		public readonly VECTOR trn;     // Omni position
		public readonly CVECTOR col;    // Light color
		public readonly int fade_from;  // Fade from distance
		public readonly int fade_to;    // Fade to distance

		//Didn't exist in Demo
		public readonly uint? level;     // Light level (0 = off, 1 = full). UFx32 (20.12)

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
		public fixed ushort VertexIndices[4];

		public TVectorPSX texture;

		public static FacePSX Parse(WadReader reader)
		{
			FacePSX face;
			face.normal = reader.Read(SVECTOR.ParseWithImportantPadding);
			reader.ReadData(face.VertexIndices, 4);
			face.texture = reader.Read<TVectorPSX>();
			return face;
		}
	}

	public unsafe struct TFacePSX:IReadable<TFacePSX>
	{
		//Normally only on non-track objects. Track object only have this in the dummy and was removed in all later versions.
		public SVECTOR? normal;/* face normal for flat polys + face count */

		//TODO: These may be in a different order
		public fixed ushort VertexIndices[4];

		public TVectorPSX texture;

		public static TFacePSX Parse(WadReader reader)
		{
			TFacePSX face;
			if(reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				face.normal = reader.Read(SVECTOR.ParseWithImportantPadding);
			}
			else
			{
				face.normal = null;
			}
			reader.ReadData(face.VertexIndices, 4);
			face.texture = reader.Read<TVectorPSX>();
			return face;
		}
	}

	public abstract class BaseObjectPSX<Face> where Face:IReadable<Face>
	{
		public IReadOnlyList<SVECTOR> bbox;//[9];	/* bounding box */
		public int nvert;							/* number of vertices */
		public IReadOnlyList<SVECTOR> lvert;		/* list of vertices */
		public IReadOnlyList<SVECTOR>? lnorm;		/* list of normals */
		public int nface;							/* number of faces */
		public IReadOnlyList<Face> lface;			/* list of faces */
		public ushort nfloor;						/* number of floor collision faces */
		public ushort nceil;						/* number of ceiling collision faces */
		#region NEW_COLLISION
		public ushort? nwall;						/* number of wall collision faces */
		public ushort? pad;
		#endregion
		public IReadOnlyList<FaceCollision> lcoll;	/* list of collision faces */

		protected void BaseParse(WadReader reader)
		{
			bbox = reader.ReadArray<SVECTOR>(9);
			nvert = reader.Read<int>();
			reader.AssertRead<uint>(0);//lvert placeholder
			reader.AssertRead<uint>(0);//lnorm placeholder
			nface = reader.Read<int>();
			reader.AssertRead<uint>(0);//lface placeholder
			nfloor = reader.Read<ushort>();
			nceil = reader.Read<ushort>();

			if(nvert > 1000 || nface > 1000)
			{
				if(reader.Configuration.IgnoreWarnings)
				{
					Models3DWarning.Warn(nvert, nface);
				}
				else
				{
					throw new Models3DWarning(reader.AbsolutePosition, nvert, nface);
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
			reader.AssertRead<uint>(0);//lcoll placeholder
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

		public void ParseSetupData(WadReader reader)
		{
			lvert = reader.ReadArray(SVECTOR.ParseWithImportantPadding, nvert);
			lnorm = reader.ReadArray(SVECTOR.ParseWithImportantPadding, nvert);

			lface = reader.ReadArray<FacePSX>(nface);

			int ncoll = nfloor + nceil;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				ncoll += nwall.Value;
			}
			lcoll = reader.ReadArray<FaceCollision>(ncoll);
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

		public void ParseSetupData(WadReader reader)
		{
			lvert = reader.ReadArray(SVECTOR.ParseWithImportantPadding, nvert);

			if(reader.ReadVersion != HARRY_POTTER_1_PS1.WadVersion && reader.ReadVersion != HARRY_POTTER_2_PS1.WadVersion)
			{
				lnorm = reader.ReadArray(SVECTOR.ParseWithImportantPadding, nvert);
			}
			else
			{
				lnorm = null;
			}

			lface = reader.ReadArray<TFacePSX>(nface);

			int ncoll = nfloor + nceil;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				ncoll += nwall.Value;
			}
			lcoll = reader.ReadArray<FaceCollision>(ncoll);
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
