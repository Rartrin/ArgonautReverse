using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.DPSX
{
	public abstract class Model3DHeader
	{
		public int n_vertices{get;protected set;}
		public int n_faces{get;protected set;}
		public int n_bounding_box_info{get;protected set;}

		public static Model3DHeader Parse(WadReader data_in, bool track)
		{
			if(track)
			{
				return new Model3DHeader_Track(TOBJECT.Parse(data_in));
			}
			else
			{
				return new Model3DHeader_Object(OBJECT.Parse(data_in));
			}
		}
	}

	public sealed class Model3DHeader_Object:Model3DHeader
	{
		public readonly OBJECT Object;

		public Model3DHeader_Object(OBJECT obj)
		{
			Object = obj;
			n_vertices = (int)obj.nvert;
			n_faces = (int)obj.nface;
			n_bounding_box_info = obj.nfloor + obj.nceil;
			if(obj.nwall.HasValue)
			{
				n_bounding_box_info += obj.nwall.Value;
			}
		}
	}

	public sealed class Model3DHeader_Track:Model3DHeader
	{
		public readonly TOBJECT TrackObject;

		public Model3DHeader_Track(TOBJECT trackObj)
		{
			TrackObject = trackObj;
			n_vertices = (int)trackObj.nvert;
			n_faces = (int)trackObj.nface;
			n_bounding_box_info = trackObj.nfloor + trackObj.nceil;
			if(trackObj.nwall.HasValue)
			{
				n_bounding_box_info += trackObj.nwall.Value;
			}
		}
	}
}