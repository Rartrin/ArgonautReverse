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

		//@classmethod
		public static Model3DHeader parse(Parser data_in, Configuration conf)
		{
			//base.parse(data_in, conf);
			data_in.Seek(72, SeekOrigin.Current);
			var n_vertices = data_in.ReadInt32();
			data_in.Seek(8, SeekOrigin.Current);
			var n_faces = data_in.ReadInt32();

			if(n_vertices > 1000 || n_faces > 1000)
			{
				if(conf.ignore_warnings)
				{
					warnings.warn($"Too much vertices or faces ({n_vertices} vertices, {n_faces} faces). It is most probably caused by an inaccuracy in my reverse engineering of the models format.");
				}
				else
				{
					throw new Models3DWarning(data_in.Position, n_vertices, n_faces);
				}
			}
			data_in.Seek(4, SeekOrigin.Current);
			var n_bounding_box_info = (
				data_in.ReadUInt16()
				+ data_in.ReadUInt16()
				+ data_in.ReadUInt16()
			);

			if(conf.game==G.CROC_2_PS1 || conf.game==G.CROC_2_DEMO_PS1 || conf.game==G.CROC_2_DEMO_PS1_DUMMY)
			{
				data_in.Seek(2, SeekOrigin.Current);
			}
			else if(conf.game==G.HARRY_POTTER_1_PS1 || conf.game==G.HARRY_POTTER_2_PS1)
			{
				data_in.Seek(6, SeekOrigin.Current);
			}

			return new Model3DHeader(n_vertices, n_faces, n_bounding_box_info);
		}
	}
}