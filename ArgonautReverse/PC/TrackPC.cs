using ArgonautReverse.IO;
using ArgonautReverse.WadChunks.PC;

namespace ArgonautReverse.PC
{
	public readonly struct ModelCollisionStruct0PC:IReadable<ModelCollisionStruct0PC>
	{
		public readonly short wField0;
		public readonly short wField1;
		public readonly int field2;

		public ModelCollisionStruct0PC(short wField0, short wField1, int field2)
		{
			this.wField0 = wField0;
			this.wField1 = wField1;
			this.field2 = field2;
		}

		public static ModelCollisionStruct0PC Parse(WadReader reader)
		{
			var wField0 = reader.Read<short>();
			var wField1 = reader.Read<short>();
			var field2 = reader.Read<int>();
			return new ModelCollisionStruct0PC(wField0, wField1, field2);
		}
	}

	public sealed class Model_SubStruct1PC:IReadable<Model_SubStruct1PC>
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
	}

	public readonly struct ModelVertexPC:IReadable<ModelVertexPC>
	{
		public readonly Vector3F Position;
		public readonly Vector3F Direction;

		public ModelVertexPC(Vector3F position, Vector3F direction)
		{
			Position = position;
			Direction = direction;
		}

		public static ModelVertexPC Parse(WadReader reader)
		{
			var position = reader.Read<Vector3F>();
			var direction = reader.Read<Vector3F>();
			return new ModelVertexPC(position, direction);
		}
	}

	public sealed class ModelTrianglePC:IReadable<ModelTrianglePC>
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
	}

	public sealed class StratObjectPC:IReadableArrayMultipass<StratObjectPC>//ModelStruct
	{
		//The value of the position in the wad
		public int WadOffset;

		public Vector3F[] vec;//[9]
		public ushort vertexCount;
		public ushort triangleCount;
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
			model.vertexCount = reader.Read<ushort>();
			model.triangleCount = reader.Read<ushort>();
			reader.AssertRead<uint>(0);//vertices placeholder
			reader.AssertRead<uint>(0);//triangles placeholder
			model.wField1 = reader.Read<ushort>();
			model.wField2 = reader.Read<ushort>();
			reader.AssertRead<uint>(0);//array2 placeholder
			return model;
		}

		public static void ParseData(WadReader reader, StratObjectPC stratObject)
		{
			stratObject.vertices = reader.ReadArray<ModelVertexPC>(stratObject.vertexCount);

			stratObject.triangles = reader.ReadArray<ModelTrianglePC>(stratObject.triangleCount);

			int array2Length = stratObject.wField1 + stratObject.wField2;
			stratObject.array2 = reader.ReadArray<Model_SubStruct1PC>(array2Length);
		}
	}

	public sealed class StratObject2PC:IReadableArrayMultipass<StratObject2PC>//ModelStruct2
	{
		public StratObjectPC model;
		public ushort wField0;
		public ushort boneCount;
		//Number of verts in a bone
		public IReadOnlyList<ushort> boneVertCounts;

		public static StratObject2PC ParseStruct(WadReader reader)
		{
			var model = new StratObject2PC();
			model.model = StratObjectPC.ParseStruct(reader);
			model.wField0 = reader.Read<ushort>();
			model.boneCount = reader.Read<ushort>();
			reader.AssertRead<uint>(0);//boneVertCounts placeholder
			return model;
		}

		public static void ParseData(WadReader reader, StratObject2PC stratObject)
		{
			stratObject.boneVertCounts = reader.ReadArray<ushort>(stratObject.boneCount);
			StratObjectPC.ParseData(reader, stratObject.model);
		}
	}
}
