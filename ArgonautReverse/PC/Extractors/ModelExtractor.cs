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
				RenderModel0(wad, Path.Join(modelDirectory, $"Model_{i}"), models[i], null);
			}
		}

		public unsafe struct ObjFace
		{
			//Indices start at 1
			public fixed int vertIndex[3];
			public fixed int textCoordIndex[3];
			public fixed int normalIndex[3];

			public int textSource;
			public ColorBGRA32 color;
		}

		//Based on ModelStruct.DrawModel0
		public static unsafe bool RenderModel0(WadFilePC wad, string fileName, StratObject2PC model, Matrix4x4F[]? animationTransforms)
		{
			//Graphics.lighting = false;

			//static UnknownRenderStruct5 positionLookup[4800];
			var faces = new ObjFace[4800];
			int faceCount = 0;

			var textCoords = new Vector2F[4800];
			int textCoordCount = 0;

			//Used for color transforms
			textCoords[textCoordCount+0] = new(0,0);
			textCoords[textCoordCount+1] = new(1,0);
			textCoords[textCoordCount+2] = new(1,1);
			textCoords[textCoordCount+3] = new(1,0);
			textCoordCount+=4;

			//UInt32 modelFlags;
			//if(!Model.RenderModelToDrawBuffer(model.model, Matrix4x4F.Identity, &modelFlags))
			//{
			//	return false;
			//}

			//const ModelVertexPC* vertexLookup = model.GetVertexLookup(animationTransforms != null, animationTransforms, -1, -1, -1);
			var vertexLookup = new ModelVertexPC[4800];

			//if(animationTransforms == null)
			//{
			//	return model.model.vertices;
			//}
			int v=0;
			for(int i=0; i<model.wField0; i++)
			{
				vertexLookup[v] = model.model.vertices[v];
				v++;
			}
			if(animationTransforms != null)
			{
				for(int boneIndex=0; boneIndex<model.boneVertCounts.Length; boneIndex++)
				{
					ref readonly Matrix4x4F currTransform = ref animationTransforms[boneIndex];
					for(int boneVertIndex=0; boneVertIndex<model.boneVertCounts[boneIndex]; boneVertIndex++)
					{
						vertexLookup[v] = new
						(
							Position: currTransform.TransformPoint(model.model.vertices[v].Position),
							Direction: currTransform.TransformPoint(model.model.vertices[v].Direction)
						);
						v++;
					}
				}
			}

			TextureStructPC? texture = null;
			BrTexturePalettePC? palette = null;
			int pendingVertCount = 0;
			SpriteStructPC? pendingSprite = null;
			bool useTriangleFan = false;
			bool flatShade = false;
			bool alphaEnable = false;
			bool zFuncLessOrEqual = false;
			bool zEnable = false;
			//UnknownRenderStruct4? format0 = null;
			//UnknownRenderStruct4? format1 = null;
			bool triZBias = false;
			bool spriteFlag20 = false;
			ColorBGRA32 color = new();

			for(int t = 0; t<model.model.triangles.Length; t++)
			{
				ref var vert = ref model.model.triangles[t];
				if(((vert.flags & 8) != 0) || (spriteFlag20 && pendingSprite.sourceTexture != vert.sprite.sourceTexture))
				{
					//if(pendingVertCount>0)
					//{
					//	Graphics.DrawPrimitives(texture, palette, drawVerts, pendingVertCount, null, 0, useTriangleFan, false, flatShade, alphaEnable, zFuncLessOrEqual, zEnable, format0.AlphaOp, format0.BlendMode, monoEnable, triZBias);
					//	pendingVertCount = 0;
					//}
					pendingSprite = vert.sprite;
					vert.GetRenderInfo(wad, out texture, out palette, out alphaEnable, out spriteFlag20, out color/*, out format0, out format1*/);
					//flatShade = true;
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
				ref var face = ref faces[faceCount];
				for(int i=0; i<3; i++)
				{
					face.vertIndex[i] = vert.vertexIndices[i];
					face.textCoordIndex[i] = 0;
					face.normalIndex[i] = vert.vertexIndices[i];
				}
				faceCount++;

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
					face.color = color;
					face.textSource = vert.sprite.sourceTexture;
			
					TextureStructPC textureSource = wad.TextChunk.Textures[face.textSource];
					float width = textureSource.Width;
					float height = textureSource.Height;

					//I think texture coords start at the bottom left so we need to flip Y
					textCoords[textCoordCount+0].X = vert.sprite.sourceMinX.U8/width;
					textCoords[textCoordCount+0].Y = 1 - vert.sprite.sourceMinY.U8/height;

					textCoords[textCoordCount+1].X = vert.sprite.sourceMaxX.U8/width;
					textCoords[textCoordCount+1].Y = 1 - vert.sprite.sourceMinY.U8/height;

					textCoords[textCoordCount+2].X = vert.sprite.sourceMinX.U8/width;
					textCoords[textCoordCount+2].Y = 1 - vert.sprite.sourceMaxY.U8/height;

					textCoords[textCoordCount+3].X = vert.sprite.sourceMaxX.U8/width;
					textCoords[textCoordCount+3].Y = 1 - vert.sprite.sourceMaxY.U8/height;

					if((vert.flags & 0x800) != 0)
					{
						if((vert.flags & 0x10) != 0)
						{
							if((vert.flags & 0x20) != 0)
							{
								face.textCoordIndex[0] = textCoordCount + 1;
								face.textCoordIndex[1] = textCoordCount + 0;
								face.textCoordIndex[2] = textCoordCount + 2;
							}
							else
							{
								face.textCoordIndex[0] = textCoordCount + 0;
								face.textCoordIndex[1] = textCoordCount + 1;
								face.textCoordIndex[2] = textCoordCount + 3;
							}
						}
						else
						{
							if((vert.flags & 0x20) != 0)
							{
								face.textCoordIndex[0] = textCoordCount + 2;
								face.textCoordIndex[1] = textCoordCount + 3;
								face.textCoordIndex[2] = textCoordCount + 1;
							}
							else
							{
								face.textCoordIndex[0] = textCoordCount + 3;
								face.textCoordIndex[1] = textCoordCount + 2;
								face.textCoordIndex[2] = textCoordCount + 0;
							}
						}
					}
					else
					{
						if((vert.flags & 0x20) != 0)
						{
							face.textCoordIndex[0] = textCoordCount + 2;
							face.textCoordIndex[1] = textCoordCount + 3;
							face.textCoordIndex[2] = textCoordCount + 0;
						}
						else
						{
							face.textCoordIndex[0] = textCoordCount + 3;
							face.textCoordIndex[1] = textCoordCount + 2;
							face.textCoordIndex[2] = textCoordCount + 1;
						}
					}
					textCoordCount+=4;
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

				//flatShade &= !v227b;
				//pendingVertCount += vertCount0;

				//Correct indices to start at 1
				for(int i=0; i<3; i++)
				{
					face.vertIndex[i]++;
					face.textCoordIndex[i]++;
					face.normalIndex[i]++;
				}
			}

			//Materials
			string mtlFilePath = $"{fileName}.mtl";
			using(var outputMtl = new StreamWriter(mtlFilePath, false))
			{
				for(int i=0; i<faceCount; i++)
				{
					int faceTextSource = faces[i].textSource;
					ColorBGRA32 faceColor = faces[i].color;
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
						outputMtl.WriteLine($"map_Kd Texture_{faceTextSource}.bmp");
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
				for(int i=0; i<model.model.vertices.Length; i++)
				{
					var pos = vertexLookup[i].Position;
					outputObj.WriteLine($"v {pos.X} {pos.Y} {pos.Z}");
				}
				outputObj.WriteLine();

				outputObj.WriteLine("# Texture Coords");
				for(int i=0; i<textCoordCount; i++)
				{
					ref var coords = ref textCoords[i];
					outputObj.WriteLine($"vt {coords.X} {coords.Y}");
				}
				outputObj.WriteLine();

				outputObj.WriteLine("# Vertex Normals");
				for(int i=0; i<model.model.vertices.Length; i++)
				{
					var dir = vertexLookup[i].Direction;
					outputObj.WriteLine($"vn {dir.X} {dir.Y} {dir.Z}");
				}
				outputObj.WriteLine();

				outputObj.WriteLine("# Polygon faces");
				for(int i=0; i<faceCount; i++)
				{
					ref var face = ref faces[i];
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

			return true;
		}
    }
}
