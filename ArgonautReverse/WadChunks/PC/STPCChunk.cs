using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class STPCChunkInfo:BaseWADChunkInfo<STPCChunk>
	{
		public static readonly STPCChunkInfo Instance = new STPCChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.ParsableWadsPC;
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
			if((wadFlags & WadFlagPC.HasCutscenes) != 0)
			{
				var cutsceneCount = reader.Read<int>();
				cutscenes = reader.ReadArray<Cutscene>(cutsceneCount);
			}

			//There is no guarentee that there couldn't be other data before or after this. It just happened to work.
			var scriptCount = reader.Read<int>();
			var scripts = reader.ReadArray<ScriptPC>(scriptCount);

			reader.AssertEndOfChunk(ChunkType);
			return new(Instance, models, animations, cutscenes, scripts, reader.GetAllWadData());
		}
	}
	public sealed class STPCChunk(BaseWADChunkInfo info, IReadOnlyList<StratObject2PC> models, IReadOnlyList<AnimationStructPC> animations, IReadOnlyList<Cutscene>? cutscenes, IReadOnlyList<ScriptPC> scripts, byte[]? data):BaseWadChunk(info, data)
	{
		public readonly IReadOnlyList<StratObject2PC> Models = models;
		public readonly IReadOnlyList<AnimationStructPC> Animations = animations;
		public readonly IReadOnlyList<Cutscene>? Cutscenes = cutscenes;
		public readonly IReadOnlyList<ScriptPC> Scripts = scripts;

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
			if((wadFlags & WadFlagPC.HasCutscenes) != 0)
			{
				writer.Write<int>(Cutscenes!.Count);
				writer.WriteArray<Cutscene>(Cutscenes);
			}
		}

		public ScriptPC GetScript(int rawEntryPoint)
		{
			foreach(var curScript in Scripts)
			{
				if(curScript.DataChunkAddress<=rawEntryPoint && rawEntryPoint<curScript.DataChunkAddress+curScript.DataChunkLength)
				{
					var entryPoint = rawEntryPoint-curScript.DataChunkAddress;
					if(!curScript.EntryPointAddrs.Contains(entryPoint))
					{
						curScript.EntryPointAddrs.Add(entryPoint);
					}
					return curScript;
				}
			}
			throw new Exception("Entry point outside of known scripts");
		}

		public void ProcessScipts(WADFile wadFile)
		{
            //TODO: Validate this theory
			//In theory, some strats entry points may be within others, so we need to loop over multiple times
			bool processedStrats = true;
			while(processedStrats)
			{
				processedStrats = false;
				for(int i=0; i<Scripts.Count; i++)
				{
					var script = Scripts[i];
					if(script.ProcessScript())
					{
						processedStrats = true;
					}
				}
			}
			for(int i=0; i<Scripts.Count; i++)
			{
				var script = Scripts[i];
				if(script.EntryPointAddrs.Count == 0)
				{
					Console.WriteLine($"WARNING: Script {wadFile.Name}_{i} missing entrypoint");
				}
			}
		}
	}
}