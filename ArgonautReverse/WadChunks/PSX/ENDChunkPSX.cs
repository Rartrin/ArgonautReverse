using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX;

namespace ArgonautReverse.WadChunks.PSX
{
	public sealed class ENDChunkInfoPSX:BaseWADChunkInfo<ENDChunkPSX>
	{
		public static readonly ENDChunkInfoPSX Instance = new ENDChunkInfoPSX();

		public override ChunkType ChunkType => ChunkType.ID_END;
		public override string ChunkDescription => "END but sometimes includes sound effects, background music, and dialogues";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.PSX_PARSABLE_WADS;//new[]{HARRY_POTTER_1_PS1.Instance, HARRY_POTTER_2_PS1.Instance};

		public override ENDChunkPSX Parse(WadReader data_in)
		{
			if(data_in.WadFile.GetChunk(SPSXChunkInfo.Instance) is SPSXChunk spsxChunk)
			{
				//TODO: Implement sound for other games
				if(data_in.Length != 0)
				{
					if((spsxChunk.spsx_flags&SPSXFlagsPSX.HAS_LEVEL_SFX)!=0)
					{
						spsxChunk.level_sfx_groups.parse_vags(data_in);
						spsxChunk.level_sfx_mapping.parse_mapping(spsxChunk.level_sfx_groups);
					}
					data_in.AbsolutePosition = 2048 * (int)Math.Ceiling(data_in.AbsolutePosition / 2048.0);
					spsxChunk.dialogues_bgms.parse_vags(data_in);

					if(data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion)
					{
						data_in.AbsolutePosition = 2048 * (int)Math.Ceiling(data_in.AbsolutePosition / 2048.0);
					}
					data_in.AssertEndOfChunk(ChunkType);
				}
				return new ENDChunkPSX(spsxChunk);
			}
			return new ENDChunkPSX(null);
		}
	}
	public sealed class ENDChunkPSX(SPSXChunk? spsxChunk):BaseWadChunk(ENDChunkInfoPSX.Instance)
	{
		public readonly SPSXChunk? spsxChunk = spsxChunk;

		protected override void WriteData(ChunkWriter writer)
		{
			if(spsxChunk==null){return;}
			throw new NotImplementedException();
		}
		
		public override void Serialize(WadWriter data_out)
		{
			var start = base.SerializeHeader(data_out);
			if(spsxChunk!=null)
			{
				if((this.spsxChunk.spsx_flags&SPSXFlagsPSX.HAS_LEVEL_SFX)!=0)
				{
					this.spsxChunk.level_sfx_groups.serialize_vags(data_out);
				}

				if((this.spsxChunk.spsx_flags&SPSXFlagsPSX.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
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