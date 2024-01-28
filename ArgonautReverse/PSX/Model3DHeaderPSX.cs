using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
    public abstract class Model3DHeaderPSX
    {
        public int n_vertices { get; protected set; }
        public int n_faces { get; protected set; }
        public int n_bounding_box_info { get; protected set; }
    }

    public sealed class Model3DHeaderPSX_Object : Model3DHeaderPSX
    {
        public readonly ObjectPSX Object;

        public Model3DHeaderPSX_Object(ObjectPSX obj)
        {
            Object = obj;
            n_vertices = (int)obj.nvert;
            n_faces = (int)obj.nface;
            n_bounding_box_info = obj.nfloor + obj.nceil;
            if (obj.nwall.HasValue)
            {
                n_bounding_box_info += obj.nwall.Value;
            }
        }

        public static Model3DHeaderPSX_Object Parse(WadReader data_in)
        {
            return new Model3DHeaderPSX_Object(ObjectPSX.Parse(data_in));
        }
    }

    public sealed class Model3DHeaderPSX_Track : Model3DHeaderPSX
    {
        public readonly TObjectPSX TrackObject;

        public Model3DHeaderPSX_Track(TObjectPSX trackObj)
        {
            TrackObject = trackObj;
            n_vertices = (int)trackObj.nvert;
            n_faces = (int)trackObj.nface;
            n_bounding_box_info = trackObj.nfloor + trackObj.nceil;
            if (trackObj.nwall.HasValue)
            {
                n_bounding_box_info += trackObj.nwall.Value;
            }
        }

        public static Model3DHeaderPSX_Track Parse(WadReader data_in)
        {
            return new Model3DHeaderPSX_Track(TObjectPSX.Parse(data_in));
        }
    }
}