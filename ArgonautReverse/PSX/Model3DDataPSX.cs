using System.Numerics;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX.LibGTE;

namespace ArgonautReverse.PSX
{
	public abstract class Model3DDataPSX<Face>(bool isTrack, IReadOnlyList<IReadOnlyList<Vector3>> vertices, IReadOnlyList<IReadOnlyList<Vector3>> normals, IReadOnlyList<int[]> quads, IReadOnlyList<Vector3> tris, IReadOnlyList<Vector3> facesNormals, IReadOnlyList<int> facesTextureIds, int verticesGroups) where Face:IReadable<Face>
	{
		public const string mtl_header = "mtllib {0}.MTL\nusemtl mtl1\ns off";

		public readonly bool IsTrack = isTrack;
		public readonly IReadOnlyList<IReadOnlyList<Vector3>> Vertices = vertices;
		public readonly IReadOnlyList<IReadOnlyList<Vector3>> Normals = normals;
		public readonly IReadOnlyList<int[]> Quads = quads;
		public readonly IReadOnlyList<Vector3> Tris = tris;
		public readonly IReadOnlyList<Vector3> FacesNormals = facesNormals;
		public readonly IReadOnlyList<int> FacesTextureIds = facesTextureIds;
		public readonly int VerticesGroups = verticesGroups;

		protected static IReadOnlyList<IReadOnlyList<Vector3>> ParseVerticesNormals(IReadOnlyList<SVECTOR> verts_norms)
		{
			var res = new List<Vector3[]>();
			var group = new List<Vector3>();
			for(int v = 0; v < verts_norms.Count; v++)
			{
				// Vertices
				var xyz_pad = verts_norms[v];
				var xyz = new Vector3(xyz_pad.vx, xyz_pad.vy, xyz_pad.vz);
				group.Add(xyz);
				//TODO: Use index for more?
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
			for(int i = 0; i < Quads.Count; i++)
			{
				var v1 = vertexIndexOffset + Quads[i][1] + 1;
				var v2 = vertexIndexOffset + Quads[i][0] + 1;
				var v3 = vertexIndexOffset + Quads[i][2] + 1;
				var v4 = vertexIndexOffset + Quads[i][3] + 1;
				var t1 = 4 * FacesTextureIds[i] + 2;
				var t2 = 4 * FacesTextureIds[i] + 1;
				var t3 = 4 * FacesTextureIds[i] + 3;
				var t4 = 4 * FacesTextureIds[i] + 4;
				obj.WriteLine($"f {v1}/{t1}/{v1} {v2}/{t2}/{v2} {v3}/{t3}/{v3} {v4}/{t4}/{v4}");
			}
		}

		private void WriteTriFaces(TextWriter obj, int vertexIndexOffset)
		{
			var n_q = Quads.Count;
			for(int i = 0; i < Tris.Count; i++)
			{
				var v1 = vertexIndexOffset + Tris[i].Y + 1;
				var v2 = vertexIndexOffset + Tris[i].X + 1;
				var v3 = vertexIndexOffset + Tris[i].Z + 1;
				var t1 = 4 * FacesTextureIds[n_q + i] + 2;
				var t2 = 4 * FacesTextureIds[n_q + i] + 1;
				var t3 = 4 * FacesTextureIds[n_q + i] + 3;

				obj.WriteLine($"f {v1}/{t1}/{v1} {v2}/{t2}/{v2} {v3}/{t3}/{v3}");
			}
		}

		/// <summary>Creates a standalone Wavefront OBJ 3D model.</summary>
		public void ToSingleObj(TextWriter obj, string obj_filename, IEnumerable<TextureDataPSX> textures, string? mtl_filename = null)
		{
			obj.WriteLine(string.Format(mtl_header, mtl_filename ?? obj_filename));//The format coming in looks for the variable mtl_filename

			WriteVertices(obj, Vertices, ChunkRotationPSX.TOP, 0, 0, 0);
			WriteNormals(obj, Normals);

			WriteTextures(obj, textures);

			WriteQuadFaces(obj, 0);
			WriteTriFaces(obj, 0);
		}
		/// <summary>Creates a Wavefront OBJ 3D model and appends it to an existing TextWriter (used to export entire levels).</summary>
		public void ToBatchObj(TextWriter obj, string filename, int x, int y, int z, ChunkRotationPSX rotation, int vertex_index_offset)
		{
			obj.WriteLine($"o {filename}");

			WriteVertices(obj, Vertices, rotation, x, y, z);
			WriteNormals(obj, Normals);

			WriteQuadFaces(obj, vertex_index_offset);
			WriteTriFaces(obj, vertex_index_offset);
		}
	}

	public sealed class ObjectModelDataPSX(ObjectPSX header, IReadOnlyList<IReadOnlyList<Vector3>> vertices, IReadOnlyList<IReadOnlyList<Vector3>> normals, IReadOnlyList<int[]> quads, IReadOnlyList<Vector3> tris, IReadOnlyList<Vector3> facesNormals, IReadOnlyList<int> facesTextureIds, int verticesGroups):Model3DDataPSX<FacePSX>(false, vertices, normals, quads, tris, facesNormals, facesTextureIds, verticesGroups)
	{
		//header parameter is needed for chunks 3D models, where the headers are separated from the 3D model data.
		public readonly ObjectPSX obj = header;
		public int n_vertices => obj.nvert;

		public static unsafe ObjectModelDataPSX Parse(WadReader data_in, ObjectPSX obj)
		{
			obj.ParseSetupData(data_in);

			var vertices = ParseVerticesNormals(obj.lvert);
			var normals = ParseVerticesNormals(obj.lnorm!);

			if(vertices.Count != normals.Count)
			{
				throw new VerticesNormalsGroupsMismatch(vertices.Count, normals.Count, -1);
			}
			// Faces
			var quads = new List<int[]>();
			var tris = new List<Vector3>();
			var faces_normals = new List<Vector3>();
			var facesTextureIds = new List<int>();

			var faces = obj.lface;

			// Large face headers (Actors' models)
			for(int face_id = 0; face_id < obj.nface; face_id++)
			{
				var face = faces[face_id];
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
						quads.Add
						([
							face.VertexIndices[0],
							face.VertexIndices[1],
							face.VertexIndices[2],
							face.VertexIndices[3]
						]);
					}
					else
					{
						quads.Add
						([
							face.VertexIndices[0],
							face.VertexIndices[1],
							face.VertexIndices[2],
							face.VertexIndices[3]
						]);
					}
				}
				else
				{
					Utils.Assert(face.VertexIndices[3] == 0);
					// 1st vertex, then 2nd and 3rd
					tris.Add(new Vector3(face.VertexIndices[0], face.VertexIndices[1], face.VertexIndices[2]));
				}
				faces_normals.Add(new Vector3(normal.X, normal.Y, normal.Z));
				facesTextureIds.Add(face.texture.tex_no);
				if(unknownIndex < 1)
				{
					throw new NegativeIndexError(-1, NegativeIndexError.CAUSE_FACE, unknownIndex, null);
				}
			}

			return new ObjectModelDataPSX
			(
				obj,
				vertices,
				normals,
				quads,
				tris,
				faces_normals,
				facesTextureIds,
				vertices.Count
			);
		}
	}

	public sealed class TrackModelDataPSX(TObjectPSX header, IReadOnlyList<IReadOnlyList<Vector3>> vertices, IReadOnlyList<IReadOnlyList<Vector3>> normals, IReadOnlyList<int[]> quads, IReadOnlyList<Vector3> tris, IReadOnlyList<Vector3> facesNormals, IReadOnlyList<int> facesTextureIds, int verticesGroups):Model3DDataPSX<TFacePSX>(true, vertices, normals, quads, tris, facesNormals, facesTextureIds, verticesGroups)
	{
		//header parameter is needed for chunks 3D models, where the headers are separated from the 3D model data.
		public readonly TObjectPSX obj = header;
		public int n_vertices => obj.nvert;

		public static unsafe TrackModelDataPSX Parse(WadReader data_in, TObjectPSX obj)
		{
			obj.ParseSetupData(data_in);

			var vertices = ParseVerticesNormals(obj.lvert);
			IReadOnlyList<IReadOnlyList<Vector3>> normals;

			if(data_in.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion)
			{
				normals = [];
			}
			else
			{
				normals = ParseVerticesNormals(obj.lnorm!);

				if(vertices.Count != normals.Count)
				{
					throw new VerticesNormalsGroupsMismatch(vertices.Count, normals.Count, -1);
				}
			}
			// Faces
			var quads = new List<int[]>();
			var tris = new List<Vector3>();
			var faces_normals = new List<Vector3>();
			var facesTextureIds = new List<int>();

			var faces = obj.lface;


			//Dummy has normal field on both track
			if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				// Large face headers (Actors' models)
				for(int face_id = 0; face_id < obj.nface; face_id++)
				{
					var face = faces[face_id];
					var normal = face.normal!.Value;

					var unknownIndex = normal.PAD;

					if((face.texture.flags & 0x0800) != 0)
					{
						//TODO: Validate this
						// 1st vertex, then 2nd, 4th and 3rd, except in Croc 2 Demo Dummy WADs
						if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
						{
							// FIXME
							//quads.Add(new int[]{face.vertex[0], face.vertex[1], face.vertex[3], face.vertex[2]});
							quads.Add
							([
								face.VertexIndices[0],
								face.VertexIndices[1],
								face.VertexIndices[2],
								face.VertexIndices[3]
							]);
						}
						else
						{
							quads.Add
							([
								face.VertexIndices[0],
								face.VertexIndices[1],
								face.VertexIndices[2],
								face.VertexIndices[3]
							]);
						}
					}
					else
					{
						Utils.Assert(face.VertexIndices[3] == 0);
						// 1st vertex, then 2nd and 3rd
						tris.Add(new Vector3(face.VertexIndices[0], face.VertexIndices[1], face.VertexIndices[2]));
					}
					faces_normals.Add(new Vector3(normal.X, normal.Y, normal.Z));
					facesTextureIds.Add(face.texture.tex_no);
					if(unknownIndex < 1)
					{
						throw new NegativeIndexError(-1, NegativeIndexError.CAUSE_FACE, unknownIndex, null);
					}
				}
			}
			else
			{
				// Small face headers (Subchunks' models)
				for(int faceId = 0; faceId < obj.nface; faceId++)
				{
					var face = faces[faceId];

					//TODO: Handle potential normals

					if((face.texture.flags & 0x0800) != 0)
					{
						quads.Add
						([
							face.VertexIndices[0],
							face.VertexIndices[1],
							face.VertexIndices[2],
							face.VertexIndices[3]
						]);
					}
					else
					{
						Utils.Assert(face.VertexIndices[3] == 0);
						tris.Add(new Vector3(face.VertexIndices[0], face.VertexIndices[1], face.VertexIndices[2]));
					}
					facesTextureIds.Add(face.texture.tex_no);
				}
			}

			return new TrackModelDataPSX
			(
				obj,
				vertices,
				normals,
				quads,
				tris,
				faces_normals,
				facesTextureIds,
				vertices.Count
			);
		}
	}

	public sealed class ObjectDataPSX:IReadable<ObjectDataPSX>
	{
		public ObjectPSX Object{get;}
		public ObjectModelDataPSX Data{get;}

		private ObjectDataPSX(ObjectPSX obj, ObjectModelDataPSX data)
		{
			Object = obj;
			Data = data;
		}

		public static ObjectDataPSX Parse(WadReader data_in)
		{
			var obj = ObjectPSX.Parse(data_in);
			var data = ObjectModelDataPSX.Parse(data_in, obj);
			return new ObjectDataPSX(obj, data);
		}

		/// <summary>
		/// Returns vertices and vertices normals of this model after application of an animation frame's
		/// rotation & translation information. The model is **not** modified.
		/// </summary>
		public ObjectDataPSX Animate(AnimationDataPSX animation, int frame_id = 0)
		{
			if(Data.VerticesGroups != animation.n_vertices_groups)
			{
				throw new IncompatibleAnimationError(Data.VerticesGroups, animation.n_vertices_groups);
			}
			var vertices = new Vector3[Data.VerticesGroups][];
			var normals = new Vector3[Data.VerticesGroups][];
			for(int i = 0; i < Data.VerticesGroups; i++)
			{
				//var rotation = animation[frame_id][i][.., 0..3];
				//var translation = animation[frame_id][i].Translation;
				var transform = animation.Frames[frame_id][i];

				vertices[i] = new Vector3[Data.Vertices[i].Count];
				for(int j=0; j<vertices[i].Length; j++)
				{
					vertices[i][j] = Vector3.Transform(Data.Vertices[i][j], transform);//np.add(this.vertices[i].dot(rotation), translation)
				}
				normals[i] = new Vector3[Data.Normals[i].Count];
				for(int j=0; j<normals[i].Length; j++)
				{
					normals[i][j] = Vector3.Transform(Data.Normals[i][j], transform);//np.add(this.normals[i].dot(rotation), translation)
				}
			}
			var newData = new ObjectModelDataPSX
			(
				Data.obj,
				vertices,
				normals,
				Data.Quads,
				Data.Tris,
				Data.FacesNormals,
				Data.FacesTextureIds,
				Data.VerticesGroups
			);
			return new ObjectDataPSX(Object, newData);
		}
	}

	public sealed class TObjectDataPSX
	{
		public TObjectPSX Object{get;}
		public TrackModelDataPSX Data{get;}

		private TObjectDataPSX(TObjectPSX obj, TrackModelDataPSX data)
		{
			Object = obj;
			Data = data;
		}

		public static TObjectDataPSX Parse(WadReader data_in, TObjectPSX obj)
		{
			var data = TrackModelDataPSX.Parse(data_in, obj);
			return new TObjectDataPSX(obj, data);
		}
	}
}