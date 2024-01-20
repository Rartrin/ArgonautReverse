using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.SPSX
{
	public sealed class SPSXChunkInfo:BaseWADChunkInfo
	{
		public static readonly SPSXChunkInfo Instance = new SPSXChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_SAMPLEPSX;
		public override string ChunkDescription => "sound effects, background music & dialogues";
		public override WadVersion[] SupportedWadVersions{get;} = new[]
		{
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion
		}.SelectMany(dat => dat.WadVersions).ToArray();

		public override SPSXChunk Parse(WadReader data_in)
		{
			bool isHarryPotterGame = data_in.ReadVersion==HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion==HARRY_POTTER_2_PS1.WadVersion;

			var spsx_flags = (SPSXFlags)data_in.Read<uint>();

			//TODO: Implement sound for other games
			if(!isHarryPotterGame)
			{
				return new SPSXChunk(spsx_flags, null, null, null, null, null, null, null);
			}


			Utils.Assert(!isHarryPotterGame || (spsx_flags & SPSXFlags.AMBIENTSEP) == 0);// Bit 1 is always unset
			//Bit 0 and 4 are identical
			Utils.Assert(((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0) == ((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS_)!=0));

			//logging.debug(f"Flags: {str(spsx_flags)}")

			var n_sfx = data_in.Read<int>();
			//logging.debug(f"sound effects count: {n_sfx}")

			CommonSFXContainer common_sfx = null;
			//If it is either HP game, we need to check the flag, otherwise it is required
			if(!isHarryPotterGame || (spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				var sounds = new EffectSound[n_sfx];
				for(int i = 0; i<n_sfx; i++)
				{
					sounds[i] = EffectSound.parse(data_in);
				}
				common_sfx = new CommonSFXContainer(sounds);
			}

			AmbientContainer ambient_tracks = null;
			if((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			{
				var ambient_tracks_headers_size = data_in.Read<int>();
				Utils.Assert((ambient_tracks_headers_size % 20) == 0);
				var n_ambient_tracks = ambient_tracks_headers_size / 20;
				var sounds = new AmbientSound[n_ambient_tracks];
				for(int i = 0; i<n_ambient_tracks; i++)
				{
					sounds[i] = AmbientSound.parse(data_in);
				}
				ambient_tracks = new AmbientContainer(sounds);
			}
			// Level sound effects groups
			int? idk1 = null;
			int? idk2 = null;
			LevelSFXContainer level_sfx_groups = null;
			LevelSFXMapping level_sfx_mapping = null;
			if((spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)//Only Harry Potter?
			{
				var n_level_sfx_groups = data_in.Read<int>();
				Utils.Assert(n_level_sfx_groups < 16);
				idk1 = data_in.Read<int>();
				idk2 = data_in.Read<int>();
				var n_unique_level_sfx = data_in.Read<int>();
				var groups = new LevelSFXGroupContainer[n_level_sfx_groups];
				for(int i = 0; i<n_level_sfx_groups; i++)
				{
					groups[i] = LevelSFXGroupContainer.parse(data_in);
				}
				level_sfx_groups = new LevelSFXContainer(groups);
				level_sfx_groups.parse_groups(data_in);
				Utils.Assert(n_unique_level_sfx <= level_sfx_groups.n_sounds);
				level_sfx_mapping = LevelSFXMapping.parse(data_in, n_unique_level_sfx:n_unique_level_sfx);
			}

			//Dialogues & BGMs
			DialoguesBGMsContainer dialogues_bgms = null;
			var n_dialogues_bgms = data_in.Read<int>();


			if((spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				// Gap between level sound effects and dialogues/BGMs
				var end_gap = data_in.Read<uint>();
				Utils.Assert((end_gap != 0) == ((spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0));
				//logging.debug(f"END (DNE) gap: {end_gap}")

				var sounds = new DialogueBGMSound[n_dialogues_bgms];
				for(int i=0; i<n_dialogues_bgms; i++)
				{
					sounds[i] = DialogueBGMSound.parse(data_in);
				}
				dialogues_bgms = new DialoguesBGMsContainer(sounds);

				// Common sound effects audio data
				var common_sfx_total_size = data_in.Read<int>();
				Utils.Assert(common_sfx_total_size == common_sfx.size);
				common_sfx.parse_vags(data_in);
			}
			// Ambient tracks audio data
			if((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			{
				var ambient_tracks_total_size = data_in.Read<int>();
				Utils.Assert(ambient_tracks_total_size == ambient_tracks.size);
				ambient_tracks.parse_vags(data_in);
			}
			CheckSize(data_in);
			return new SPSXChunk(spsx_flags, common_sfx, ambient_tracks, level_sfx_groups, level_sfx_mapping, idk1, idk2, dialogues_bgms);
		}
	}
	public sealed class SPSXChunk:BaseWadChunk
	{
		public readonly SPSXFlags spsx_flags;
		public readonly CommonSFXContainer common_sfx;
		public readonly AmbientContainer ambient_tracks;
		public readonly LevelSFXContainer level_sfx_groups;
		public readonly LevelSFXMapping level_sfx_mapping;
		public readonly int? idk1;
		public readonly int? idk2;
		public readonly DialoguesBGMsContainer dialogues_bgms;

		public SPSXChunk(SPSXFlags spsx_flags, CommonSFXContainer common_sfx, AmbientContainer ambient_tracks, LevelSFXContainer level_sfx_groups, LevelSFXMapping level_sfx_mapping, int? idk1, int? idk2, DialoguesBGMsContainer dialogues_bgms):base(SPSXChunkInfo.Instance)
		{
			this.spsx_flags = spsx_flags;
			this.common_sfx = common_sfx ?? new CommonSFXContainer();
			this.ambient_tracks = ambient_tracks ?? new AmbientContainer();
			this.level_sfx_groups = level_sfx_groups ?? new LevelSFXContainer();
			this.level_sfx_mapping = level_sfx_mapping;
			this.idk1 = idk1;
			this.idk2 = idk2;
			this.dialogues_bgms = dialogues_bgms ?? new DialoguesBGMsContainer();
		}

		public int n_sounds =>
		(
			this.n_common_sfx
			+ this.n_ambient_tracks
			+ this.n_level_sfx
			+ this.n_dialogues_bgms
		);

		public int n_common_sfx => this.common_sfx.Sounds.Count;

		public int common_sfx_size => this.common_sfx.size;

		public int n_ambient_tracks => this.ambient_tracks.Sounds.Count;

		public int ambient_tracks_size => this.ambient_tracks.size;

		public int n_level_sfx_groups => this.level_sfx_groups.Groups.Count;

		public int n_level_sfx => this.level_sfx_groups.n_sounds;

		public int n_dialogues_bgms => this.dialogues_bgms.Sounds.Count;

		public int end_gap => this.level_sfx_groups.Groups.Sum(group => Utils.RoundUpPadding(group.size));

		public override void Serialize(Serializer data_out)
		{
			var start = base.SerializeHeader(data_out);

			data_out.WriteUInt32((uint)this.spsx_flags);
			data_out.WriteInt32(this.n_common_sfx);
			if((this.spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				this.common_sfx.serialize(data_out);
			}
			if((this.spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			{
				data_out.WriteInt32((20 * this.n_ambient_tracks));
				this.ambient_tracks.serialize(data_out);
			}
			if((this.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
			{
				data_out.WriteInt32(this.n_level_sfx_groups);
				data_out.WriteInt32(this.idk1.Value);
				data_out.WriteInt32(this.idk2.Value);
				data_out.WriteInt32(this.level_sfx_mapping.n_unique_level_sfx);

				this.level_sfx_groups.serialize(data_out);
				foreach(var group in this.level_sfx_groups.Groups)
				{
					group.serialize_children(data_out);
				}
				this.level_sfx_mapping.serialize(data_out, level_sfx_groups:this.level_sfx_groups);
			}
			data_out.WriteInt32(this.n_dialogues_bgms);
			if((this.spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				data_out.WriteInt32(this.end_gap);
				this.dialogues_bgms.serialize(data_out);
				data_out.WriteInt32(this.common_sfx_size);
				foreach(var vag in this.common_sfx.vags)
				{
					data_out.WriteBytes(vag.data);
				}
			}
			if((this.spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			{
				data_out.WriteInt32(this.ambient_tracks_size);
				foreach(var vag in this.ambient_tracks.vags)
				{
					data_out.WriteBytes(vag.data);
				}
			}
			SerializeChunkSize(data_out, start);
		}
	}
}