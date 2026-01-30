using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public sealed class Cutscene:IReadable<Cutscene>,IWritable
	{
		public readonly string FileExtension;//char[4]
		public readonly int ObjCount;
		public readonly IReadOnlyList<CutsceneInfoPC> InfoList;
		public readonly IReadOnlyList<StratObject2PC> ObjList;

		public Cutscene(string fileExtension, int objCount, IReadOnlyList<CutsceneInfoPC> infoList, IReadOnlyList<StratObject2PC> objList)
		{
			this.FileExtension = fileExtension;
			this.ObjCount = objCount;
			if(infoList.Count != objList.Count)
			{
				throw new Exception();
			}
			this.InfoList = infoList;
			this.ObjList = objList;
		}

		public static Cutscene Parse(WadReader reader)
		{
			var fileExtension = reader.ReadString(4);
			var objCount = reader.Read<int>();
			reader.AssertRead<uint>(0);//InfoList placeholder
			reader.AssertRead<uint>(0);//ObjList placeholder

			var infoList = reader.ReadArray<CutsceneInfoPC>(objCount);
			var objList = reader.ReadArrayMultipass<StratObject2PC>(objCount);

			return new Cutscene(fileExtension, objCount, infoList, objList);
		}

		public void Write(WadWriter writer)
		{
			writer.WriteString(4, FileExtension);
			writer.Write<int>(ObjCount);
			writer.Write<uint>(0);//InfoList placeholder
			writer.Write<uint>(0);//ObjList placeholder

			writer.WriteSizedArray<CutsceneInfoPC>(ObjCount, InfoList);
			writer.WriteSizedArrayMultipass<StratObject2PC>(ObjCount, ObjList);
		}
	}

	public sealed class CutsceneInfoPC(int morphCount, int boneCount):IReadable<CutsceneInfoPC>,IWritable
	{
		public readonly int MorphCount = morphCount;
		public readonly int BoneCount = boneCount;

		public static CutsceneInfoPC Parse(WadReader reader)
		{
			var morphCount = reader.Read<int>();
			var boneCount = reader.Read<int>();
			return new CutsceneInfoPC(morphCount, boneCount);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<int>(MorphCount);
			writer.Write<int>(BoneCount);
		}
	}
}