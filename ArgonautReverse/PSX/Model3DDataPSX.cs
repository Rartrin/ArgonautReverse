using System.Numerics;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGTE;

namespace ArgonautReverse.PSX
{
	public sealed class Model3DDataPSX
	{
		public const string mtl_header = "mtllib {0}.MTL\nusemtl mtl1\ns off";

		public readonly Model3DHeaderPSX header;
		public readonly bool is_world_model_3d;
		public readonly IReadOnlyList<IReadOnlyList<Vector3>> vertices;
		public readonly IReadOnlyList<IReadOnlyList<Vector3>> normals;
		public readonly IReadOnlyList<int[]> quads;
		public readonly IReadOnlyList<Vector3> tris;
		public readonly IReadOnlyList<Vector3> faces_normals;
		public readonly IReadOnlyList<int> faces_texture_ids;
		public readonly int n_vertices_groups;

		public Model3DDataPSX(
			Model3DHeaderPSX header,
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

		public int n_vertices => header.n_vertices;

		public int n_faces => header.n_faces;

		public int n_bounding_box_info => header.n_bounding_box_info;

		public static unsafe Model3DDataPSX Parse<FaceType>(WadReader data_in, Model3DHeaderPSX header, BaseObjectPSX<FaceType> obj, bool is_world_model_3d) where FaceType : IReadable<FaceType>
		{
			obj.ParseSetupData(data_in, is_world_model_3d);

			static IReadOnlyList<IReadOnlyList<Vector3>> ParseVerticesNormals(BaseObjectPSX<FaceType> obj, IReadOnlyList<SVECTOR> verts_norms)
			{
				var res = new List<Vector3[]>();
				var group = new List<Vector3>();
				for(int v = 0; v < obj.nvert; v++)
				{
					// Vertices
					var xyz_pad = verts_norms[v];
					var xyz = new Vector3(xyz_pad.vx, xyz_pad.vy, xyz_pad.vz);
					group.Add(xyz);
					ushort index = (ushort)xyz_pad.pad;
					if(index < 1)
					{
						throw new NegativeIndexError(-1, "vertex/normals", index, xyz_pad);
					}
					else if(index == 1)
					{
						res.Add(group.ToArray());
						group = new List<Vector3>();
					}
				}
				if(group.Count > 0)
				{
					res.Add(group.ToArray());
				}
				return res.ToArray();
			}

			var vertices = ParseVerticesNormals(obj, obj.lvert);
			var n_vertices_groups = vertices.Count;
			IReadOnlyList<IReadOnlyList<Vector3>> normals;

			if(!is_world_model_3d || data_in.ReadVersion != HARRY_POTTER_1_PS1.WadVersion && data_in.ReadVersion != HARRY_POTTER_2_PS1.WadVersion)
			{
				normals = ParseVerticesNormals(obj, obj.lnorm);
				var n_normals_groups = normals.Count;

				if(n_vertices_groups != n_normals_groups)
				{
					throw new VerticesNormalsGroupsMismatch(n_vertices_groups, n_normals_groups, -1);
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

			var faces = obj.lface;

			if(!is_world_model_3d/*|| (data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)*/)
			{
				// Large face headers (Actors' models)
				for(int face_id = 0; face_id < header.n_faces; face_id++)
				{
					var face = (FacePSX)(object)faces[face_id];
					var normal = face.normal;

					var unknownIndex = normal.PAD;

					if((face.texture.flags & 0x0800) != 0)
					{
						//TODO: Validate this
						// 1st vertex, then 2nd, 4th and 3rd, except in Croc 2 Demo Dummy WADs
						if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
						{
							// FIXME
							//quads.Add(new int[]{face.vertex[0], face.vertex[1], face.vertex[3], face.vertex[2]});
							quads.Add(new int[]
							{
								face.vertex[0],
								face.vertex[1],
								face.vertex[2],
								face.vertex[3]
							});
						}
						else
						{
							quads.Add(new int[]
							{
								face.vertex[0],
								face.vertex[1],
								face.vertex[2],
								face.vertex[3]
							});
						}
					}
					else
					{
						Utils.Assert(face.vertex[3] == 0);
						// 1st vertex, then 2nd and 3rd
						tris.Add(new Vector3(face.vertex[0], face.vertex[1], face.vertex[2]));
					}
					faces_normals.Add(new Vector3(normal.X, normal.Y, normal.Z));
					faces_texture_ids.Add(face.texture.tex_no);
					if(unknownIndex < 1)
					{
						throw new NegativeIndexError(-1, NegativeIndexError.CAUSE_FACE, unknownIndex, null);
					}
				}
			}
			else
			{
				// Small face headers (Subchunks' models)
				for(int face_id = 0; face_id < header.n_faces; face_id++)
				{
					var face = (TFacePSX)(object)faces[face_id];

					if((face.texture.flags & 0x0800) != 0)
					{
						quads.Add(new int[]
						{
							face.vertex[0],
							face.vertex[1],
							face.vertex[2],
							face.vertex[3]
						});
					}
					else
					{
						Utils.Assert(face.vertex[3] == 0);
						tris.Add(new Vector3(face.vertex[0], face.vertex[1], face.vertex[2]));
					}
					faces_texture_ids.Add(face.texture.tex_no);
				}
			}

			return new Model3DDataPSX
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

		private static void WriteVertices(TextWriter obj, IReadOnlyList<IReadOnlyList<Vector3>> vertices, ChunkRotationPSX rotation, int x, int y, int z)
		{
			// / 1024: Best value I found to correctly rescale the mesh

			switch(rotation)
			{
				case ChunkRotationPSX.TOP:
				{
					foreach(var vg in vertices)
					{
						foreach(var v in vg)
						{
							obj.WriteLine($"v {(v.X + x) / 1024.0} {(v.Y + y) / 1024.0} {(v.Z + z) / 1024.0}");
						}
					}
					break;
				}
				case ChunkRotationPSX.RIGHT:
				{
					foreach(var vg in vertices)
					{
						foreach(var v in vg)
						{
							obj.WriteLine($"v {(v.Z + x) / 1024.0} {(v.Y + y) / 1024.0} {(-v.X + z) / 1024.0}");
						}
					}
					break;
				}
				case ChunkRotationPSX.BOTTOM:
				{
					foreach(var vg in vertices)
					{
						foreach(var v in vg)
						{
							obj.WriteLine($"v {(-v.X + x) / 1024.0} {(v.Y + y) / 1024.0} {(-v.Z + z) / 1024.0}");
						}
					}
					break;
				}
				case ChunkRotationPSX.LEFT:
				{
					foreach(var vg in vertices)
					{
						foreach(var v in vg)
						{
							obj.WriteLine($"v {(-v.Z + x) / 1024.0} {(v.Y + y) / 1024.0} {(v.X + z) / 1024.0}");
						}
					}
					break;
				}
				default: throw new Exception("Unknown rotation");
			}
		}

		private static void WriteNormals(TextWriter obj, IReadOnlyList<IReadOnlyList<Vector3>> normals)
		{
			foreach(var ng in normals)
			{
				foreach(var n in ng)
				{
					obj.WriteLine($"vn {n.X} {n.Y} {n.Z}");
				}
			}
		}

		private static void WriteTextures(TextWriter obj, IEnumerable<TextureDataPSX> textures)
		{
			foreach(var texture in textures)
			{
				foreach(var coord in texture.output_coords)
				{
					obj.WriteLine($"vt {coord.X / 1024.0} {(1024 - coord.Y) / 1024.0}");
				}
			}
		}

		private void WriteQuadFaces(TextWriter obj, int vertexIndexOffset)
		{
			for(int i = 0; i < quads.Count; i++)
			{
				var v1 = vertexIndexOffset + quads[i][1] + 1;
				var v2 = vertexIndexOffset + quads[i][0] + 1;
				var v3 = vertexIndexOffset + quads[i][2] + 1;
				var v4 = vertexIndexOffset + quads[i][3] + 1;
				var t1 = 4 * faces_texture_ids[i] + 2;
				var t2 = 4 * faces_texture_ids[i] + 1;
				var t3 = 4 * faces_texture_ids[i] + 3;
				var t4 = 4 * faces_texture_ids[i] + 4;
				obj.WriteLine($"f {v1}/{t1}/{v1} {v2}/{t2}/{v2} {v3}/{t3}/{v3} {v4}/{t4}/{v4}");
			}
		}

		private void WriteTriFaces(TextWriter obj, int vertexIndexOffset)
		{
			var n_q = quads.Count;
			for(int i = 0; i < tris.Count; i++)
			{
				var v1 = vertexIndexOffset + tris[i].Y + 1;
				var v2 = vertexIndexOffset + tris[i].X + 1;
				var v3 = vertexIndexOffset + tris[i].Z + 1;
				var t1 = 4 * faces_texture_ids[n_q + i] + 2;
				var t2 = 4 * faces_texture_ids[n_q + i] + 1;
				var t3 = 4 * faces_texture_ids[n_q + i] + 3;

				obj.WriteLine($"f {v1}/{t1}/{v1} {v2}/{t2}/{v2} {v3}/{t3}/{v3}");
			}
		}

		/// <summary>Creates a standalone Wavefront OBJ 3D model.</summary>
		public void ToSingleObj(TextWriter obj, string obj_filename, IEnumerable<TextureDataPSX> textures, string mtl_filename = null)
		{
			obj.WriteLine(string.Format(mtl_header, mtl_filename ?? obj_filename));//The format coming in looks for the variable mtl_filename

			WriteVertices(obj, vertices, ChunkRotationPSX.TOP, 0, 0, 0);
			WriteNormals(obj, normals);

			WriteTextures(obj, textures);

			WriteQuadFaces(obj, 0);
			WriteTriFaces(obj, 0);
		}
		/// <summary>Creates a Wavefront OBJ 3D model and appends it to an existing TextWriter (used to export entire levels).</summary>
		public void ToBatchObj(TextWriter obj, string filename, int x, int y, int z, ChunkRotationPSX rotation, int vertex_index_offset)
		{
			obj.WriteLine($"o {filename}");

			WriteVertices(obj, vertices, rotation, x, y, z);
			WriteNormals(obj, normals);

			WriteQuadFaces(obj, vertex_index_offset);
			WriteTriFaces(obj, vertex_index_offset);
		}
	}

	public sealed class Object3DDataPSX
	{
		public Model3DHeaderPSX_Object Header { get; }
		public Model3DDataPSX Data { get; }

		private Object3DDataPSX(Model3DHeaderPSX_Object header, Model3DDataPSX data)
		{
			Header = header;
			Data = data;
		}

		public static Object3DDataPSX Parse(WadReader data_in)
		{
			var header = Model3DHeaderPSX_Object.Parse(data_in);
			var data = Model3DDataPSX.Parse(data_in, header, header.Object, is_world_model_3d: false);
			return new Object3DDataPSX(header, data);
		}

		/// <summary>
		/// Returns vertices and vertices normals of this model after application of an animation frame's
		/// rotation & translation information. The model is **not** modified.
		/// </summary>
		public Object3DDataPSX Animate(AnimationDataPSX animation, int frame_id = 0)
		{
			if(Data.n_vertices_groups != animation.n_vertices_groups)
			{
				throw new IncompatibleAnimationError(Data.n_vertices_groups, animation.n_vertices_groups);
			}
			var vertices = new Vector3[Data.n_vertices_groups][];
			var normals = new Vector3[Data.n_vertices_groups][];
			for(int i = 0; i < Data.n_vertices_groups; i++)
			{
				//var rotation = animation[frame_id][i][.., 0..3];
				//var translation = animation[frame_id][i].Translation;
				var transform = animation[frame_id][i];
				vertices[i] = Data.vertices[i].Select(v => Vector3.Transform(v, transform)).ToArray();//np.add(this.vertices[i].dot(rotation), translation)
				normals[i] = Data.normals[i].Select(v => Vector3.Transform(v, transform)).ToArray();//np.add(this.normals[i].dot(rotation), translation)
			}
			var newData = new Model3DDataPSX
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
			return new Object3DDataPSX(Header, newData);
		}
	}

	public sealed class LevelGeom3DDataPSX
	{
		public Model3DHeaderPSX_Track Header { get; }
		public Model3DDataPSX Data { get; }

		private LevelGeom3DDataPSX(Model3DHeaderPSX_Track header, Model3DDataPSX data)
		{
			Header = header;
			Data = data;
		}

		public static LevelGeom3DDataPSX Parse(WadReader data_in, Model3DHeaderPSX_Track header)
		{
			var data = Model3DDataPSX.Parse(data_in, header, header.TrackObject, is_world_model_3d: true);
			return new LevelGeom3DDataPSX(header, data);
		}
	}
}