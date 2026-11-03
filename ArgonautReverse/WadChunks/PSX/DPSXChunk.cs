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

		public override DPSXChunk Parse(WadReader reader)
		{
			//TODO: WadFlag and SpriteOffset
			WadFlagPSX wadFlags = (WadFlagPSX)reader.Read<uint>();

			//TODO: Why are these also in TPSX?
			var spriteOffset = reader.Read<int>();

			var fontLookup = reader.ReadArray<FontPSX>(256);

			if(reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: What is this for?
				//This is in the DUMMY wads but not the main demo wads.
				var unknown = reader.Read<int>();
			}

			var stratObjectCount = reader.Read<int>();
			var stratObjects = reader.ReadArray<ObjectPSX>(stratObjectCount);

			var n_animations = reader.Read<int>();
			var animations = reader.ReadArrayWithoutMultipass<AnimationPSX>(n_animations);

			if((wadFlags & WadFlagPSX.WF_HASCUTSCENES) != 0)//if(reader.Version==CROC_2_PS1.Instance || reader.Version==CROC_2_DEMO_PS1.Instance)
			{
				//TODO: Cutscene data
				throw new NotImplementedException();
			}

			SimpleImagePSX[] heads;
			if((wadFlags & WadFlagPSX.WF_HASHEADS) != 0)
			{
				var headCount = reader.Read<int>();
				heads = reader.ReadArray<SimpleImagePSX>(headCount);
			}
			else
			{
				heads = [];
			}

			var n_actors = reader.Read<int>();
			var actors = reader.ReadArray<ActorDataPSX>(n_actors);

			var trackObjectCount = reader.Read<int>();
			var trackObjects = reader.ReadArrayMultipass<TObjectPSX>(trackObjectCount);

			uint localPoolSize = 8;
			if((wadFlags & WadFlagPSX.WF_LOCALPOOLSIZE) != 0)
			{
				if(reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new NotImplementedException();
				}
				localPoolSize = reader.Read<uint>();
			}
			localPoolSize <<= 10;

			uint maxParticles = 200;
			if((wadFlags & WadFlagPSX.WF_PARTICLESIZE) != 0)
			{
				if(reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//Nothing here in dummy. Likly this just indicates to use a different hardcoded count.
				}
				else
				{
					maxParticles = reader.Read<uint>();
				}
			}

			//Track pieces
			var n_sub_chunks = reader.Read<int>();

			MapPSX? map = null;
			if(n_sub_chunks != 0)
			{
				map = MapPSX.Parse(reader, wadFlags, trackObjects);
				Utils.Assert(n_sub_chunks == map.NumberOfPieces);
			}
			if(reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: End of Demo Dummy's level files aren't reversed yet
				reader.AssertEndOfChunk(ChunkType, warn:true);
			}
			else
			{
				reader.AssertEndOfChunk(ChunkType);
			}

			var chunk = new DPSXChunk(reader.GetAllWadData())
			{
				WadFlags = wadFlags,
				SpriteOffset = spriteOffset,
				FontLookup = fontLookup,
				StratObjects = stratObjects,
				Animations = animations,
				Heads = heads,
				Actors = actors,
				TrackObjects = trackObjects,
				LocalPoolSize = localPoolSize,
				MaxParticles = maxParticles,
				Map = map,
			};
			chunk.LevelFile = LevelFilePSX.Setup(chunk, reader);
			return chunk;
		}
	}

	public sealed class DPSXChunk(byte[]? fallback_data = null):BaseWadChunk(DPSXChunkInfo.Instance, fallback_data)
	{
		public required WadFlagPSX WadFlags{get;init;}
		public required int SpriteOffset{get;init;}
		public required FontPSX[] FontLookup{get;init;}
		public required ObjectPSX[] StratObjects{get;init;}
		public required AnimationPSX[] Animations{get;init;}
		public required IReadOnlyList<SimpleImagePSX> Heads{get;init;}
		public required IReadOnlyList<ActorDataPSX> Actors{get;init;}
		public required TObjectPSX[] TrackObjects{get;init;}
		public required uint LocalPoolSize{get;init;}
		public required uint MaxParticles{get;init;}
		public required MapPSX? Map{get;init;}

		//Data for exporting
		public LevelFilePSX LevelFile;

		public override void PostParseSetup(WADFile wadFile)
		{
			//Setup strat scripts
			if(Map == null)
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
			if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion){throw new Exception("Writing DUMMY wads is not supported.");}

			//TODO: Build WadFlags based on data.
			writer.Write<uint>((uint)WadFlags);

			//TODO: Why are these also in TPSX?
			writer.Write<int>(SpriteOffset);

			writer.WriteSizedArray<FontPSX>(256, FontLookup);

			writer.Write<int>(StratObjects.Length);
			writer.WriteArray<ObjectPSX>(StratObjects);

			writer.Write<int>(Animations.Length);
			writer.WriteArrayWithoutMultipass<AnimationPSX>(Animations);

			if((WadFlags & WadFlagPSX.WF_HASCUTSCENES) != 0)
			{
				//TODO: Cutscene data
				throw new NotImplementedException();
			}

			if((WadFlags & WadFlagPSX.WF_HASHEADS) != 0)
			{
				writer.Write<int>(Heads.Count);
				writer.WriteArray<SimpleImagePSX>(Heads);
			}

			writer.Write<int>(Actors.Count);
			writer.WriteArray<ActorDataPSX>(Actors);

			//Track Objects
			writer.Write<int>(TrackObjects.Length);
			writer.WriteArrayMultipass<TObjectPSX>(TrackObjects);

			if((WadFlags & WadFlagPSX.WF_LOCALPOOLSIZE) != 0)
			{
				writer.Write<uint>(LocalPoolSize >> 10);
			}

			if((WadFlags & WadFlagPSX.WF_PARTICLESIZE) != 0)
			{
				writer.Write<uint>(MaxParticles);
			}
			if(Map != null)
			{
				writer.Write<int>(Map.NumberOfPieces);
				Map.Write(writer, WadFlags, TrackObjects);
			}
			else
			{
				writer.Write<int>(0);
			}
		}

		public void ProcessScipts(WADFile wadFile)
		{
			if(Map != null)
			{
				foreach(var strat in Map.Strats)
				{
					strat.Script = GetScript(strat.AddrOffset);
				}
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