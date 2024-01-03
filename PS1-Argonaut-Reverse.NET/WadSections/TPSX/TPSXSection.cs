using System.Text;

namespace ArgonautReverse.WadSections.TPSX
{
	public enum TextureFlag:int
	{
		CompressedTPage		= (1 << 0),
		Compressed16Bit		= (1 << 1),
		HasLevelName		= (1 << 2),
		HasMemoryCardIcons	= (1 << 3),
		HasLongLevelName	= (1 << 4),//Includes translated names
	}
	public sealed class TPSXSectionInfo:BaseWADSectionInfo<TPSXSection>
	{
		public static readonly TPSXSectionInfo Instance = new TPSXSectionInfo();

		public override string codename_str => "TPSX";//"XSPT";
		public override G[] supported_games => Configuration.PARSABLE_GAMES;
		public override string section_content_description => "textures";

		//@classmethod
		public override TPSXSection parse(Parser data_in, Configuration conf)
		{
			var fallback_data = fallback_parse_data(data_in);
			var (size, start) = base.parseInner(data_in, conf);
			bool hasMemoryCardIcons;
			bool compressed16bit;
			string[] titles;
			Font[] fontLookup;
			if(conf.game == G.CROC_2_DEMO_PS1_DUMMY)
			{
				hasMemoryCardIcons = false;
				titles = Array.Empty<string>();
				fontLookup = Array.Empty<Font>();
				compressed16bit = false;
			}
			else
			{
				var tpsx_flags = (TextureFlag)data_in.ReadInt32();
				var hasLongLevelName = (tpsx_flags & TextureFlag.HasLongLevelName) != 0;//has_translated_titles
				hasMemoryCardIcons = (tpsx_flags & TextureFlag.HasMemoryCardIcons) != 0;
				var hasLevelName = (tpsx_flags & TextureFlag.HasLevelName) != 0;
				compressed16bit = (tpsx_flags & TextureFlag.Compressed16Bit) != 0;

				//TODO: Ensure these always match
				bool rle = conf.game == G.CROC_2_PS1 || conf.game == G.CROC_2_DEMO_PS1 || conf.game == G.HARRY_POTTER_1_PS1 || conf.game == G.HARRY_POTTER_2_PS1;
				if(compressed16bit != rle)
				{
					throw new Exception();
				}

				if(hasLevelName)
				{
					if(hasLongLevelName)
					{
						var n_titles = data_in.ReadInt32();
						titles = new string[n_titles];
						for(int i=0; i<n_titles; i++)
						{
							titles[i] = Encoding.Latin1.GetString(data_in.ReadBytes(48)).Trim('\0');
						}
					}
					else
					{
						titles = new[]{Encoding.Latin1.GetString(data_in.ReadBytes(32)).Trim('\0')};
					}

					var spriteOffset = data_in.ReadInt32();

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
			var texture_file = TextureFile.parse(data_in, conf, compressed16bit:compressed16bit, hasMemoryCardIcons:hasMemoryCardIcons, end:start + size);

			check_size(size, start, data_in.Position);
			return new TPSXSection(texture_file, titles, fontLookup, fallback_data);
		}
	}

	public sealed class TPSXSection:BaseWADSection
	{
		public readonly TextureFile texture_file;
		public readonly IReadOnlyList<string> titles;
		public readonly IReadOnlyList<Font> FontLookup;

		public TPSXSection(TextureFile texture_file, IReadOnlyList<string> titles, IReadOnlyList<Font> fontLookup, byte[] fallback_data = null):base(TPSXSectionInfo.Instance, fallback_data)
		{
			this.texture_file = texture_file;
			this.titles = titles;
			this.FontLookup = fontLookup;
		}
	}
}