using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public sealed class Cutscene:IReadable<Cutscene>
	{
		public readonly string FileExtension;//char[4]
		//public readonly int ObjCount;
		public readonly IReadOnlyList<CutsceneInfoPC> InfoList;
		public readonly IReadOnlyList<StratObject2PC> ObjList;

		public Cutscene(string fileExtension, IReadOnlyList<CutsceneInfoPC> infoList, IReadOnlyList<StratObject2PC> objList)
		{
			this.FileExtension = fileExtension;
			//this.arraySize = arraySize;
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

			return new Cutscene(fileExtension, infoList, objList);
		}
	}

	public sealed class CutsceneInfoPC:IReadable<CutsceneInfoPC>
	{
		public readonly int MorphCount;
		public readonly int BoneCount;

		public CutsceneInfoPC(int morphCount, int boneCount)
		{
			this.MorphCount = morphCount;
			this.BoneCount = boneCount;
		}

		public static CutsceneInfoPC Parse(WadReader reader)
		{
			var morphCount = reader.Read<int>();
			var boneCount = reader.Read<int>();
			return new CutsceneInfoPC(morphCount, boneCount);
		}
	}
}