using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadSections.SPSX;

namespace ArgonautReverse.WadSections
{
	public sealed class ENDSectionInfo:BaseWADSectionInfo<ENDSection>
	{
		public static readonly ENDSectionInfo Instance = new ENDSectionInfo();

		public override string codename_str => "END ";
		public override string section_content_description => "sound effects, background music & dialogues";
		public override VersionInfo[] supported_games{get;} = Configuration.PARSABLE_GAMES;//new[]{HARRY_POTTER_1_PS1.Instance, HARRY_POTTER_2_PS1.Instance};

		public override ENDSection Parse(WadReader data_in)
		{
			throw new Exception("Use other Parse function");
		}
		public ENDSection Parse(WadReader data_in, SPSXSection spsx_section)
		{
			if(spsx_section != null)
			{
				var (size, start) = base.parseInner(data_in);
				if(size != 0)
				{
					if((spsx_section.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
					{
						spsx_section.level_sfx_groups.parse_vags(data_in);
						spsx_section.level_sfx_mapping.parse_mapping(spsx_section.level_sfx_groups);
					}
					data_in.Seek(2048 * (int)Math.Ceiling(data_in.Position / 2048.0));
					spsx_section.dialogues_bgms.parse_vags(data_in);

					if(data_in.Version == HARRY_POTTER_2_PS1.Instance)
					{
						data_in.Seek(2048 * (int)Math.Ceiling(data_in.Position / 2048.0));
					}
					check_size(size, start, data_in.Position);
				}
			}
			return new ENDSection(spsx_section);
		}
	}
	public sealed class ENDSection:BaseWADSection
	{
		public readonly SPSXSection spsx_section;

		public ENDSection(SPSXSection spsx_section):base(ENDSectionInfo.Instance)
		{
			this.spsx_section = spsx_section;
		}
		
		public override void serialize(Serializer data_out, Configuration conf)
		{
			var start = base.serializeInner(data_out, conf);
			if(spsx_section!=null)
			{
				if((this.spsx_section.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
				{
					this.spsx_section.level_sfx_groups.serialize_vags(data_out, conf);
				}

				if((this.spsx_section.spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
				{
					Utils.pad_out_2048_bytes(data_out);
					this.spsx_section.dialogues_bgms.serialize_vags(data_out, conf);
				}

				if(conf.InputVersion == HARRY_POTTER_2_PS1.Instance)
				{
					Utils.pad_out_2048_bytes(data_out);
				}
			}

			SerializeSectionSize(data_out, start);
		}
	}
}