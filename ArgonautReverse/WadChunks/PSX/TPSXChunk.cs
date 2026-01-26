using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX;

namespace ArgonautReverse.WadChunks.PSX
{
	public enum TextureFlag:int
	{
		CompressedTPage = 1 << 0,
		Compressed16Bit = 1 << 1,
		HasLevelName = 1 << 2,
		HasMemoryCardIcons = 1 << 3,
		HasLongLevelName = 1 << 4,//Includes translated names
	}
	public sealed class TPSXChunkInfo:BaseWADChunkInfo<TPSXChunk>
	{
		public static readonly TPSXChunkInfo Instance = new TPSXChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PSX_TEXT;
		public override WadVersion[] SupportedWadVersions => Configuration.PSX_PARSABLE_WADS;
		public override string ChunkDescription => "textures";

		public override TPSXChunk Parse(WadReader data_in)
		{
			bool hasMemoryCardIcons;
			bool compressed16bit;
			string[] titles;
			FontPSX[] fontLookup;
			if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				hasMemoryCardIcons = false;
				titles = [];
				fontLookup = [];
				compressed16bit = false;
			}
			else
			{
				var tpsx_flags = (TextureFlag)data_in.Read<int>();
				var hasLongLevelName = (tpsx_flags & TextureFlag.HasLongLevelName) != 0;
				hasMemoryCardIcons = (tpsx_flags & TextureFlag.HasMemoryCardIcons) != 0;
				var hasLevelName = (tpsx_flags & TextureFlag.HasLevelName) != 0;
				compressed16bit = (tpsx_flags & TextureFlag.Compressed16Bit) != 0;

				if(hasLevelName)
				{
					if(hasLongLevelName)
					{
						var n_titles = data_in.Read<int>();
						titles = new string[n_titles];
						for(int i = 0; i < n_titles; i++)
						{
							titles[i] = data_in.ReadString(48).Trim('\0');
						}
					}
					else
					{
						titles = [data_in.ReadString(32).Trim('\0')];
					}

					//TODO: Why are these also in DPSX?
					var spriteOffset = data_in.Read<int>();

					fontLookup = new FontPSX[256];
					for(var i = 0; i < 256; i++)
					{
						fontLookup[i] = FontPSX.Parse(data_in);
					}
				}
				else
				{
					titles = [];
					fontLookup = [];
				}
			}
			var texture_file = TextureFilePSX.parse(data_in, compressed16bit, hasMemoryCardIcons);

			data_in.AssertEndOfChunk(ChunkType);
			return new TPSXChunk(texture_file, titles, fontLookup, data_in.GetAllWadData());
		}
	}

	public sealed class TPSXChunk(TextureFilePSX texture_file, IReadOnlyList<string> titles, IReadOnlyList<FontPSX> fontLookup, byte[]? fallback_data = null):BaseWadChunk(TPSXChunkInfo.Instance, fallback_data)
	{
		public TextureFilePSX TextureFile{get;} = texture_file;
		public IReadOnlyList<string> Titles{get;} = titles;
		public IReadOnlyList<FontPSX> FontLookup{get;} = fontLookup;

		protected override void WriteData(WadWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}