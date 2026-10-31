using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.PSX;

namespace ArgonautReverse.WadChunks.PSX
{
	public sealed class DPSXChunkInfo:BaseWADChunkInfo<DPSXChunk>
	{
		public static readonly DPSXChunkInfo Instance = new DPSXChunkInfo();
		public override ChunkType ChunkType => ChunkType.ID_PSX_DATA;
		public override WadVersion[] SupportedWadVersions{get;} = new[]
		{
			CROC_2_PS1.DatVersion,
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion
		}.SelectMany(datVersion => datVersion.WadVersions).Distinct().ToArray();

		public override string ChunkDescription => "3D models, animations & level geometry";

		public override DPSXChunk Parse(WadReader data_in)
		{
			//TODO: WadFlag and SpriteOffset
			WadFlagPSX wadFlags = (WadFlagPSX)data_in.Read<uint>();

			//TODO: Why are these also in TPSX?
			var spriteOffset = data_in.Read<int>();

			var fontLookup = data_in.ReadArray<FontPSX>(256);

			if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: What is this for?
				//This is in the DUMMY wads but not the main demo wads.
				var unknown = data_in.Read<int>();
			}

			var n_models_3d = data_in.Read<int>();
			var models_3d = data_in.ReadArray<ObjectDataPSX>(n_models_3d);

			var n_animations = data_in.Read<int>();
			var animations = data_in.ReadArrayWithoutMultipass<AnimationPSX>(n_animations);
			var animationData = new AnimationDataPSX[n_animations];
			for(int i = 0; i < n_animations; i++)
			{
				animationData[i] = AnimationDataPSX.Parse(animations[i]);
			}

			if((wadFlags & WadFlagPSX.WF_HASCUTSCENES) != 0)//if(data_in.Version==CROC_2_PS1.Instance || data_in.Version==CROC_2_DEMO_PS1.Instance)
			{
				//TODO: Cutscene data
				throw new NotImplementedException();
			}

			SimpleImagePSX[] heads;
			if((wadFlags & WadFlagPSX.WF_HASHEADS) != 0)
			{
				var headCount = data_in.Read<int>();
				heads = data_in.ReadArray<SimpleImagePSX>(headCount);
			}
			else
			{
				heads = [];
			}

			var n_actors = data_in.Read<int>();
			var actors = data_in.ReadArray<ActorDataPSX>(n_actors);

			var level_file = LevelFilePSX.Parse(data_in, wadFlags);

			if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: End of Demo Dummy's level files aren't reversed yet
				data_in.AssertEndOfChunk(ChunkType, warn:true);
			}
			else
			{
				data_in.AssertEndOfChunk(ChunkType);
			}
			return new DPSXChunk(wadFlags, spriteOffset, fontLookup, models_3d, animations, animationData, heads, actors, level_file, data_in.GetAllWadData());
		}
	}

	public sealed class DPSXChunk(WadFlagPSX wadFlags, int spriteOffset, FontPSX[] fontLookup, ObjectDataPSX[] models3D, IReadOnlyList<AnimationPSX> animations, AnimationDataPSX[] animationData, SimpleImagePSX[] heads, ActorDataPSX[] actors, LevelFilePSX levelFile, byte[]? fallback_data = null):BaseWadChunk(DPSXChunkInfo.Instance, fallback_data)
	{
		public readonly IReadOnlyList<ObjectDataPSX> Models3D = models3D;
		public readonly IReadOnlyList<AnimationPSX> Animations = animations;
		public readonly IReadOnlyList<AnimationDataPSX> AnimationData = animationData;
		public readonly IReadOnlyList<SimpleImagePSX> Heads = heads;
		public readonly IReadOnlyList<ActorDataPSX> Actors = actors;
		public readonly LevelFilePSX LevelFile = levelFile;

		public override void PostParseSetup(WADFile wadFile)
		{
			//Setup strat scripts
			if(LevelFile.map==null)
			{
				Utils.Assert(Actors.Count == 0);
			}
		}

		public ActorDataPSX GetScript(int rawEntryPoint)
		{
			foreach(var curScript in Actors)
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

		protected override void WriteData(ChunkWriter writer)
		{
			//TODO: Build WadFlags based on data.
			writer.Write<uint>((uint)wadFlags);

			//TODO: Why are these also in TPSX?
			writer.Write<int>(spriteOffset);

			writer.WriteSizedArray<FontPSX>(256, fontLookup);

			if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//writer.Write<int>(unknown);
				throw new Exception();
			}

			writer.Write<int>(Models3D.Count);
			writer.WriteArray<ObjectDataPSX>(Models3D);

			writer.Write<int>(AnimationData.Count);
			writer.WriteArrayWithoutMultipass<AnimationPSX>(Animations);

			if((wadFlags & WadFlagPSX.WF_HASCUTSCENES) != 0)//if(data_in.Version==CROC_2_PS1.Instance || data_in.Version==CROC_2_DEMO_PS1.Instance)
			{
				//TODO: Cutscene data
				throw new NotImplementedException();
			}

			if((wadFlags & WadFlagPSX.WF_HASHEADS) != 0)
			{
				writer.Write<int>(Heads.Count);
				writer.WriteArray<SimpleImagePSX>(Heads);
			}

			writer.Write<int>(Actors.Count);
			writer.WriteArray<ActorDataPSX>(Actors);

			LevelFile.Write(writer, wadFlags);
		}

		public void ProcessScipts(WADFile wadFile)
		{
			foreach(var strat in LevelFile.map.Strats)
			{
				strat.Script = GetScript(strat.AddrOffset);
			}

			//TODO: Validate this theory
			//In theory, some strats entry points may be within others, so we need to loop over multiple times
			bool processedStrats = true;
			while(processedStrats)
			{
				processedStrats = false;
				foreach(var script in Actors)
				{
					if(script.ProcessScript())
					{
						processedStrats = true;
					}
				}
			}
			for(int i=0; i<Actors.Count; i++)
			{
				var script = Actors[i];
				if(script.EntryPointAddrs.Count == 0)
				{
					Console.WriteLine($"WARNING: Script {wadFile.Name}_{i} missing entrypoint");
				}
			}
		}
	}
}