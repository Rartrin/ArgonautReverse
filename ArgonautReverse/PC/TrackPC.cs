using ArgonautReverse.IO;
using ArgonautReverse.Universal;
using ArgonautReverse.WadChunks.PC;

namespace ArgonautReverse.PC
{
	public readonly struct ModelCollisionStruct0PC(short wField0, short wField1, int field2):IReadable<ModelCollisionStruct0PC>,IWritable
	{
		public readonly short wField0 = wField0;
		public readonly short wField1 = wField1;
		public readonly int field2 = field2;

		public static ModelCollisionStruct0PC Parse(WadReader reader)
		{
			var wField0 = reader.Read<short>();
			var wField1 = reader.Read<short>();
			var field2 = reader.Read<int>();
			return new ModelCollisionStruct0PC(wField0, wField1, field2);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<short>(wField0);
			writer.Write<short>(wField1);
			writer.Write<int>(field2);
		}
	}

	public sealed class Model_SubStruct1PC:IReadable<Model_SubStruct1PC>,IWritable
	{
		public byte bField0;
		public byte bField1;
		public short wGapField0;
		public ModelCollisionStruct0PC[] collisionArray;//[4]
		public short Y;
		public short X;
		public int field4;

		public static Model_SubStruct1PC Parse(WadReader reader)
		{
			var ret = new Model_SubStruct1PC();
			ret.bField0 = reader.Read<byte>();
			ret.bField1 = reader.Read<byte>();
			ret.wGapField0 = reader.Read<short>();
			ret.collisionArray = reader.ReadArray<ModelCollisionStruct0PC>(4);
			ret.Y = reader.Read<short>();
			ret.X = reader.Read<short>();
			ret.field4 = reader.Read<int>();
			return ret;
		}

		public void Write(WadWriter writer)
		{
			writer.Write<byte>(bField0);
			writer.Write<byte>(bField1);
			writer.Write<short>(wGapField0);
			writer.WriteSizedArray<ModelCollisionStruct0PC>(4, collisionArray);
			writer.Write<short>(Y);
			writer.Write<short>(X);
			writer.Write<int>(field4);
		}
	}

	public readonly record struct ModelVertexPC(Vector3F Position, Vector3F Direction):IReadable<ModelVertexPC>,IWritable
	{
		public static ModelVertexPC Parse(WadReader reader)
		{
			var position = reader.Read<Vector3F>();
			var direction = reader.Read<Vector3F>();
			return new ModelVertexPC(position, direction);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<Vector3F>(Position);
			writer.Write<Vector3F>(Direction);
		}
	}

	public sealed class ModelTrianglePC:IReadable<ModelTrianglePC>,IWritable
	{
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
			reader.AssertRead<ushort>(0);//Padding
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

		public void GetRenderInfo(WadFilePC wad, out TextureStructPC? texture, out BrTexturePalettePC? palette, out bool alphaEnable, out bool spriteFlag20, out ColorBGRA32 color/*, out UnknownRenderStruct4* format0, out UnknownRenderStruct4* format1*/)
		{
			alphaEnable = (this.sprite.flags & SpriteFlagsPC.HasAlpha) != 0;
			spriteFlag20 = (this.sprite.flags & SpriteFlagsPC._20) != 0;
			int v83 = ((int)this.sprite.flags >> 8) & 7;
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
				texture = null;
				palette = null;
				//format0 = &Graphics.textureFormats0[v84];
				//format1 = &Graphics.textureFormats2[v84];
			}
			else
			{
				color = new ColorBGRA32
				(
					//Added other color channels
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
				if(this.sprite.sourceTexture == -1)
				{
					texture = null;
				}
				else
				{
					texture = wad.TextChunk.Textures[this.sprite.sourceTexture];
				}
				if(this.sprite.paletteIndex == -1)
				{
					palette = null;
				}
				else
				{
					palette = wad.TextChunk.Palettes[this.sprite.paletteIndex];
				}
				//format0 = &Graphics.textureFormats1[v83][v84];
				//format1 = &Graphics.textureFormats3[v83][v84];
			}
		}
	}

	public sealed class StratObjectPC:IReadableArrayMultipass<StratObjectPC>,IWritableArrayMultipass//ModelStruct
	{
		//The value of the position in the wad
		public int WadOffset;

		public Vector3F[] vec;//[9]
		public ModelVertexPC[] vertices;
		public ModelTrianglePC[] triangles;
		public ushort wField1;
		public ushort wField2;
		public Model_SubStruct1PC[] array2;

		public static StratObjectPC ParseStruct(WadReader reader)
		{
			var model = new StratObjectPC();
			model.WadOffset = reader.Position;

			model.vec = reader.ReadArray<Vector3F>(9);
			model.vertices = new ModelVertexPC[reader.Read<ushort>()];
			model.triangles = new ModelTrianglePC[reader.Read<ushort>()];
			reader.AssertRead<uint>(0);//vertices placeholder
			reader.AssertRead<uint>(0);//triangles placeholder
			model.wField1 = reader.Read<ushort>();
			model.wField2 = reader.Read<ushort>();
			reader.AssertRead<uint>(0);//array2 placeholder
			return model;
		}

		public static void ParseData(WadReader reader, StratObjectPC stratObject)
		{
			reader.ReadArray<ModelVertexPC>(stratObject.vertices);
			reader.ReadArray<ModelTrianglePC>(stratObject.triangles);

			int array2Length = stratObject.wField1 + stratObject.wField2;
			stratObject.array2 = reader.ReadArray<Model_SubStruct1PC>(array2Length);
		}

		public void WriteStruct(WadWriter writer)
		{
			writer.WriteSizedArray<Vector3F>(9, vec);
			writer.Write((ushort)vertices.Length);
			writer.Write((ushort)triangles.Length);
			writer.Write<uint>(0);//vertices placeholder
			writer.Write<uint>(0);//triangles placeholder
			writer.Write<ushort>(wField1);
			writer.Write<ushort>(wField2);
			writer.Write<uint>(0);//array2 placeholder
		}

		public void WriteData(WadWriter writer)
		{
			writer.WriteArray<ModelVertexPC>(vertices);
			writer.WriteArray<ModelTrianglePC>(triangles);

			int array2Length = wField1 + wField2;
			writer.WriteSizedArray<Model_SubStruct1PC>(array2Length, array2);
		}
	}

	public sealed class StratObject2PC:IReadableArrayMultipass<StratObject2PC>,IWritableArrayMultipass//ModelStruct2
	{
		public StratObjectPC model;
		public ushort wField0;
		//Number of verts in a bone
		public ushort[] boneVertCounts;

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
	}
}