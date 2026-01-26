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
		public override WadVersion[] SupportedWadVersions { get; } = new[]
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

			var fontLookup = new FontPSX[256];
			for(var i = 0; i < 256; i++)
			{
				fontLookup[i] = FontPSX.Parse(data_in);
			}

			if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: What is this for?
				//This is in the DUMMY wads but not the main demo wads.
				var unknown = data_in.Read<int>();
			}

			var n_models_3d = data_in.Read<int>();
			var models_3d = new ObjectDataPSX[n_models_3d];
			for(int i = 0; i < n_models_3d; i++)
			{
				models_3d[i] = ObjectDataPSX.Parse(data_in);
			}

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
			var actors = new ActorDataPSX[n_actors];
			for(int i = 0; i < n_actors; i++)
			{
				actors[i] = ActorDataPSX.Parse(data_in);
			}

			var level_file = LevelFilePSX.Parse(data_in, wadFlag);

			// FIXME End of Croc 2 & Croc 2 Demo Dummy's level files aren't reversed yet
			if(data_in.ReadVersion != CROC_2_PS1.WadVersion && data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				data_in.AssertEndOfChunk(ChunkType);
			}
			return new DPSXChunk(models_3d, animationData, actors, level_file, data_in.GetAllWadData());
		}
	}

	public sealed class DPSXChunk:BaseWadChunk
	{
		public readonly IReadOnlyList<ObjectDataPSX> models_3d;
		public readonly IReadOnlyList<AnimationDataPSX> animations;
		public readonly IReadOnlyList<ActorDataPSX> actors;
		public readonly LevelFilePSX level_file;

		public DPSXChunk(ObjectDataPSX[] models_3d, AnimationDataPSX[] animations, ActorDataPSX[] actors, LevelFilePSX level_file, byte[] fallback_data = null) : base(DPSXChunkInfo.Instance, fallback_data)
		{
			this.models_3d = models_3d;
			this.animations = animations;
			this.actors = actors;
			this.level_file = level_file;
		}

		public override void PostParseSetup(WADFile wadFile)
		{
			var wadFilePSX = (WadFilePSX)wadFile;

			//Setup strat scripts
			if(level_file.map==null)
			{
				Utils.Assert(actors.Count == 0);
				return;
			}
			foreach(var strat in level_file.map.Strats)
			{
				strat.Script = GetScript(strat.AddrOffset);
			}

			//TODO: Validate this theory
			//In theory, some strats entry points may be within others, so we need to loop over multiple times
			bool processedStrats = true;
			while(processedStrats)
			{
				processedStrats = false;
				foreach(var script in actors)
				{
					if(script.ProcessScript(wadFilePSX))
					{
						processedStrats = true;
					}
				}
			}
			for(int i=0; i<actors.Count; i++)
			{
				var script = actors[i];
				if(script.EntryPointAddrs.Count == 0)
				{
					Console.WriteLine($"WARNING: Script {wadFile.Name}_{i} missing entrypoint");
				}
			}
		}

		public ActorDataPSX GetScript(int rawEntryPoint)
		{
			foreach(var curScript in actors)
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

		protected override void WriteData(WadWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}