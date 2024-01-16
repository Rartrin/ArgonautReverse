using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadSections.SPSX;

namespace ArgonautReverse.WadSections
{
	public sealed class ENDSectionInfo:BaseWADSectionInfo<ENDSection>
	{
		public static readonly ENDSectionInfo Instance = new ENDSectionInfo();

		public override ChunkType ChunkType => ChunkType.ID_END;
		public override string section_content_description => "sound effects, background music & dialogues";
		public override WadVersion[] supported_games{get;} = Configuration.PARSABLE_WADS;//new[]{HARRY_POTTER_1_PS1.Instance, HARRY_POTTER_2_PS1.Instance};

		public override ENDSection Parse(WadReader data_in)
		{
			var spsx_section = data_in.WadFile.spsx;
			if(spsx_section != null)
			{
				//TODO: Implement sound for other games
				base.parseInner(data_in, out var size, out var start);
				if(size != 0)
				{
					if((spsx_section.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
					{
						spsx_section.level_sfx_groups.parse_vags(data_in);
						spsx_section.level_sfx_mapping.parse_mapping(spsx_section.level_sfx_groups);
					}
					data_in.Seek(2048 * (int)Math.Ceiling(data_in.Position / 2048.0));
					spsx_section.dialogues_bgms.parse_vags(data_in);

					if(data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion)
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
		
		public override void serialize(Serializer data_out)
		{
			var start = base.serializeInner(data_out);
			if(spsx_section!=null)
			{
				if((this.spsx_section.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
				{
					this.spsx_section.level_sfx_groups.serialize_vags(data_out);
				}

				if((this.spsx_section.spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
				{
					Utils.PadOut2048Bytes(data_out);
					this.spsx_section.dialogues_bgms.serialize_vags(data_out);
				}

				if(data_out.WriteVersion == HARRY_POTTER_2_PS1.WadVersion)
				{
					Utils.PadOut2048Bytes(data_out);
				}
			}

			SerializeSectionSize(data_out, start);
		}
	}
}