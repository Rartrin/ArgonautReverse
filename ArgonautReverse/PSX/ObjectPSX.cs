using System.Runtime.InteropServices;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGPU;
using ArgonautReverse.PSX.LibGTE;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PSX
{
	public sealed class OmniLightPSX:IReadable<OmniLightPSX>,IWritable//OMNI_LIGHT
	{
		//32 bytes after Croc 2
		//28 bytes in Croc 2.

		public readonly VECTOR trn;     // Omni position
		public readonly CVECTOR col;    // Light color
		public readonly int fade_from;  // Fade from distance
		public readonly int fade_to;    // Fade to distance

		//Didn't exist in Croc 2
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
			if(!reader.ReadVersion.IsOlderOrSame(CROC_2_PS1.WadVersion))//Added sometime after Croc 2
			{
				level = reader.Read<uint>();
			}
			return new OmniLightPSX(trn, col, fade_from, fade_to, level);
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<VECTOR>(trn);
			writer.Write<CVECTOR>(col);
			writer.Write<int>(fade_from);
			writer.Write<int>(fade_to);
			uint? level = null;
			if(!writer.WriteVersion.IsOlderOrSame(CROC_2_PS1.WadVersion))//Added sometime after Croc 2
			{
				writer.Write<uint>(level!.Value);
			}
		}
	}

	public readonly struct TVectorPSX:IReadable<TVectorPSX>,IWritable
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

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<ushort>(tex_no);
			writer.Write<ushort>(flags);
		}
	}

	public unsafe struct FacePSX:IReadable<FacePSX>,IWritable
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

		void IWritable.Write(WadWriter writer)
		{
			writer.WriteFrom<SVECTOR>(SVECTOR.WriteWithImportantPadding, normal);
			fixed(ushort* vertexIndices0 = VertexIndices)
			{
				writer.WriteData(vertexIndices0, 4);
			}
			writer.Write<TVectorPSX>(texture);
		}
	}

	public unsafe struct TFacePSX:IReadable<TFacePSX>,IWritable
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

		void IWritable.Write(WadWriter writer)
		{
			if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				writer.WriteFrom(SVECTOR.WriteWithImportantPadding, normal!.Value);
			}
			fixed(ushort* vertexIndices0 = VertexIndices)
			{
				writer.WriteData(vertexIndices0, 4);
			}
			writer.Write<TVectorPSX>(texture);
		}
	}

	public abstract class BaseObjectPSX<Face> where Face:IReadable<Face>,IWritable
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

		protected void BaseWrite(WadWriter writer)
		{
			writer.WriteSizedArray<SVECTOR>(9, bbox);
			writer.Write<int>(nvert);
			writer.Write<uint>(0);//lvert placeholder
			writer.Write<uint>(0);//lnorm placeholder
			writer.Write<int>(nface);
			writer.Write<uint>(0);//lface placeholder
			writer.Write<ushort>(nfloor);
			writer.Write<ushort>(nceil);

			if(writer.WriteVersion.NEW_COLLISION)
			{
				writer.Write<ushort>(nwall!.Value);
				writer.Write<ushort>(pad!.Value);
			}
			writer.Write<uint>(0);//lcoll placeholder
		}
	}

	public sealed class ObjectPSX:BaseObjectPSX<FacePSX>,IReadable<ObjectPSX>,IWritable
	{
		public static ObjectPSX Parse(WadReader reader)
		{
			var ret = new ObjectPSX();

			ret.BaseParse(reader);

			ret.lvert = reader.ReadArray(SVECTOR.ParseWithImportantPadding, ret.nvert);
			ret.lnorm = reader.ReadArray(SVECTOR.ParseWithImportantPadding, ret.nvert);

			ret.lface = reader.ReadArray<FacePSX>(ret.nface);

			int ncoll = ret.nfloor + ret.nceil;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				ncoll += ret.nwall!.Value;
			}
			ret.lcoll = reader.ReadArray<FaceCollision>(ncoll);
			return ret;
		}

		public void Write(WadWriter writer)
		{
			BaseWrite(writer);
			writer.WriteFromArray(SVECTOR.WriteWithImportantPadding, lvert);
			writer.WriteFromArray(SVECTOR.WriteWithImportantPadding, lnorm!);
			writer.WriteArray<FacePSX>(lface);
			writer.WriteArray<FaceCollision>(lcoll);
		}
	}

	public sealed class TObjectPSX:BaseObjectPSX<TFacePSX>,IReadableArrayMultipass<TObjectPSX>,IWritableArrayMultipass
	{
		public static TObjectPSX ParseStruct(WadReader reader)
		{
			var ret = new TObjectPSX();
			ret.BaseParse(reader);
			return ret;
		}

		public void WriteStruct(WadWriter writer)
		{
			BaseWrite(writer);
		}

		public static void ParseData(WadReader reader, TObjectPSX obj)
		{
			obj.lvert = reader.ReadArray(SVECTOR.ParseWithImportantPadding, obj.nvert);

			if(reader.ReadVersion != HARRY_POTTER_1_PS1.WadVersion && reader.ReadVersion != HARRY_POTTER_2_PS1.WadVersion)
			{
				obj.lnorm = reader.ReadArray(SVECTOR.ParseWithImportantPadding, obj.nvert);
			}
			else
			{
				obj.lnorm = null;
			}

			obj.lface = reader.ReadArray<TFacePSX>(obj.nface);

			int ncoll = obj.nfloor + obj.nceil;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				ncoll += obj.nwall!.Value;
			}
			obj.lcoll = reader.ReadArray<FaceCollision>(ncoll);
		}

		public void WriteData(WadWriter writer)
		{
			writer.WriteFromArray(SVECTOR.WriteWithImportantPadding, lvert);
			if(writer.WriteVersion != HARRY_POTTER_1_PS1.WadVersion && writer.WriteVersion != HARRY_POTTER_2_PS1.WadVersion)
			{
				writer.WriteFromArray(SVECTOR.WriteWithImportantPadding, lnorm!);
			}
			else
			{
				lnorm = null;
			}

			writer.WriteArray<TFacePSX>(lface);
			writer.WriteArray<FaceCollision>(lcoll);
		}
	}

	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	public struct PolyAllPSX:IReadable<PolyAllPSX>,IWritable//POLY_ALL//union
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
				throw new Exception();
			}
			return reader.ReadData<PolyAllPSX>();
		}

		unsafe void IWritable.Write(WadWriter writer)
		{
			if(sizeof(PolyAllPSX) != sizeof(int) * 13)
			{
				throw new Exception();
			}
			writer.WriteData<PolyAllPSX>(this);
		}
	}

	public struct PreLitPSX//PRE_LIT
	{
		public IReadOnlyList<PolyAllPSX> lface;
	}
}