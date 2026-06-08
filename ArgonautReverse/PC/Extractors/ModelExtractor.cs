using ArgonautReverse.Universal;

namespace ArgonautReverse.PC.Extractors
{
	public static class ModelExtractor
	{
		public static void ExtractAll(ProgramArgs args, Configuration conf, WadFilePC wad, IReadOnlyList<StratObject2PC> models)
		{
			if(!args.ExtractModels){return;}

			var modelDirectory = args.GetExtractDirectory(wad.Stem, "Models");
			for(int i=0; i<models.Count; i++)
			{
				var renderData = new RenderData(wad);
				renderData.RenderModel0(models[i], null);
				renderData.OutputRender(Path.Join(modelDirectory, $"Model_{i}"));
			}
		}

		public static void ExtractAll(ProgramArgs args, Configuration conf, WadFilePC wad, IReadOnlyList<StratObjectPC> pieces)
		{
			if(!args.ExtractLevels){return;}

			var trackDirectory = args.GetExtractDirectory(wad.Stem, "Track");
			for(int i=0; i<pieces.Count; i++)
			{
				var renderData = new RenderData(wad);
				renderData.RenderModel0(pieces[i], default);
				renderData.OutputRender(Path.Join(trackDirectory, $"Piece_{i}"));
			}
		}

		public static void DrawWorldGeometry(ProgramArgs args, Configuration conf, WadFilePC wad, MapPC map)
		{
			if(!args.ExtractLevels){return;}

			var trackDirectory = args.GetExtractDirectory(wad.Stem, "Track");

			var renderData = new RenderData(wad);

			for(int z=0; z<map.MapHeight; z++)
			{
				for(int x=0; x<map.MapWidth; x++)
				{
					for(var cell = map.MapPieceArray[z][x]; cell!=null; cell = cell.Next)
					{
						var cellInfo = cell.Piece;
						cellInfo.RotY = map.Positions[cellInfo.CellIndex].Rotation.Y.Raw * 0.001533980786132813f;//Not quite the value in TurnsFxToRadians
						var rotPos = new RotPos3F
						(
							rotation: new(0f, cellInfo.RotY, 0f),
							position: cellInfo.Pos
						);
						renderData.RenderModel0(wad.TrackChunk.Models[map.ModelIndices[cellInfo.CellIndex]], rotPos);
					}
				}
			}
			renderData.OutputRender(Path.Join(trackDirectory, "Full"));
		}
		public sealed class RenderData(WadFilePC wad)
		{
			private readonly List<ModelVertexPC> vertexLookup = new(4800);
			private readonly List<ObjFace> faces = new(4800);
			private readonly List<Vector2F> textureCoords = new(4800)
			{
				//Used for color transforms
				new(0,0),
				new(1,0),
				new(1,1),
				new(1,0),
			};

			public unsafe struct ObjFace
			{
				//Indices start at 1
				public fixed int vertIndex[3];
				public fixed int textCoordIndex[3];
				public fixed int normalIndex[3];

				public int textSource;
				public ColorBGRA32 color;
			}

			public void RenderModel0(StratObjectPC model, RotPos3F rotPos)
			{
				var vertices = model.GetVertexLookup(rotPos);
				GetFaces(model.triangles, vertexLookup.Count);
				vertexLookup.AddRange(vertices);
			}

		
			public void RenderModel0(StratObject2PC model, Matrix4x4F[]? animationTransforms)
			{
				var vertices = model.GetVertexLookup(animationTransforms);
				GetFaces(model.model.triangles, vertexLookup.Count);
				vertexLookup.AddRange(vertices);
			}

			private unsafe void GetFaces(ModelTrianglePC[] triangles, int vertStartingIndex)
			{
				//Graphics.lighting = false;

				//static UnknownRenderStruct5 positionLookup[4800];

				SpriteStructPC? pendingSprite = null;
				//UnknownRenderStruct4? format0 = null;
				//UnknownRenderStruct4? format1 = null;
				bool spriteFlag20 = false;
				ColorBGRA32 color = new();

				for(int t = 0; t<triangles.Length; t++)
				{
					ref var vert = ref triangles[t];
					if(((vert.flags & 8) != 0) || (spriteFlag20 && pendingSprite!.sourceTexture != vert.sprite.sourceTexture))
					{
						pendingSprite = vert.sprite;
						vert.GetRenderInfo(/*wad, out _, out _, out _,*/ out spriteFlag20, out color/*, out format0, out format1*/);
					}
					//if((positionLookup[vert.vertexIndices[0]].flags & positionLookup[vert.vertexIndices[1]].flags & positionLookup[vert.vertexIndices[2]].flags) != 0)
					//{
					//	continue;
					//}

					//TODO: [Extractor] What about this flag?
					//if((0 < vert.pos.W) && ((vert.flags & 0x400) == 0))
					//{
					//	continue;
					//}
					// 
					//float maxRhw = 1;
					//bool v227b = maxRhw > (double)door_scaledMinRange && byte_6039F2;
					//int vertCount0 = 3 + 3*6;
					//D3D.LPTLVERTEX vertices[2];
					//vertices[0] = &drawVerts[pendingVertCount];
					//vertices[1] = &drawVerts[vertCount0 + pendingVertCount];

					//vert.ApplyTrianglePosition(vertices[1], positionLookup, true);
					var face = new ObjFace();
					for(int i=0; i<3; i++)
					{
						face.vertIndex[i] = vertStartingIndex + vert.vertexIndices[i];
						face.textCoordIndex[i] = 0;
						face.normalIndex[i] = vertStartingIndex + vert.vertexIndices[i];
					}

					//vert.ApplyTriangleSurface(vertices[1], pendingSprite, color, null, false, false, 0);
					if((vert.sprite.flags & SpriteFlagsPC.HasColor) != 0)
					{
						face.color = color;
						face.textSource = -1;

						//These are still needed
						face.textCoordIndex[0] = 0;
						face.textCoordIndex[1] = 1;
						face.textCoordIndex[2] = 2;
					}
					else
					{
						face.color = color;//TODO: Only use alpha?
						face.textSource = vert.sprite.sourceTexture;
			
						TextureStructPC textureSource = wad.TextChunk.Textures[face.textSource];
						float width = textureSource.Width;
						float height = textureSource.Height;

						//I think texture coords start at the bottom left so we need to flip Y
						textureCoords.Add(new(vert.sprite.sourceMinX.U8/width, 1 - vert.sprite.sourceMinY.U8/height));
						textureCoords.Add(new(vert.sprite.sourceMaxX.U8/width, 1 - vert.sprite.sourceMinY.U8/height));
						textureCoords.Add(new(vert.sprite.sourceMinX.U8/width, 1 - vert.sprite.sourceMaxY.U8/height));
						textureCoords.Add(new(vert.sprite.sourceMaxX.U8/width, 1 - vert.sprite.sourceMaxY.U8/height));

						if((vert.flags & 0x800) != 0)
						{
							if((vert.flags & 0x10) != 0)
							{
								if((vert.flags & 0x20) != 0)
								{
									face.textCoordIndex[0] = textureCoords.Count - 3;
									face.textCoordIndex[1] = textureCoords.Count - 4;
									face.textCoordIndex[2] = textureCoords.Count - 2;
								}
								else
								{
									face.textCoordIndex[0] = textureCoords.Count - 4;
									face.textCoordIndex[1] = textureCoords.Count - 3;
									face.textCoordIndex[2] = textureCoords.Count - 1;
								}
							}
							else
							{
								if((vert.flags & 0x20) != 0)
								{
									face.textCoordIndex[0] = textureCoords.Count - 2;
									face.textCoordIndex[1] = textureCoords.Count - 1;
									face.textCoordIndex[2] = textureCoords.Count - 3;
								}
								else
								{
									face.textCoordIndex[0] = textureCoords.Count - 1;
									face.textCoordIndex[1] = textureCoords.Count - 2;
									face.textCoordIndex[2] = textureCoords.Count - 4;
								}
							}
						}
						else
						{
							if((vert.flags & 0x20) != 0)
							{
								face.textCoordIndex[0] = textureCoords.Count - 2;
								face.textCoordIndex[1] = textureCoords.Count - 1;
								face.textCoordIndex[2] = textureCoords.Count - 4;
							}
							else
							{
								face.textCoordIndex[0] = textureCoords.Count - 1;
								face.textCoordIndex[1] = textureCoords.Count - 2;
								face.textCoordIndex[2] = textureCoords.Count - 3;
							}
						}
						
					}

					//TODO: [Extractor] Alpha support
					//vert.ApplyTriangleAlpha(vertices[1], v227b, zValue, format0.HasAlpha, color.alpha);
		
					//if(v227b)
					//{
					//	vertCount0 = Graphics.sub_456930(vertices[0], vertices[1], 3, 0x3F);
					//}
					//else
					//{
					//	vertCount0 = Graphics.sub_456300(vertices[0], vertices[1], 3, 0x3F);
					//}
					//if(vertCount0 == 0)
					//{
					//	continue;
					//}
					//pendingVertCount += vertCount0;

					//Correct indices to start at 1
					for(int i=0; i<3; i++)
					{
						face.vertIndex[i]++;
						face.textCoordIndex[i]++;
						face.normalIndex[i]++;
					}
					faces.Add(face);
				}
			}

			public unsafe void OutputRender(string fileName)
			{
				//Materials
				string mtlFilePath = $"{fileName}.mtl";
				using(var outputMtl = new StreamWriter(mtlFilePath, false))
				{
					for(int i=0; i<faces.Count; i++)
					{
						var face = faces[i];
						int faceTextSource = face.textSource;
						ColorBGRA32 faceColor = face.color;
						bool alreadyDone = false;
						for(int old=0; old<i; old++)
						{
							if(faces[old].textSource == faceTextSource && faces[old].color.Raw == faceColor.Raw)
							{
								alreadyDone = true;
								break;
							}
						}
						if(alreadyDone){continue;}
						if(faceTextSource>=0)
						{
							outputMtl.WriteLine($"newmtl Texture_{faceTextSource}");
							outputMtl.WriteLine($"map_Kd {faceTextSource}.png");
						}
						else
						{
							outputMtl.WriteLine($"newmtl Color_{faceColor.Raw}");
							outputMtl.WriteLine("Kd {0} {1} {2}", faceColor.R/255f, faceColor.G/255f, faceColor.B/255f);
						}
						outputMtl.WriteLine("Ka {0} {1} {2}", faceColor.R/255f, faceColor.G/255f, faceColor.B/255f);
						//outputMtl.WriteLine("Ks 0.0 0.0 0.0");
						//outputMtl.WriteLine("Tr 1.0");//Transparency
						//outputMtl.WriteLine("illum 1");
						//outputMtl.WriteLine("Ns 0.0");
						outputMtl.WriteLine();
					}
				}

				//Object
				string objFilePath = $"{fileName}.obj";
				using(var outputObj = new StreamWriter(objFilePath, false))
				{
					outputObj.WriteLine($"# {Path.GetFileName(objFilePath)}");
					outputObj.WriteLine();

					outputObj.WriteLine("# Material");
					outputObj.WriteLine($"mtllib {Path.GetFileName(mtlFilePath)}");
					outputObj.WriteLine();
	
					outputObj.WriteLine("# Vertices");
					for(int i=0; i<vertexLookup.Count; i++)
					{
						var pos = vertexLookup[i].Position;
						outputObj.WriteLine($"v {pos.X} {pos.Y} {pos.Z}");
					}
					outputObj.WriteLine();

					outputObj.WriteLine("# Texture Coords");
					for(int i=0; i<textureCoords.Count; i++)
					{
						var coords = textureCoords[i];
						outputObj.WriteLine($"vt {coords.X} {coords.Y}");
					}
					outputObj.WriteLine();

					outputObj.WriteLine("# Vertex Normals");
					for(int i=0; i<vertexLookup.Count; i++)
					{
						var dir = vertexLookup[i].Direction;
						outputObj.WriteLine($"vn {dir.X} {dir.Y} {dir.Z}");
					}
					outputObj.WriteLine();

					outputObj.WriteLine("# Polygon faces");
					for(int i=0; i<faces.Count; i++)
					{
						var face = faces[i];
						if(i==0 || face.textSource != faces[i-1].textSource || face.color.Raw != faces[i-1].color.Raw)
						{
							if(face.textSource >= 0)
							{
								outputObj.WriteLine();
								outputObj.WriteLine($"usemtl Texture_{face.textSource}");
							}
							else
							{
								outputObj.WriteLine();
								outputObj.WriteLine($"usemtl Color_{face.color.Raw}");
							}
						}

						outputObj.Write("f");
						for(int j=0; j<3; j++)
						{
							outputObj.Write($" {face.vertIndex[j]}/");
							if(face.textCoordIndex[j]>0)
							{
								outputObj.Write(face.textCoordIndex[j]);
							}
							outputObj.Write('/');
							if(face.normalIndex[j]>0)
							{
								outputObj.Write(face.normalIndex[j]);
							}
						}
						outputObj.WriteLine();
					}
				}
			}
		}
	}
}