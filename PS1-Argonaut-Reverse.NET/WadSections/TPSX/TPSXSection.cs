using System.Text;

namespace ArgonautReverse.WadSections.TPSX
{
	public sealed class TPSXSectionInfo:BaseWADSectionInfo<TPSXSection>
	{
		public static readonly TPSXSectionInfo Instance = new TPSXSectionInfo();

		public override string codename_str => "TPSX";//"XSPT";
		public override G[] supported_games => Configuration.PARSABLE_GAMES;
		public override string section_content_description => "textures";

		//@classmethod
		public override TPSXSection parse(Parser data_in, Configuration conf/*, *args, **kwargs*/)//BufferedIOBase
		{
			var fallback_data = fallback_parse_data(data_in);
			var (size, start) = base.parseInner(data_in, conf);
			bool has_legacy_textures;
			string[] titles;
			Font[] fontLookup;
			if(conf.game == G.CROC_2_DEMO_PS1_DUMMY)
			{
				has_legacy_textures = false;
				titles = Array.Empty<string>();
				fontLookup = Array.Empty<Font>();
			}
			else
			{
				var tpsx_flags = data_in.ReadInt32();
				var hasLongLevelName = (tpsx_flags & 16) != 0;//has_translated_titles
				has_legacy_textures = (tpsx_flags & 8) != 0;
				var hasLevelName = (tpsx_flags & 4) != 0;
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
				
					//Skips fonts
					data_in.seek(2048, SeekOrigin.Current);
				
				}
				else
				{
					titles = Array.Empty<string>();
					fontLookup = Array.Empty<Font>();
				}
			}
			var texture_file = TextureFile.parse(data_in, conf, has_legacy_textures:has_legacy_textures, end:start + size);

			check_size(size, start, (int)data_in.Position);
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