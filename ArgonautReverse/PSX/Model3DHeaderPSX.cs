using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public abstract class Model3DHeaderPSX
	{
		public int n_vertices{get;protected set;}
		public int n_faces{get;protected set;}
	}

	public sealed class Model3DHeaderPSX_Object:Model3DHeaderPSX
	{
		public readonly ObjectPSX Object;

		public Model3DHeaderPSX_Object(ObjectPSX obj)
		{
			Object = obj;
			n_vertices = (int)obj.nvert;
			n_faces = (int)obj.nface;
		}

		public static Model3DHeaderPSX_Object Parse(WadReader data_in)
		{
			return new Model3DHeaderPSX_Object(ObjectPSX.Parse(data_in));
		}
	}

	public sealed class Model3DHeaderPSX_Track:Model3DHeaderPSX
	{
		public readonly TObjectPSX TrackObject;

		public Model3DHeaderPSX_Track(TObjectPSX trackObj)
		{
			TrackObject = trackObj;
			n_vertices = (int)trackObj.nvert;
			n_faces = (int)trackObj.nface;
		}

		public static Model3DHeaderPSX_Track Parse(WadReader data_in)
		{
			return new Model3DHeaderPSX_Track(TObjectPSX.Parse(data_in));
		}
	}
}