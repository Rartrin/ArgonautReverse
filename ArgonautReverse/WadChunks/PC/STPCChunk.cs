using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class STPCChunkInfo:BaseWADChunkInfo<STPCChunk>
	{
		public static readonly STPCChunkInfo Instance = new STPCChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "PC Strats";
		public override ChunkType ChunkType => ChunkType.ID_PC_STRAT;

		private STPCChunkInfo(){}

		public override STPCChunk Parse(WadReader reader)
		{
			int modelCount = reader.Read<int>();
			var models = reader.ReadArrayWithoutMultipass<StratObject2PC>(modelCount);

			int animationCount = reader.Read<int>();
			var animations = reader.ReadArrayWithoutMultipass<AnimationStructPC>(animationCount);

			IReadOnlyList<Cutscene>? cutscenes = null;
			var wadFlags = reader.WadFile.GetChunk(WFPCChunkInfo.Instance).WadFlags;
			if((wadFlags & WadFlagPC.WAD_FLAG_100) != 0)
			{
				var cutsceneCount = reader.Read<int>();
				cutscenes = reader.ReadArray<Cutscene>(cutsceneCount);
			}
			return new STPCChunk(Instance, models, animations, cutscenes, reader.GetAllWadData());
		}
	}
	public sealed class STPCChunk(BaseWADChunkInfo info, IReadOnlyList<StratObject2PC> models, IReadOnlyList<AnimationStructPC> animations, IReadOnlyList<Cutscene>? cutscenes, byte[]? data):BaseWadChunk(info, data)
	{
		public readonly IReadOnlyList<StratObject2PC> Models = models;
		public readonly IReadOnlyList<AnimationStructPC> Animations = animations;
		public readonly IReadOnlyList<Cutscene>? Cutscenes = cutscenes;

		public StratObject2PC GetStratObject(int addr)
		{
			foreach(var model in Models)
			{
				if(model.model.WadOffset == addr)
				{
					return model;
				}
			}
			throw new Exception("StratObject for given addr was not found");
		}

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write<int>(Models.Count);
			writer.WriteArrayWithoutMultipass<StratObject2PC>(Models);

			writer.Write<int>(Animations.Count);
			writer.WriteArrayWithoutMultipass<AnimationStructPC>(Animations);

			var wadFlags = writer.WadFile.GetChunk(WFPCChunkInfo.Instance).WadFlags;
			if((wadFlags & WadFlagPC.WAD_FLAG_100) != 0)
			{
				writer.Write<int>(Cutscenes!.Count);
				writer.WriteArray<Cutscene>(Cutscenes);
			}
		}
	}
}