using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class Model3DHeader:BaseDataClass
	{
		public readonly int n_vertices;
		public readonly int n_faces;
		public readonly int n_bounding_box_info;
		public Model3DHeader(int n_vertices, int n_faces, int n_bounding_box_info)
		{
			this.n_vertices = n_vertices;
			this.n_faces = n_faces;
			this.n_bounding_box_info = n_bounding_box_info;
		}

		public static Model3DHeader Parse(WadReader data_in)
		{
			//base.parse(data_in, conf);
			data_in.Seek(72, SeekOrigin.Current);//sizeof(SVECTOR) * 9//Bounding box
			var n_vertices = data_in.ReadInt32();
			data_in.Seek(4, SeekOrigin.Current);//Placeholder for list of vertices
			data_in.Seek(4, SeekOrigin.Current);//Placeholder for list of normals
			var n_faces = data_in.ReadInt32();

			if(n_vertices > 1000 || n_faces > 1000)
			{
				if(data_in.Configuration.IgnoreWarnings)
				{
					Models3DWarning.Warn(n_vertices, n_faces);
				}
				else
				{
					throw new Models3DWarning(data_in.Position, n_vertices, n_faces);
				}
			}
			data_in.Seek(4, SeekOrigin.Current);//Placeholder for list of faces
			var n_bounding_box_info
				= data_in.ReadUInt16()	//nfloors
				+ data_in.ReadUInt16();	//nceil
				
			//TODO: Find way to determine NEW_COLLISION programmatically
			//TODO: Works with both levels and models?
			if(data_in.ReadVersion.NEW_COLLISION)
			{
				n_bounding_box_info += data_in.ReadUInt16();//nwall
				data_in.ReadUInt16();//Pad
			}
			data_in.Seek(4, SeekOrigin.Current);//List of collision faces

			return new Model3DHeader(n_vertices, n_faces, n_bounding_box_info);
		}
	}
}