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
		}.SelectMany(datVersion => datVersion.WadVersions).ToArray();

		public override string ChunkDescription => "3D models, animations & level geometry";

		public override DPSXChunk Parse(WadReader data_in)
		{
			//TODO: WadFlag and SpriteOffset
			WadFlagPSX wadFlag = (WadFlagPSX)data_in.Read<uint>();

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

			if((wadFlag & WadFlagPSX.WF_HASCUTSCENES) != 0)//if(data_in.Version==CROC_2_PS1.Instance || data_in.Version==CROC_2_DEMO_PS1.Instance)
			{
				//TODO: Cutscene data
				//This probably shouldn't be a fixed amount
				var n_dpsx_legacy_textures = data_in.Read<int>();
				data_in.SkipBytes(n_dpsx_legacy_textures * 3072);
			}

			if((wadFlag & WadFlagPSX.WF_HASHEADS) != 0)
			{
				throw new NotImplementedException();
			}

			var n_actors = data_in.Read<int>();
			var actors = data_in.ReadArray<ActorDataPSX>(n_actors);

			var level_file = LevelFilePSX.Parse(data_in, wadFlag);

			// FIXME End of Croc 2 & Croc 2 Demo Dummy's level files aren't reversed yet
			if(data_in.ReadVersion != CROC_2_PS1.WadVersion && data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				data_in.AssertEndOfChunk(ChunkType);
			}
			return new DPSXChunk(models_3d, animationData, actors, level_file, data_in.GetAllWadData());
		}
	}

	public sealed class DPSXChunk(ObjectDataPSX[] models3D, AnimationDataPSX[] animations, ActorDataPSX[] actors, LevelFilePSX levelFile, byte[]? fallback_data = null):BaseWadChunk(DPSXChunkInfo.Instance, fallback_data)
	{
		public readonly IReadOnlyList<ObjectDataPSX> Models3D = models3D;
		public readonly IReadOnlyList<AnimationDataPSX> Animations = animations;
		public readonly IReadOnlyList<ActorDataPSX> Actors = actors;
		public readonly LevelFilePSX LevelFile = levelFile;

		public override void PostParseSetup(WADFile wadFile)
		{
			//Setup strat scripts
			if(LevelFile.map==null)
			{
				Utils.Assert(Actors.Count == 0);
				return;
			}
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
			throw new NotImplementedException();
		}
	}
}