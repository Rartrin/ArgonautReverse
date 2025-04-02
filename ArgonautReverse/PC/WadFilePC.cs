using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.WadChunks.PC;
using ArgonautReverse.WadChunks;
using System.Diagnostics.CodeAnalysis;

namespace ArgonautReverse.PC
{
	public sealed class WadFilePC:WADFile
	{
		public INFOChunk InfoChunk{get;private set;}
		public VERSChunk VersionChunk{get;private set;}
		public MAPChunk MapChunk{get;private set;}
		public TRAKChunk TrackChunk{get;private set;}
		public TEXTChunk TextChunk{get;private set;}
		//public LGHTChunk LightChunk{get;private set;}
		public STPCChunk StratChunk{get;private set;}
		public WFPCChunk WadflagsChunk{get;private set;}
		//public SMPCChunk SampleChunk{get;private set;}
		//public LANGChunk LanguageChunk{get;private set;}
		//public AMPCChunk AMPCChunk{get;private set;}
		public FONTChunk FontChunk{get;private set;}
		//public SPRTChunk SpriteChunk{get;private set;}
		//public RIMGChunk RIMGChunk{get;private set;}
		public ENDChunkPC EndChunk{get;private set;}

		public WadFilePC(WadVersion version, string stem, byte[] data) : base(version, stem, data){}

		public override bool TryGetChunkInfo(ChunkType chunkType, [MaybeNullWhen(false)]out BaseWADChunkInfo info)
		{
			info = chunkType switch
			{
				ChunkType.ID_PC_INFO => INFOChunkInfo.Instance,
				ChunkType.ID_PC_VERSION => VERSChunkInfo.Instance,
				ChunkType.ID_PC_MAP => MAPChunkInfo.Instance,
				ChunkType.ID_PC_TRACK => TRAKChunkInfo.Instance,
				ChunkType.ID_PC_TEXT => TEXTChunkInfo.Instance,
				//ChunkType.ID_PC_LIGHT => LGHTChunkInfo.Instance,
				ChunkType.ID_PC_STRAT => STPCChunkInfo.Instance,
				ChunkType.ID_PC_WADFLAGS => WFPCChunkInfo.Instance,

				//ChunkType.ID_PC_SAMPLE => SMPCChunkInfo.Instance,
				//ChunkType.ID_PC_LANG => LANGChunkInfo.Instance,

				//ChunkType.ID_PC_AMPC => AMPCChunkInfo.Instance,
				ChunkType.ID_PC_FONT => FONTChunkInfo.Instance,
				//ChunkType.ID_PC_SPRITE => SPRTChunkInfo.Instance,
				//ChunkType.ID_PC_RIMG => RIMGChunkInfo.Instance,

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
				//ChunkType.ID_PC_LIGHT => LightChunk,
				ChunkType.ID_PC_STRAT => StratChunk,
				ChunkType.ID_PC_WADFLAGS => WadflagsChunk,

				//ChunkType.ID_PC_SAMPLE => SampleChunk,
				//ChunkType.ID_PC_LANG => LanguageChunk,

				//ChunkType.ID_PC_AMPC => AMPCChunk,
				ChunkType.ID_PC_FONT => FontChunk,
				//ChunkType.ID_PC_SPRITE => SpriteChunk,
				//ChunkType.ID_PC_RIMG => RIMGChunk,

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
			switch(chunk.Info.ChunkType)
			{
				case ChunkType.ID_PC_INFO:EnsureEmpty(InfoChunk);InfoChunk = (INFOChunk)chunk;break;
				case ChunkType.ID_PC_VERSION:EnsureEmpty(VersionChunk);VersionChunk = (VERSChunk)chunk;break;
				case ChunkType.ID_PC_MAP:EnsureEmpty(MapChunk);MapChunk = (MAPChunk)chunk;break;
				case ChunkType.ID_PC_TRACK:EnsureEmpty(TrackChunk);TrackChunk = (TRAKChunk)chunk;break;
				case ChunkType.ID_PC_TEXT:EnsureEmpty(TextChunk);TextChunk = (TEXTChunk)chunk;break;
				//case ChunkType.ID_PC_LIGHT:EnsureEmpty(LightChunk);LightChunk = (LGHTChunk)chunk;break;
				case ChunkType.ID_PC_STRAT:EnsureEmpty(StratChunk);StratChunk = (STPCChunk)chunk;break;
				case ChunkType.ID_PC_WADFLAGS:EnsureEmpty(WadflagsChunk);WadflagsChunk = (WFPCChunk)chunk;break;

				//case ChunkType.ID_PC_SAMPLE:EnsureEmpty(SampleChunk);SampleChunk = (SMPCChunk)chunk;break;
				//case ChunkType.ID_PC_LANG:EnsureEmpty(LanguageChunk);LanguageChunk = (LANGChunk)chunk;break;

				//case ChunkType.ID_PC_AMPC:EnsureEmpty(AMPCChunk);AMPCChunk = (AMPCChunk)chunk;break;
				case ChunkType.ID_PC_FONT:EnsureEmpty(FontChunk);FontChunk = (FONTChunk)chunk;break;
				//case ChunkType.ID_PC_SPRITE:EnsureEmpty(SpriteChunk);SpriteChunk = (SPRTChunk)chunk;break;
				//case ChunkType.ID_PC_RIMG:EnsureEmpty(RIMGChunk);RIMGChunk = (RIMGChunk)chunk;break;

				case ChunkType.ID_END:EnsureEmpty(EndChunk);EndChunk = (ENDChunkPC)chunk;break;
				//default:throw new Exception("Unsupported chunk for platform");
			}
		}

		private void ExportSTRAT(ProgramArgs args, Configuration conf)
		{
			throw new NotImplementedException();
		}

		public override void ExportWadAssets(ProgramArgs args, Configuration conf)
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
			//ExportRIMG(args, conf);
			//Skip END
			throw new NotImplementedException();
		}
	}
}
