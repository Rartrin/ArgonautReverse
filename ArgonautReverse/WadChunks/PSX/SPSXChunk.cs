using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX;

namespace ArgonautReverse.WadChunks.PSX
{
    public sealed class SPSXChunkInfo:BaseWADChunkInfo<SPSXChunk>
	{
		public static readonly SPSXChunkInfo Instance = new SPSXChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PSX_SAMPLE;
		public override string ChunkDescription => "sound effects, background music & dialogues";
		public override WadVersion[] SupportedWadVersions { get; } = new[]
		{
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion
		}.SelectMany(dat => dat.WadVersions).ToArray();

		public override SPSXChunk Parse(WadReader data_in)
		{
			bool isHarryPotterGame = data_in.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion;

			var spsx_flags = (SPSXFlagsPSX)data_in.Read<uint>();

			//TODO: Implement sound for other games
			if(!isHarryPotterGame)
			{
				return new SPSXChunk(spsx_flags, null, null, null, null, null, null, null);
			}


			Utils.Assert(!isHarryPotterGame || (spsx_flags & SPSXFlagsPSX.AMBIENTSEP) == 0);// Bit 1 is always unset
																							//Bit 0 and 4 are identical
			Utils.Assert((spsx_flags & SPSXFlagsPSX.HAS_AMBIENT_TRACKS) != 0 == ((spsx_flags & SPSXFlagsPSX.HAS_AMBIENT_TRACKS_) != 0));

			//logging.debug(f"Flags: {str(spsx_flags)}")

			var n_sfx = data_in.Read<int>();
			//logging.debug(f"sound effects count: {n_sfx}")

			CommonSFXContainerPSX common_sfx = null;
			//If it is either HP game, we need to check the flag, otherwise it is required
			if(!isHarryPotterGame || (spsx_flags & SPSXFlagsPSX.HAS_COMMON_SFX_AND_DIALOGUES_BGMS) != 0)
			{
				var sounds = new EffectSoundPSX[n_sfx];
				for(int i = 0; i < n_sfx; i++)
				{
					sounds[i] = EffectSoundPSX.parse(data_in);
				}
				common_sfx = new CommonSFXContainerPSX(sounds);
			}

			AmbientContainerPSX ambient_tracks = null;
			if((spsx_flags & SPSXFlagsPSX.HAS_AMBIENT_TRACKS) != 0)
			{
				var ambient_tracks_headers_size = data_in.Read<int>();
				Utils.Assert(ambient_tracks_headers_size % 20 == 0);
				var n_ambient_tracks = ambient_tracks_headers_size / 20;
				var sounds = new AmbientSoundPSX[n_ambient_tracks];
				for(int i = 0; i < n_ambient_tracks; i++)
				{
					sounds[i] = AmbientSoundPSX.parse(data_in);
				}
				ambient_tracks = new AmbientContainerPSX(sounds);
			}
			// Level sound effects groups
			int? idk1 = null;
			int? idk2 = null;
			LevelSFXContainerPSX level_sfx_groups = null;
			LevelSFXMappingPSX level_sfx_mapping = null;
			if((spsx_flags & SPSXFlagsPSX.HAS_LEVEL_SFX) != 0)//Only Harry Potter?
			{
				var n_level_sfx_groups = data_in.Read<int>();
				Utils.Assert(n_level_sfx_groups < 16);
				idk1 = data_in.Read<int>();
				idk2 = data_in.Read<int>();
				var n_unique_level_sfx = data_in.Read<int>();
				var groups = new LevelSFXGroupContainerPSX[n_level_sfx_groups];
				for(int i = 0; i < n_level_sfx_groups; i++)
				{
					groups[i] = LevelSFXGroupContainerPSX.parse(data_in);
				}
				level_sfx_groups = new LevelSFXContainerPSX(groups);
				level_sfx_groups.parse_groups(data_in);
				Utils.Assert(n_unique_level_sfx <= level_sfx_groups.n_sounds);
				level_sfx_mapping = LevelSFXMappingPSX.parse(data_in, n_unique_level_sfx: n_unique_level_sfx);
			}

			//Dialogues & BGMs
			DialoguesBGMsContainerPSX dialogues_bgms = null;
			var n_dialogues_bgms = data_in.Read<int>();


			if((spsx_flags & SPSXFlagsPSX.HAS_COMMON_SFX_AND_DIALOGUES_BGMS) != 0)
			{
				// Gap between level sound effects and dialogues/BGMs
				var end_gap = data_in.Read<uint>();
				Utils.Assert(end_gap != 0 == ((spsx_flags & SPSXFlagsPSX.HAS_LEVEL_SFX) != 0));
				//logging.debug(f"END (DNE) gap: {end_gap}")

				var sounds = new DialogueBGMSoundPSX[n_dialogues_bgms];
				for(int i = 0; i < n_dialogues_bgms; i++)
				{
					sounds[i] = DialogueBGMSoundPSX.parse(data_in);
				}
				dialogues_bgms = new DialoguesBGMsContainerPSX(sounds);

				// Common sound effects audio data
				var common_sfx_total_size = data_in.Read<int>();
				Utils.Assert(common_sfx_total_size == common_sfx.size);
				common_sfx.parse_vags(data_in);
			}
			// Ambient tracks audio data
			if((spsx_flags & SPSXFlagsPSX.HAS_AMBIENT_TRACKS) != 0)
			{
				var ambient_tracks_total_size = data_in.Read<int>();
				Utils.Assert(ambient_tracks_total_size == ambient_tracks.size);
				ambient_tracks.parse_vags(data_in);
			}
			data_in.AssertEndOfChunk(ChunkType);
			return new SPSXChunk(spsx_flags, common_sfx, ambient_tracks, level_sfx_groups, level_sfx_mapping, idk1, idk2, dialogues_bgms);
		}
	}
	public sealed class SPSXChunk:BaseWadChunk
	{
		public readonly SPSXFlagsPSX spsx_flags;
		public readonly CommonSFXContainerPSX common_sfx;
		public readonly AmbientContainerPSX ambient_tracks;
		public readonly LevelSFXContainerPSX level_sfx_groups;
		public readonly LevelSFXMappingPSX level_sfx_mapping;
		public readonly int? idk1;
		public readonly int? idk2;
		public readonly DialoguesBGMsContainerPSX dialogues_bgms;

		public SPSXChunk(SPSXFlagsPSX spsx_flags, CommonSFXContainerPSX common_sfx, AmbientContainerPSX ambient_tracks, LevelSFXContainerPSX level_sfx_groups, LevelSFXMappingPSX level_sfx_mapping, int? idk1, int? idk2, DialoguesBGMsContainerPSX dialogues_bgms) : base(SPSXChunkInfo.Instance)
		{
			this.spsx_flags = spsx_flags;
			this.common_sfx = common_sfx ?? new CommonSFXContainerPSX();
			this.ambient_tracks = ambient_tracks ?? new AmbientContainerPSX();
			this.level_sfx_groups = level_sfx_groups ?? new LevelSFXContainerPSX();
			this.level_sfx_mapping = level_sfx_mapping;
			this.idk1 = idk1;
			this.idk2 = idk2;
			this.dialogues_bgms = dialogues_bgms ?? new DialoguesBGMsContainerPSX();
		}

		public int n_sounds =>

			n_common_sfx
			+ n_ambient_tracks
			+ n_level_sfx
			+ n_dialogues_bgms
		;

		public int n_common_sfx => common_sfx.Sounds.Count;

		public int common_sfx_size => common_sfx.size;

		public int n_ambient_tracks => ambient_tracks.Sounds.Count;

		public int ambient_tracks_size => ambient_tracks.size;

		public int n_level_sfx_groups => level_sfx_groups.Groups.Count;

		public int n_level_sfx => level_sfx_groups.n_sounds;

		public int n_dialogues_bgms => dialogues_bgms.Sounds.Count;

		public int end_gap => level_sfx_groups.Groups.Sum(group => Utils.RoundUpPadding(group.size));

		public override void Serialize(WadWriter data_out)
		{
			var start = SerializeHeader(data_out);

			data_out.WriteUInt32((uint)spsx_flags);
			data_out.WriteInt32(n_common_sfx);
			if((spsx_flags & SPSXFlagsPSX.HAS_COMMON_SFX_AND_DIALOGUES_BGMS) != 0)
			{
				common_sfx.serialize(data_out);
			}
			if((spsx_flags & SPSXFlagsPSX.HAS_AMBIENT_TRACKS) != 0)
			{
				data_out.WriteInt32(20 * n_ambient_tracks);
				ambient_tracks.serialize(data_out);
			}
			if((spsx_flags & SPSXFlagsPSX.HAS_LEVEL_SFX) != 0)
			{
				data_out.WriteInt32(n_level_sfx_groups);
				data_out.WriteInt32(idk1.Value);
				data_out.WriteInt32(idk2.Value);
				data_out.WriteInt32(level_sfx_mapping.n_unique_level_sfx);

				level_sfx_groups.serialize(data_out);
				foreach(var group in level_sfx_groups.Groups)
				{
					group.serialize_children(data_out);
				}
				level_sfx_mapping.serialize(data_out, level_sfx_groups: level_sfx_groups);
			}
			data_out.WriteInt32(n_dialogues_bgms);
			if((spsx_flags & SPSXFlagsPSX.HAS_COMMON_SFX_AND_DIALOGUES_BGMS) != 0)
			{
				data_out.WriteInt32(end_gap);
				dialogues_bgms.serialize(data_out);
				data_out.WriteInt32(common_sfx_size);
				foreach(var vag in common_sfx.vags)
				{
					data_out.WriteBytes(vag.data);
				}
			}
			if((spsx_flags & SPSXFlagsPSX.HAS_AMBIENT_TRACKS) != 0)
			{
				data_out.WriteInt32(ambient_tracks_size);
				foreach(var vag in ambient_tracks.vags)
				{
					data_out.WriteBytes(vag.data);
				}
			}
			SerializeChunkSize(data_out, start);
		}
	}
}