using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.Universal;
using ArgonautReverse.WadChunks.PC;

namespace ArgonautReverse.PC
{
	public record struct ModelVertexPC(Vector3F Position, Vector3F Direction):IReadable<ModelVertexPC>,IWritable
	{
		public static ModelVertexPC Parse(WadReader reader)
		{
			var position = reader.Read<Vector3F>();
			var direction = reader.Read<Vector3F>();
			return new ModelVertexPC(position, direction);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write<Vector3F>(Position);
			writer.Write<Vector3F>(Direction);
		}
	}

	public sealed class ModelTrianglePC:IReadable<ModelTrianglePC>,IWritable
	{
		//These flags likely have changed in Aladdin.
		public ushort flags;
		public ushort[] vertexIndices;//[3];

		public int SpriteIndex;
		public SpriteStructPC sprite;

		public Vector4F pos;

		public static ModelTrianglePC Parse(WadReader reader)
		{
			var triangle = new ModelTrianglePC();
			triangle.flags = reader.Read<ushort>();
			triangle.vertexIndices = reader.ReadArray<ushort>(3);

			triangle.SpriteIndex = reader.Read<ushort>();
			reader.AssertRead<ushort>(0, warn:reader.ReadVersion == Aladdin_PC.WadVersion);//Padding
			var textChunk = reader.WadFile.GetChunk(TEXTChunkInfo.Instance);
			triangle.sprite = textChunk.Sprites[triangle.SpriteIndex];

			triangle.pos = reader.Read<Vector4F>();
			return triangle;
		}

		public void Write(WadWriter writer)
		{
			writer.Write<ushort>(flags);
			writer.WriteSizedArray<ushort>(3, vertexIndices);

			writer.Write<ushort>((ushort)SpriteIndex);
			writer.Write<ushort>(0);//Padding
			writer.Write<Vector4F>(pos);
		}

		public void GetRenderInfo(/*WadFilePC wad, out TextureStructPC? texture, out BrTexturePalettePC? palette, out bool alphaEnable,*/ out bool spriteFlag20, out ColorBGRA32 color/*, out UnknownRenderStruct4* format0, out UnknownRenderStruct4* format1*/)
		{
			//alphaEnable = (this.sprite.flags & SpriteFlagsPC.HasAlpha) != 0;
			spriteFlag20 = (this.sprite.flags & SpriteFlagsPC._20) != 0;
			//int v83 = ((int)this.sprite.flags >> 8) & 7;
			int v84 = ((int)this.sprite.flags >> 12) & 7;
			if((this.sprite.flags & SpriteFlagsPC.HasColor) != 0)
			{
				color = new ColorBGRA32
				(
					blue: this.sprite.ColorB,
					green: this.sprite.ColorG,
					red: this.sprite.ColorR,
					alpha: this.sprite.ColorAlpha
				);
				//texture = null;
				//palette = null;
				//format0 = &Graphics.textureFormats0[v84];
				//format1 = &Graphics.textureFormats2[v84];
			}
			else
			{
				color = new ColorBGRA32
				(
					//Added other color channels. Originally not set.
					blue: 0,
					green: 0,
					red: 0,
					alpha: v84 switch
					{
						0 => 0xFF,
						1 => 0x80,
						2 => 0xC0,
						3 => 0x80,
						4 => 0x40,
						_ => throw new Exception()
					}
				);
				//if(this.sprite.sourceTexture == -1)
				//{
				//	texture = null;
				//}
				//else
				//{
				//	texture = wad.TextChunk.Textures[this.sprite.sourceTexture];
				//}
				//if(this.sprite.paletteIndex == -1)
				//{
				//	palette = null;
				//}
				//else
				//{
				//	palette = wad.TextChunk.Palettes[this.sprite.paletteIndex];
				//}
				//format0 = &Graphics.textureFormats1[v83][v84];
				//format1 = &Graphics.textureFormats3[v83][v84];
			}
		}
	}

	public interface IStratObjectPC
	{
		public StratObjectPC Model{get;}
		//public abstract void GetVertexLookup(Matrix4x4F[]? animationBoneTransforms, List<ModelVertexPC> vertices);
	}

	public sealed class StratObjectPC:IReadableArrayMultipass<StratObjectPC>,IWritableArrayMultipass,IStratObjectPC//ModelStruct
	{
		//The value of the position in the wad
		public int WadOffset;

		public Vector3F[] vec;//[9]
		public ModelVertexPC[] vertices;
		public ModelTrianglePC[] triangles;
		public ushort FloorCount;
		public ushort CeilingCount;
		public ushort? WallCount;
		public FaceCollision[] collisionFaces;

		StratObjectPC IStratObjectPC.Model => this;

		public static StratObjectPC ParseStruct(WadReader reader)
		{
			var model = new StratObjectPC();
			model.WadOffset = reader.Position;

			model.vec = reader.ReadArray<Vector3F>(9);
			model.vertices = new ModelVertexPC[reader.Read<ushort>()];
			model.triangles = new ModelTrianglePC[reader.Read<ushort>()];
			reader.AssertRead<uint>(0, warn:reader.ReadVersion == Aladdin_PC.WadVersion);//vertices placeholder
			reader.AssertRead<uint>(0, warn:reader.ReadVersion == Aladdin_PC.WadVersion);//triangles placeholder
			model.FloorCount = reader.Read<ushort>();
			model.CeilingCount = reader.Read<ushort>();
			if(reader.ReadVersion.NEW_COLLISION)
			{
				model.WallCount = reader.Read<ushort>();
				reader.AssertRead<ushort>(0, warn:reader.ReadVersion == Aladdin_PC.WadVersion);//Padding
			}
			reader.AssertRead<uint>(0, warn:reader.ReadVersion == Aladdin_PC.WadVersion);//collisionFaces placeholder
			return model;
		}

		public static void ParseData(WadReader reader, StratObjectPC stratObject)
		{
			reader.ReadArray<ModelVertexPC>(stratObject.vertices);
			reader.ReadArray<ModelTrianglePC>(stratObject.triangles);

			int collisionFaceCount = stratObject.FloorCount + stratObject.CeilingCount;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				collisionFaceCount += stratObject.WallCount!.Value;
			}
			stratObject.collisionFaces = reader.ReadArray<FaceCollision>(collisionFaceCount);
		}

		public void WriteStruct(WadWriter writer)
		{
			writer.WriteSizedArray<Vector3F>(9, vec);
			writer.Write((ushort)vertices.Length);
			writer.Write((ushort)triangles.Length);
			writer.Write<uint>(0);//vertices placeholder
			writer.Write<uint>(0);//triangles placeholder
			writer.Write<ushort>(FloorCount);
			writer.Write<ushort>(CeilingCount);
			if(writer.WriteVersion.NEW_COLLISION)
			{
				writer.Write<ushort>(WallCount!.Value);
				writer.Write<ushort>(0);//Padding
			}
			writer.Write<uint>(0);//collisionFaces placeholder
		}

		public void WriteData(WadWriter writer)
		{
			writer.WriteArray<ModelVertexPC>(vertices);
			writer.WriteArray<ModelTrianglePC>(triangles);

			int collisionFaceCount = FloorCount + CeilingCount;
			if(writer.WriteVersion.NEW_COLLISION)
			{
				collisionFaceCount += WallCount!.Value;
			}
			writer.WriteSizedArray<FaceCollision>(collisionFaceCount, collisionFaces);
		}

		public ModelVertexPC[] GetVertexLookup(RotPos3F rotPos)
		{
			Matrix4x4F.CreatePositionMatrix(rotPos, out var rotPosTransform);
			var vertexLookup = new ModelVertexPC[vertices.Length];
			for(int i=0; i<vertices.Length; i++)
			{
				var vert = vertices[i];
				vertexLookup[i] = new(rotPosTransform.TransformPoint(vert.Position), vert.Direction);
			}
			return vertexLookup;
		}
	}

	public sealed class StratObject2PC:IReadableArrayMultipass<StratObject2PC>,IWritableArrayMultipass,IStratObjectPC//ModelStruct2
	{
		public StratObjectPC model;
		public ushort wField0;//Static bones
		//Number of verts in a bone
		public ushort[] boneVertCounts;

		StratObjectPC IStratObjectPC.Model => model;

		public static StratObject2PC ParseStruct(WadReader reader)
		{
			var model = new StratObject2PC();
			model.model = StratObjectPC.ParseStruct(reader);
			model.wField0 = reader.Read<ushort>();
			var boneCount = reader.Read<ushort>();
			model.boneVertCounts = new ushort[boneCount];
			reader.AssertRead<uint>(0);//boneVertCounts placeholder
			return model;
		}

		public static void ParseData(WadReader reader, StratObject2PC stratObject)
		{
			reader.ReadArray<ushort>(stratObject.boneVertCounts);
			StratObjectPC.ParseData(reader, stratObject.model);
		}

		public void WriteStruct(WadWriter writer)
		{
			model.WriteStruct(writer);
			writer.Write<ushort>(wField0);
			writer.Write((ushort)boneVertCounts.Length);
			writer.Write<uint>(0);//boneVertCounts placeholder
		}

		public void WriteData(WadWriter writer)
		{
			writer.WriteArray<ushort>(boneVertCounts);
			model.WriteData(writer);
		}

		public unsafe ModelVertexPC[] GetVertexLookup(Matrix4x4F[]? animationBoneTransforms)
		{
			if(animationBoneTransforms == null)
			{
				return model.vertices;
			}

			var vertexLookup = new ModelVertexPC[model.vertices.Length];
			int v=0;
			for(int i=0; i<wField0; i++)
			{
				vertexLookup[v] = model.vertices[v];
				v++;
			}
			for(int boneIndex=0; boneIndex<boneVertCounts.Length; boneIndex++)
			{
				Matrix4x4F currTransform = animationBoneTransforms[boneIndex];
				for(int boneVertIndex=0; boneVertIndex<boneVertCounts[boneIndex]; boneVertIndex++)
				{
					vertexLookup[v] = new
					(
						Position: currTransform.TransformPoint(model.vertices[v].Position),
						Direction: currTransform.TransformPoint(model.vertices[v].Direction)
					);
					v++;
				}
			}
			if(v != model.vertices.Length)
			{
				throw new Exception("Incorrect number of vertices.");
			}
			return vertexLookup;
		}
	}
}