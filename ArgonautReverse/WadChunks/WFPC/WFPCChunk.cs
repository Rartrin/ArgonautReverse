using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.WFPC
{
	[Flags]
	public enum WadFlagsPC:uint
	{
		WAD_FLAG_1 = (1<<0),//Aladdin PC: MAP
		WAD_FLAG_2 = (1<<1),//Aladdin PC: BACKGROUND
		WAD_FLAG_4 = (1<<2),//Aladdin PC: HASDEFSTRAT
		WAD_FLAG_8 = (1<<3),//Aladdin PC: MAP_PRELIT
		WAD_FLAG_HAS_PARTICLES = (1<<4),//Aladdin PC: MAP_DEPTHCUED
		WAD_FLAG_20 = (1<<5),//Aladdin PC: PACKETSIZE_SMALL
		WAD_FLAG_40 = (1<<6),//Aladdin PC: PACKETSIZE_BIG
		WAD_FLAG_80 = (1<<7),//Aladdin PC: HASLANGUAGES
		WAD_FLAG_100 = (1<<8),//Aladdin PC: HASCUTSCENES
		WAD_FLAG_200 = (1<<9),
		WAD_FLAG_400 = (1<<10),
		WAD_FLAG_800 = (1<<11),
		WAD_FLAG_1000 = (1<<12),
		WAD_FLAG_2000 = (1<<13),
		WAD_FLAG_4000 = (1<<14),
		WAD_FLAG_8000 = (1<<15),
		WAD_FLAG_HAS_CHANGING_GEOMETRY = (1<<16),//WF_HASOTHERPIECES
		WAD_FLAG_20000 = (1<<17),
		WAD_FLAG_40000 = (1<<18),
		WAD_FLAG_80000 = (1<<19),
		WAD_FLAG_100000 = (1<<20),
		WAD_FLAG_200000 = (1<<21),
		WAD_FLAG_400000 = (1<<22),
		WAD_FLAG_800000 = (1<<23),
		WAD_FLAG_1000000 = (1<<24),
		WAD_FLAG_2000000 = (1<<25),
		WAD_FLAG_4000000 = (1<<26),
		WAD_FLAG_8000000 = (1<<27),
		WAD_FLAG_10000000 = (1<<28),
		WAD_FLAG_20000000 = (1<<29),
		WAD_FLAG_40000000 = (1<<30),
		WAD_FLAG_80000000 = (1u<<31),
	};

	public sealed class WFPCChunkInfo:BaseWADChunkInfo
	{
		public static readonly WFPCChunkInfo Instance = new WFPCChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "WadFlags";
		public override ChunkType ChunkType => ChunkType.ID_PC_WADFLAGS;

		private WFPCChunkInfo(){}

		public override BaseWadChunk Parse(WadReader reader)
		{
			var wadFlags = (WadFlagsPC)reader.Read<uint>();
			reader.AssertEndOfChunk(ChunkType);
			return new WFPCChunk(this, wadFlags);
		}

	}
	public sealed class WFPCChunk:BaseWadChunk
	{
		public WadFlagsPC WadFlags{get;}
		public WFPCChunk(BaseWADChunkInfo info, WadFlagsPC wadFlags):base(info)
		{
			WadFlags = wadFlags;
		}
	}
}