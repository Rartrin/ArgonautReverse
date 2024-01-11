using System.Numerics;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class Model3DData:BaseDataClass
	{
		public const int vertex_size = 8;
		public const int face_size = 20;
		public const int chunk_face_size = 12;
		public const string mtl_header = "mtllib {0}.MTL\nusemtl mtl1\ns off\n";

		public readonly Model3DHeader header;
		public readonly bool is_world_model_3d;
		public readonly IReadOnlyList<IReadOnlyList<Vector3>> vertices;
		public readonly IReadOnlyList<IReadOnlyList<Vector3>> normals;
		public readonly IReadOnlyList<int[]> quads;
		public readonly IReadOnlyList<Vector3> tris;
		public readonly IReadOnlyList<Vector3> faces_normals;
		public readonly IReadOnlyList<int> faces_texture_ids;
		public readonly int n_vertices_groups;

		public Model3DData(
			Model3DHeader header,
			bool is_world_model_3d,
			IReadOnlyList<IReadOnlyList<Vector3>> vertices,
			IReadOnlyList<IReadOnlyList<Vector3>> normals,
			IReadOnlyList<int[]> quads,
			IReadOnlyList<Vector3> tris,
			IReadOnlyList<Vector3> faces_normals,
			IReadOnlyList<int> faces_texture_ids,
			int n_vertices_groups)
		{
			//header parameter is needed for chunks 3D models, where the headers are separated from the 3D model data.
			this.header = header;
			this.is_world_model_3d = is_world_model_3d;
			this.vertices = vertices;
			this.normals = normals;
			this.quads = quads;
			this.tris = tris;
			this.faces_normals = faces_normals;
			this.faces_texture_ids = faces_texture_ids;
			this.n_vertices_groups = n_vertices_groups;
		}

		public int n_vertices => this.header.n_vertices;

		public int n_faces => this.header.n_faces;

		public int n_bounding_box_info => this.header.n_bounding_box_info;

		public static Model3DData Parse(WadReader data_in, Model3DHeader header, bool is_world_model_3d)
		{
			static IReadOnlyList<IReadOnlyList<Vector3>> parse_vertices_normals(WadReader data_in, Model3DHeader header, int mode)
			{
				var res = new List<Vector3[]>();
				var group = new List<Vector3>();
				for(int v=0; v<header.n_vertices; v++)
				{
					// Vertices
					var xyz = new Vector3
					(
						data_in.ReadInt16(),//Signed
						data_in.ReadInt16(),//Signed
						data_in.ReadInt16()//Signed
					);
					group.Add(xyz);
					ushort index = data_in.ReadUInt16();
					if(index < 1)
					{
						var error_cause = (mode == 0) ? NegativeIndexError.CAUSE_VERTEX : NegativeIndexError.CAUSE_VERTEX_NORMAL;
						throw new NegativeIndexError(data_in.Position, error_cause, index, xyz);
					}
					else if(index == 1)
					{
						res.Add(group.ToArray());
						group = new List<Vector3>();
					}
				}
				if(group.Count>0)
				{
					res.Add(group.ToArray());
				}
				return res.ToArray();
			}
			var vertices = parse_vertices_normals(data_in, header, 0);
			var n_vertices_groups = vertices.Count;
			IReadOnlyList<IReadOnlyList<Vector3>> normals;

			if(!(is_world_model_3d && (data_in.Version==HARRY_POTTER_1_PS1.Instance || data_in.Version==HARRY_POTTER_2_PS1.Instance)))
			{
				normals = parse_vertices_normals(data_in, header, 1);
				var n_normals_groups = normals.Count;

				if(n_vertices_groups != n_normals_groups)
				{
					throw new VerticesNormalsGroupsMismatch(n_vertices_groups, n_normals_groups, data_in.Position);
				}
			}
			else
			{
				normals = Array.Empty<Vector3[]>();
			}
			// Faces
			var quads = new List<int[]>();
			var tris = new List<Vector3>();
			var faces_normals = new List<Vector3>();
			var faces_texture_ids = new List<int>();
			if ((data_in.Version == CROC_2_DEMO_PS1_DUMMY.Instance) || !is_world_model_3d)
			{
				// Large face headers (Actors' models)
				for(int face_id=0; face_id<header.n_faces; face_id++)
				{
					var raw_face_data0 = data_in.ReadInt16();//Signed
					var raw_face_data1 = data_in.ReadInt16();//Signed
					var raw_face_data2 = data_in.ReadInt16();//Signed

					var raw_face_data3 = data_in.ReadUInt16();
					var raw_face_data4 = data_in.ReadUInt16();
					var raw_face_data5 = data_in.ReadUInt16();
					var raw_face_data6 = data_in.ReadUInt16();
					var raw_face_data7 = data_in.ReadUInt16();
					var raw_face_data8 = data_in.ReadUInt16();
					var raw_face_data9 = data_in.ReadUInt16();
				
					if((raw_face_data9 & 0x0800) != 0)
					{
						//TODO: Validate this
						// 1st vertex, then 2nd, 4th and 3rd, except in Croc 2 Demo Dummy WADs
						if(data_in.Version != CROC_2_DEMO_PS1_DUMMY.Instance)
						{
							// FIXME
							//quads.Add(new int[]{raw_face_data4, raw_face_data5, raw_face_data7, raw_face_data6})
							quads.Add(new int[]
							{
								raw_face_data4,
								raw_face_data5,
								raw_face_data6,
								raw_face_data7
							});
						}
						else
						{
							quads.Add(new int[]
							{
								raw_face_data4,
								raw_face_data5,
								raw_face_data6,
								raw_face_data7
							});
						}
					}
					else
					{
						// 1st vertex, then 2nd and 3rd
						tris.Add(new Vector3(raw_face_data4, raw_face_data5, raw_face_data6));
					}
					faces_normals.Add(new Vector3(raw_face_data0, raw_face_data1, raw_face_data2));
					faces_texture_ids.Add(raw_face_data8);
					if(raw_face_data3 < 1)
					{
						throw new NegativeIndexError
						(
							data_in.Position,
							NegativeIndexError.CAUSE_FACE,
							raw_face_data3,
							null//raw_face_data
						);
					}
				}
			}
			else
			{
				// Small face headers (Subchunks' models)
				for(int face_id=0; face_id<header.n_faces; face_id++)
				{
					var raw_face_data = data_in.ReadArray<ushort>(6);
					if((raw_face_data[5] & 0x0800)!=0)
					{
						quads.Add(new int[]
						{
							raw_face_data[0],
							raw_face_data[1],
							raw_face_data[2],
							raw_face_data[3]
						});
					}
					else
					{
						tris.Add(new Vector3(raw_face_data[0], raw_face_data[1], raw_face_data[2]));
					}
					faces_texture_ids.Add(raw_face_data[4]);
				}
			}
			//TODO: Determine dynamically
			int bounding_box_info_size;
			if(data_in.Version==CROC_2_PS1.Instance || data_in.Version==CROC_2_DEMO_PS1.Instance || data_in.Version==CROC_2_DEMO_PS1_DUMMY.Instance)
			{
				bounding_box_info_size = 44;
			}
			else
			{
				// Harry Potter 1 & 2
				bounding_box_info_size = 32;
			}
			data_in.Seek(header.n_bounding_box_info * bounding_box_info_size, SeekOrigin.Current);
			return new Model3DData
			(
				header,
				is_world_model_3d,
				vertices,
				normals,
				quads,
				tris,
				faces_normals,
				faces_texture_ids,
				n_vertices_groups
			);
		}

		

		/// <summary>
		/// Creates a Wavefront OBJ 3D model from 3D model information and a texture file.
		/// </summary>
		public void ToObj(
			TextWriter obj,
			string filename,
			IEnumerable<TextureData> textures = null,
			int? x = null,
			int? y = null,
			int? z = null,
			ChunkRotation? rotation = null,
			int? vertex_index_offset = null
		)
		{
			bool standalone_export;
			if (
				textures is null && x.HasValue && y.HasValue && z.HasValue && rotation is not null && vertex_index_offset.HasValue)
			{
				standalone_export = false;
			}
			else
			{
				standalone_export = true;
				vertex_index_offset = 0;
			}
			if(!standalone_export)
			{
				obj.WriteLine($"o {filename}");
			}

			// / 1024: Best value I found to correctly rescale the mesh
			var vs = this.vertices;

			if(rotation == null)
			{
				foreach(var vg in vs)
				{
					foreach(var v in vg)
					{
						obj.WriteLine($"v {v[0] / 1024.0} {v[1] / 1024.0} {v[2] / 1024.0}");
					}
				}
			}
			else if(rotation == ChunkRotation.TOP)
			{
				foreach(var vg in vs)
				{
					foreach(var v in vg)
					{
						obj.WriteLine($"v {(v[0] + x) / 1024.0} {(v[1] + y) / 1024.0} {(v[2] + z) / 1024.0}");
					}
				}
			}
			else if(rotation == ChunkRotation.RIGHT)
			{
				foreach(var vg in vs)
				{
					foreach(var v in vg)
					{
						obj.WriteLine($"v {(v[2] + x) / 1024.0} {(v[1] + y) / 1024.0} {(-v[0] + z) / 1024.0}");
					}
				}
			}
			else if(rotation == ChunkRotation.BOTTOM)
			{
				foreach(var vg in vs)
				{
					foreach(var v in vg)
					{
						obj.WriteLine($"v {(-v[0] + x) / 1024.0} {(v[1] + y) / 1024.0} {(-v[2] + z) / 1024.0}");
					}
				}
			}
			else
			{
				foreach(var vg in vs)
				{
					foreach(var v in vg)
					{
						obj.WriteLine($"v {(-v[2] + x) / 1024.0} {(v[1] + y) / 1024.0} {(v[0] + z) / 1024.0}");
					}
				}
			}
			foreach(var ng in this.normals)
			{
				foreach(var n in ng)
				{
					obj.WriteLine($"vn {n[0]} {n[1]} {n[2]}");
				}
			}

			if(standalone_export)
			{
				foreach(var texture in textures)
				{
					foreach(var coord in texture.output_coords)
					{
						obj.WriteLine($"vt {coord.X / 1024.0} {(1024 - coord.Y) / 1024.0}");
					}
				}
			}

			var vio = vertex_index_offset;
			for(int i=0; i<this.quads.Count; i++)
			{
				var v1=vio + this.quads[i][1] + 1;
				var v2=vio + this.quads[i][0] + 1;
				var v3=vio + this.quads[i][2] + 1;
				var v4=vio + this.quads[i][3] + 1;
				var t1=4 * this.faces_texture_ids[i] + 2;
				var t2=4 * this.faces_texture_ids[i] + 1;
				var t3=4 * this.faces_texture_ids[i] + 3;
				var t4=4 * this.faces_texture_ids[i] + 4;
				obj.WriteLine($"f {v1}/{t1}/{v1} {v2}/{t2}/{v2} {v3}/{t3}/{v3} {v4}/{t4}/{v4}");
			}
			var n_q = this.quads.Count;
			for(int i=0; i<this.tris.Count; i++)
			{
				var v1=vio + this.tris[i].Y + 1;
				var v2=vio + this.tris[i].X + 1;
				var v3=vio + this.tris[i].Z + 1;
				var t1=4 * this.faces_texture_ids[n_q + i] + 2;
				var t2=4 * this.faces_texture_ids[n_q + i] + 1;
				var t3=4 * this.faces_texture_ids[n_q + i] + 3;

				obj.WriteLine($"f {v1}/{t1}/{v1} {v2}/{t2}/{v2} {v3}/{t3}/{v3}");
			}
		}

		/// <summary>Creates a standalone Wavefront OBJ 3D model.</summary>
		public void ToSingleObj(TextWriter obj, string obj_filename, IEnumerable<TextureData> textures, string mtl_filename = null)
		{
			obj.Write(string.Format(mtl_header, mtl_filename ?? obj_filename));//The format coming in looks for the variable mtl_filename
			this.ToObj(obj, obj_filename, textures);
		}
		//Creates a Wavefront OBJ 3D model and appends it to an existing StringIO (used to export entire levels).
		public void ToBatchObj(TextWriter obj, string filename, int x, int y, int z, ChunkRotation rotation, int vertex_index_offset)
		{
			this.ToObj(obj, filename, null, x, y, z, rotation, vertex_index_offset);
		}
	}

	public sealed class Object3DData
	{
		public Model3DHeader Header{get;}
		public Model3DData Data{get;}

		private Object3DData(Model3DHeader header, Model3DData data)
		{
			Header = header;
			Data = data;
		}

		public static Object3DData Parse(WadReader data_in)
		{
			var header = Model3DHeader.Parse(data_in);
			var data = Model3DData.Parse(data_in, header, is_world_model_3d:false);
			return new Object3DData(header, data);
		}

		/// <summary>
		/// Returns vertices and vertices normals of this model after application of an animation frame's
		/// rotation & translation information. The model is **not** modified.
		/// </summary>
		public Object3DData Animate(AnimationData animation, int frame_id = 0)
		{
			if(Data.n_vertices_groups != animation.n_vertices_groups)
			{
				throw new IncompatibleAnimationError(Data.n_vertices_groups, animation.n_vertices_groups);
			}
			var vertices = new Vector3[Data.n_vertices_groups][];
			var normals = new Vector3[Data.n_vertices_groups][];
			for(int i=0; i<Data.n_vertices_groups; i++)
			{
				//var rotation = animation[frame_id][i][.., 0..3];
				//var translation = animation[frame_id][i].Translation;
				var transform = animation[frame_id][i];
				vertices[i] = Data.vertices[i].Select(v => Vector3.Transform(v, transform)).ToArray();//np.add(this.vertices[i].dot(rotation), translation)
				normals[i] = Data.normals[i].Select(v => Vector3.Transform(v, transform)).ToArray();//np.add(this.normals[i].dot(rotation), translation)
			}
			var newData = new Model3DData
			(
				Data.header,
				Data.is_world_model_3d,
				vertices,
				normals,
				Data.quads,
				Data.tris,
				Data.faces_normals,
				Data.faces_texture_ids,
				Data.n_vertices_groups
			);
			return new Object3DData(Header, newData);
		}
	}

	public sealed class LevelGeom3DData
	{
		public Model3DHeader Header{get;}
		public Model3DData Data{get;}

		private LevelGeom3DData(Model3DHeader header, Model3DData data)
		{
			Header = header;
			Data = data;
		}

		public static LevelGeom3DData Parse(WadReader data_in, Model3DHeader header)
		{
			var data = Model3DData.Parse(data_in, header, is_world_model_3d:true);
			return new LevelGeom3DData(header, data);
		}
	}
}