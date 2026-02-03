using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.WadChunks.PC;
using ArgonautReverse.WadChunks;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using ArgonautReverse.Universal;
using ArgonautReverse.OpenStratEngine;
using ArgonautReverse.Universal.StratLang;
using ArgonautReverse.Universal.StratLang.Decompiler;

namespace ArgonautReverse.PC
{
	public sealed class WadFilePC(WadVersion version, string stem, byte[] data):WADFile(version, stem, data)
	{
		public INFOChunk InfoChunk{get;private set;}
		public VERSChunk VersionChunk{get;private set;}
		public MAPChunk MapChunk{get;private set;}
		public TRAKChunk TrackChunk{get;private set;}
		public TEXTChunk TextChunk{get;private set;}
		public LGHTChunk LightChunk{get;private set;}
		public STPCChunk StratChunk{get;private set;}
		public WFPCChunk WadflagsChunk{get;private set;}
		public SMPCChunk SampleChunk{get;private set;}
		public LGPCChunk LanguageChunk{get;private set;}
		public AMPCChunk AMPCChunk{get;private set;}
		public FONTChunk FontChunk{get;private set;}
		public SPRTChunk SpriteChunk{get;private set;}
		public RIMGChunk RIMGChunk{get;private set;}
		public ENDChunkPC EndChunk{get;private set;}

		public override bool TryGetChunkInfo(ChunkType chunkType, [MaybeNullWhen(false)]out BaseWADChunkInfo info)
		{
			info = chunkType switch
			{
				ChunkType.ID_PC_INFO => INFOChunkInfo.Instance,
				ChunkType.ID_PC_VERSION => VERSChunkInfo.Instance,
				ChunkType.ID_PC_MAP => MAPChunkInfo.Instance,
				ChunkType.ID_PC_TRACK => TRAKChunkInfo.Instance,
				ChunkType.ID_PC_TEXT => TEXTChunkInfo.Instance,
				ChunkType.ID_PC_LIGHT => LGHTChunkInfo.Instance,
				ChunkType.ID_PC_STRAT => STPCChunkInfo.Instance,
				ChunkType.ID_PC_WADFLAGS => WFPCChunkInfo.Instance,

				ChunkType.ID_PC_SAMPLE => SMPCChunkInfo.Instance,
				ChunkType.ID_PC_LANG => LGPCChunkInfo.Instance,

				ChunkType.ID_PC_AMPC => AMPCChunkInfo.Instance,
				ChunkType.ID_PC_FONT => FONTChunkInfo.Instance,
				ChunkType.ID_PC_SPRITE => SPRTChunkInfo.Instance,
				ChunkType.ID_PC_RIMG => RIMGChunkInfo.Instance,

				ChunkType.ID_END => ENDChunkPCInfo.Instance,
				_ => null
			};
			return info != null;
		}
		public override T GetChunk<T>(BaseWADChunkInfo<T> info)
		{
			return (T)(BaseWadChunk)(info.ChunkType switch
			{
				ChunkType.ID_PC_INFO => InfoChunk,
				ChunkType.ID_PC_VERSION => VersionChunk,
				ChunkType.ID_PC_MAP => MapChunk,
				ChunkType.ID_PC_TRACK => TrackChunk,
				ChunkType.ID_PC_TEXT => TextChunk,
				ChunkType.ID_PC_LIGHT => LightChunk,
				ChunkType.ID_PC_STRAT => StratChunk,
				ChunkType.ID_PC_WADFLAGS => WadflagsChunk,

				ChunkType.ID_PC_SAMPLE => SampleChunk,
				ChunkType.ID_PC_LANG => LanguageChunk,

				ChunkType.ID_PC_AMPC => AMPCChunk,
				ChunkType.ID_PC_FONT => FontChunk,
				ChunkType.ID_PC_SPRITE => SpriteChunk,
				ChunkType.ID_PC_RIMG => RIMGChunk,

				ChunkType.ID_END => EndChunk,
				_ => throw new Exception($"Unknown type: {info.ChunkType}")
			});
		}

		private static void EnsureEmpty(BaseWadChunk chunk)
		{
			if(chunk != null)
			{
				throw new Exception("Chunk already exists");
			}
		}

		public override void AddChunk(BaseWadChunk chunk)
		{
			base.AddChunk(chunk);
			switch(chunk.Info.ChunkType)
			{
				case ChunkType.ID_PC_INFO:EnsureEmpty(InfoChunk);InfoChunk = (INFOChunk)chunk;break;
				case ChunkType.ID_PC_VERSION:EnsureEmpty(VersionChunk);VersionChunk = (VERSChunk)chunk;break;
				case ChunkType.ID_PC_MAP:EnsureEmpty(MapChunk);MapChunk = (MAPChunk)chunk;break;
				case ChunkType.ID_PC_TRACK:EnsureEmpty(TrackChunk);TrackChunk = (TRAKChunk)chunk;break;
				case ChunkType.ID_PC_TEXT:EnsureEmpty(TextChunk);TextChunk = (TEXTChunk)chunk;break;
				case ChunkType.ID_PC_LIGHT:EnsureEmpty(LightChunk);LightChunk = (LGHTChunk)chunk;break;
				case ChunkType.ID_PC_STRAT:EnsureEmpty(StratChunk);StratChunk = (STPCChunk)chunk;break;
				case ChunkType.ID_PC_WADFLAGS:EnsureEmpty(WadflagsChunk);WadflagsChunk = (WFPCChunk)chunk;break;

				case ChunkType.ID_PC_SAMPLE:EnsureEmpty(SampleChunk);SampleChunk = (SMPCChunk)chunk;break;
				case ChunkType.ID_PC_LANG:EnsureEmpty(LanguageChunk);LanguageChunk = (LGPCChunk)chunk;break;

				case ChunkType.ID_PC_AMPC:EnsureEmpty(AMPCChunk);AMPCChunk = (AMPCChunk)chunk;break;
				case ChunkType.ID_PC_FONT:EnsureEmpty(FontChunk);FontChunk = (FONTChunk)chunk;break;
				case ChunkType.ID_PC_SPRITE:EnsureEmpty(SpriteChunk);SpriteChunk = (SPRTChunk)chunk;break;
				case ChunkType.ID_PC_RIMG:EnsureEmpty(RIMGChunk);RIMGChunk = (RIMGChunk)chunk;break;

				case ChunkType.ID_END:EnsureEmpty(EndChunk);EndChunk = (ENDChunkPC)chunk;break;
				default:
				{
					//throw new Exception("Unsupported chunk for platform");
					Console.WriteLine($"Skipping unsupported chunk: {chunk.Info.ChunkType}");
					break;
				}
			}
		}

		public static WadFilePC FromOSE(WadFileOSE ose, Configuration config)
		{
			var ret = new WadFilePC(config.WriteVersion.GetWadVersion(null), ose.Stem, null);
			foreach(var oseChunk in ose.Chunks)
			{
				ret.AddChunk(oseChunk switch
				{
					_ => throw new NotImplementedException(),
				});
			}
			return ret;
		}

		public override void ExtractAssets(ProgramArgs args, Configuration conf)
		{
			//Skip INFO
			//Skip VERSION
			//ExportMAP(args, conf);
			//ExportTRACK(args, conf);
			//ExportTEXT(args, conf);
			//ExportLIGHT(args, conf);
			ExportSTRAT(args, conf);
			//Skip WADFLAGS
			//ExportSAMPLE(args, conf);
			//ExportLANG(args, conf);
			//ExportAMPC(args, conf);
			//ExportFONT(args, conf);
			//ExportSPRITE(args, conf);
			//RIMGChunk.Export(this, args, conf);
			//Skip END
		}

		private void ExportTEXT(ProgramArgs args, Configuration conf)
		{
			if(!args.ExtractTextures){return;}

			var textureDirectory = args.GetExtractDirectory(Stem, "Textures");
			for(int i=0; i<TextChunk.Textures.Count; i++)
			{
				var texture = TextChunk.Textures[i];
				ExtractTexture(texture, Path.Join(textureDirectory, $"{i}.png"));
			}

			//var spriteDirectory = args.GetExtractDirectory(Stem, "Sprites");
			//for(int i=0; i<TextChunk.Sprites.Count; i++)
			//{
			//	var sprite = TextChunk.Sprites[i];
			//	ExtractTexture(sprite, Path.Join(spriteDirectory, $"{i}.png"));
			//}
		}

		public unsafe void ExtractTexture(TextureStructPC texture, string path)
		{
			var abgrPixels = texture.pixels;
			var argbPixels = stackalloc ColorARGB555[abgrPixels.Length];
			for(int i=0; i<abgrPixels.Length; i++)
			{
				argbPixels[i] = new ColorABGR555(abgrPixels[i]).ToRGB555();
			}
			//Save before leaving. Bitmap seem to load lazily meaning there could be pointer degradation regardless of stackalloc or fixed.
			var ret = new Bitmap(texture.Width, texture.Height, texture.Width * sizeof(ColorARGB555), PixelFormat.Format16bppArgb1555, (nint)argbPixels);
			ret.Save(path, ImageFormat.Png);
		}

		public void ExtractSPRITE(SpriteStructPC sprite, string path)
		{
			throw new NotImplementedException();
		}

		private void ExportSTRAT(ProgramArgs args, Configuration conf)
		{
			ExportScripts(args, conf);
		}

		public void ExportScripts(ProgramArgs args, Configuration conf)
		{
			if(!args.ExtractScripts){return;}

			var scriptsDirectory = args.GetExtractDirectory(Stem, "Scripts");


			var scriptData = new (string? baseFilePath,bool parsingSucceeded,StackAnalyzer? stackAnalyzer,FlowAnalyzer? flowAnalyzer)[StratChunk.Scripts.Count];
			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				var baseFilePath = Path.Join(scriptsDirectory, $"STRAT_{i}");

				//TODO: SKIP EVERYTHING EXCEPT for this WalkingCroc for testing
				//if((Stem, i) != ("0015F800",4)){continue;}

				var script = StratChunk.Scripts[i];
				
				
				//TODO: Make exporting ASM an argument
				script.ExportAsm(baseFilePath);

				if(script.Failed || !script.Processed)
				{
					continue;
				}
				data.baseFilePath = baseFilePath;
			}

			var parser = new AsmParser();
			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(data.baseFilePath == null){continue;}
				parser.AddScript(StratChunk.Scripts[i]);
			}

			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(data.baseFilePath == null){continue;}
				try
				{
					parser.ParseAndSetupInstructions(StratChunk.Scripts[i]);
					data.parsingSucceeded = true;
				}
				catch(Exception e)
				{
					//TODO: This could possibly break parsing.
					Console.WriteLine($"Failed to setup asm parser {data.baseFilePath}:");
					Console.WriteLine(e.Message);
					continue;
				}
			}

			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(!data.parsingSucceeded){continue;}
				try
				{
					//TODO: StackAanalyzer likely should be consistant across all scripts.
					//Failing during analysis could invalidate the values in the analyzer though.
					var stackAnalyzer = new StackAnalyzer();
					stackAnalyzer.Analyze(parser.GetSubroutines(StratChunk.Scripts[i]));

					data.stackAnalyzer = stackAnalyzer;
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to analyze stack {data.baseFilePath}:");
					Console.WriteLine(e.Message);
					continue;
				}
				var stackOutputLines = new List<string>();
				data.stackAnalyzer.Write(stackOutputLines);

				File.WriteAllLines($"{data.baseFilePath}.stack.strat", stackOutputLines);
			}
			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(data.stackAnalyzer == null){continue;}
				try
				{
					var flowAnalyzer = new FlowAnalyzer();
					flowAnalyzer.Analyze(data.stackAnalyzer.Subroutines);
					data.flowAnalyzer = flowAnalyzer;
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to analyze flow {data.baseFilePath}:");
					Console.WriteLine(e.Message);
					continue;
				}
				var flowWriter = new Writer();
				data.flowAnalyzer.Write(flowWriter);

				File.WriteAllLines($"{data.baseFilePath}.flow.strat", flowWriter.GetLines());
			}
		}

		public override (Script script, InstructionAddress address) GetStratProcAddr(int dataOffset)
		{
			//On PC, this is a STPC chunk offset.
			var script = StratChunk.GetScript(dataOffset);
			return (script, (InstructionAddress)(dataOffset - script.DataChunkAddress));
		}

		public override void ProcessScripts()
		{
			StratChunk.ProcessScipts(this);
		}
	}
}