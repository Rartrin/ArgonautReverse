using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadChunks.SPSX;

namespace ArgonautReverse.WadChunks
{
	public sealed class ENDChunkInfo:BaseWADChunkInfo
	{
		public static readonly ENDChunkInfo Instance = new ENDChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_END;
		public override string ChunkDescription => "sound effects, background music & dialogues";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.PARSABLE_WADS;//new[]{HARRY_POTTER_1_PS1.Instance, HARRY_POTTER_2_PS1.Instance};

		public override ENDChunk Parse(WadReader data_in)
		{
			var spsxChunk = data_in.WadFile.SPSX;
			if(spsxChunk != null)
			{
				//TODO: Implement sound for other games
				base.ParseHeader(data_in, out var size, out var start);
				if(size != 0)
				{
					if((spsxChunk.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
					{
						spsxChunk.level_sfx_groups.parse_vags(data_in);
						spsxChunk.level_sfx_mapping.parse_mapping(spsxChunk.level_sfx_groups);
					}
					data_in.Position = 2048 * (int)Math.Ceiling(data_in.Position / 2048.0);
					spsxChunk.dialogues_bgms.parse_vags(data_in);

					if(data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion)
					{
						data_in.Position = 2048 * (int)Math.Ceiling(data_in.Position / 2048.0);
					}
					CheckSize(size, start, data_in.Position);
				}
			}
			return new ENDChunk(spsxChunk);
		}
	}
	public sealed class ENDChunk:BaseWadChunk
	{
		public readonly SPSXChunk spsxChunk;

		public ENDChunk(SPSXChunk spsxChunk):base(ENDChunkInfo.Instance)
		{
			this.spsxChunk = spsxChunk;
		}
		
		public override void Serialize(Serializer data_out)
		{
			var start = base.SerializeHeader(data_out);
			if(spsxChunk!=null)
			{
				if((this.spsxChunk.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
				{
					this.spsxChunk.level_sfx_groups.serialize_vags(data_out);
				}

				if((this.spsxChunk.spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
				{
					Utils.PadOut2048Bytes(data_out);
					this.spsxChunk.dialogues_bgms.serialize_vags(data_out);
				}

				if(data_out.WriteVersion == HARRY_POTTER_2_PS1.WadVersion)
				{
					Utils.PadOut2048Bytes(data_out);
				}
			}

			SerializeChunkSize(data_out, start);
		}
	}
}