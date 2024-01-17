using System.Text;
using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.TPSX
{
	public enum TextureFlag:int
	{
		CompressedTPage		= (1 << 0),
		Compressed16Bit		= (1 << 1),
		HasLevelName		= (1 << 2),
		HasMemoryCardIcons	= (1 << 3),
		HasLongLevelName	= (1 << 4),//Includes translated names
	}
	public sealed class TPSXChunkInfo:BaseWADChunkInfo
	{
		public static readonly TPSXChunkInfo Instance = new TPSXChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_TEXTPSX;
		public override WadVersion[] SupportedWadVersions => Configuration.PARSABLE_WADS;
		public override string ChunkDescription => "textures";

		public override TPSXChunk Parse(WadReader data_in)
		{
			var fallback_data = GetChunkData(data_in);
			base.ParseHeader(data_in, out var size, out var start);

			bool hasMemoryCardIcons;
			bool compressed16bit;
			string[] titles;
			Font[] fontLookup;
			if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				hasMemoryCardIcons = false;
				titles = Array.Empty<string>();
				fontLookup = Array.Empty<Font>();
				compressed16bit = false;
			}
			else
			{
				var tpsx_flags = (TextureFlag)data_in.Read<int>();
				var hasLongLevelName = (tpsx_flags & TextureFlag.HasLongLevelName) != 0;
				hasMemoryCardIcons = (tpsx_flags & TextureFlag.HasMemoryCardIcons) != 0;
				var hasLevelName = (tpsx_flags & TextureFlag.HasLevelName) != 0;
				compressed16bit = (tpsx_flags & TextureFlag.Compressed16Bit) != 0;

				//TODO: Ensure these always match
				bool rle = data_in.ReadVersion == CROC_2_PS1.WadVersion || data_in.ReadVersion == CROC_2_DEMO_PS1.WadVersion || data_in.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion;
				if(compressed16bit != rle)
				{
					throw new Exception();
				}

				if(hasLevelName)
				{
					if(hasLongLevelName)
					{
						var n_titles = data_in.Read<int>();
						titles = new string[n_titles];
						for(int i=0; i<n_titles; i++)
						{
							titles[i] = data_in.ReadString(48).Trim('\0');
						}
					}
					else
					{
						titles = new[]{data_in.ReadString(32).Trim('\0')};
					}

					//TODO: Why are these also in DPSX?
					var spriteOffset = data_in.Read<int>();

					fontLookup = new Font[256];
					for(var i=0; i<256; i++)
					{
						fontLookup[i] = Font.Parse(data_in);
					}
				}
				else
				{
					titles = Array.Empty<string>();
					fontLookup = Array.Empty<Font>();
				}
			}
			var texture_file = TextureFile.parse(data_in, compressed16bit:compressed16bit, hasMemoryCardIcons:hasMemoryCardIcons, end:start + size);

			CheckSize(size, start, data_in.Position);
			return new TPSXChunk(texture_file, titles, fontLookup, fallback_data);
		}
	}

	public sealed class TPSXChunk:BaseWadChunk
	{
		public TextureFile TextureFile{get;}
		public IReadOnlyList<string> Titles{get;}
		public IReadOnlyList<Font> FontLookup{get;}

		public TPSXChunk(TextureFile texture_file, IReadOnlyList<string> titles, IReadOnlyList<Font> fontLookup, byte[] fallback_data = null):base(TPSXChunkInfo.Instance, fallback_data)
		{
			TextureFile = texture_file;
			Titles = titles;
			FontLookup = fontLookup;
		}
	}
}