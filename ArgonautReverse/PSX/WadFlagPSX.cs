using ArgonautReverse.OpenStratEngine;

namespace ArgonautReverse.PSX
{
	[Flags]
	public enum WadFlagPSX:uint
	{
		WF_MAP = 1 << 0,
		WF_BACKGROUND = 1 << 1,
		WF_LOCALPOOLSIZE = 1 << 2,
		WF_MAP_PRELIT = 1 << 3,
		WF_PARTICLESIZE = 1 << 4,
		WF_PACKETSIZE_SMALL = 1 << 5,
		WF_PACKETSIZE_BIG = 1 << 6,
		WF_HASLANGUAGES = 1 << 7,
		WF_HASCUTSCENES = 1 << 8,
		WF_HASMULTIAMBIENT = 1 << 9,
		WF_USESZONES = 1 << 10,
		WF_DOUBLEORDERTABLE = 1 << 11,
		WF_NEWZONES = 1 << 12,
		WF_BIT13 = 1 << 13,
		WF_CANSWIM = 1 << 14,
		WF_MAP_LIGHTINGTABLE = 1 << 15,
		WF_HASOTHERPIECES = 1 << 16,
		WF_OVERRIDEPACKETSIZE = 1 << 17,
		WF_HASHEADS = 1 << 18,
		WF_SWIMMINGLEVEL = 1 << 19,
		WF_HASINVENTORY = 1 << 20,
		WF_CAMERAPOINTS = 1 << 21,
		WF_FASTWATER = 1 << 22,
		WF_NOMATPOS = 1 << 23,
		WF_HASEXTRAINVENTORY = 1 << 24,
		WF_HASFULLSETOFICONS = 1 << 25,
		WF_HASSMALLHEADS = 1 << 26,
	}

	public static class WadFlagsPSXExtentions
	{
		private static readonly (WadFlagPSX psx,WadFlagsOSE ose)[] mapping =
		{
			(WadFlagPSX.WF_BACKGROUND, WadFlagsOSE.Background),

			(WadFlagPSX.WF_PARTICLESIZE, WadFlagsOSE.ParticleSize),

			(WadFlagPSX.WF_HASOTHERPIECES, WadFlagsOSE.HasOtherPieces),
		};

		public static WadFlagsOSE ToOSE(this WadFlagPSX that)
		{
			WadFlagsOSE ret = 0;
			foreach((var psx, var ose) in mapping)
			{
				if((that & psx)!=0){ret |= ose;}
			}
			return ret;
		}
	}
}
