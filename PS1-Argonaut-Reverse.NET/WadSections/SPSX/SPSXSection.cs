namespace ArgonautReverse.WadSections.SPSX
{
	public sealed class SPSXSectionInfo:BaseWADSectionInfo<SPSXSection>
	{
		public static readonly SPSXSectionInfo Instance = new SPSXSectionInfo();

		public override string codename_str => "SPSX";
		public override string section_content_description => "sound effects, background music & dialogues";
		public override Game[] supported_games{get;} = new Game[]{/*CROC_2_DEMO_PS1_DUMMY.Instance,*/ HARRY_POTTER_1_PS1.Instance, HARRY_POTTER_2_PS1.Instance};

		public override SPSXSection Parse(Parser data_in, Configuration conf)
		{
			bool isHarryPotterGame = conf.game==HARRY_POTTER_1_PS1.Instance || conf.game==HARRY_POTTER_2_PS1.Instance;
			var (size, start) = base.parseInner(data_in, conf);

			var spsx_flags = (SPSXFlags)data_in.ReadUInt32();
			Utils.Assert(!isHarryPotterGame || (spsx_flags & SPSXFlags.AMBIENTSEP) == 0);// Bit 1 is always unset
			//Bit 0 and 4 are identical
			Utils.Assert(((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0) == ((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS_)!=0));

			//logging.debug(f"Flags: {str(spsx_flags)}")

			var n_sfx = data_in.ReadInt32();
			//logging.debug(f"sound effects count: {n_sfx}")

			CommonSFXContainer common_sfx = null;
			//If it is either HP game, we need to check the flag, otherwise it is required
			if(!isHarryPotterGame || (spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				var sounds = new EffectSound[n_sfx];
				for(int i = 0; i<n_sfx; i++)
				{
					sounds[i] = EffectSound.parse(data_in, conf);
				}
				common_sfx = new CommonSFXContainer(sounds);
			}

			AmbientContainer ambient_tracks = null;
			if((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			{
				var ambient_tracks_headers_size = data_in.ReadInt32();
				Utils.Assert((ambient_tracks_headers_size % 20) == 0);
				var n_ambient_tracks = ambient_tracks_headers_size / 20;
				var sounds = new AmbientSound[n_ambient_tracks];
				for(int i = 0; i<n_ambient_tracks; i++)
				{
					sounds[i] = AmbientSound.parse(data_in, conf);
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
				var n_level_sfx_groups = data_in.ReadInt32();
				Utils.Assert(n_level_sfx_groups < 16);
				idk1 = data_in.ReadInt32();
				idk2 = data_in.ReadInt32();
				var n_unique_level_sfx = data_in.ReadInt32();
				var groups = new LevelSFXGroupContainer[n_level_sfx_groups];
				for(int i = 0; i<n_level_sfx_groups; i++)
				{
					groups[i] = LevelSFXGroupContainer.parse(data_in, conf);
				}
				level_sfx_groups = new LevelSFXContainer(groups);
				level_sfx_groups.parse_groups(data_in, conf);
				Utils.Assert(n_unique_level_sfx <= level_sfx_groups.n_sounds);
				level_sfx_mapping = LevelSFXMapping.parse(data_in, conf, n_unique_level_sfx:n_unique_level_sfx);
			}

			//Dialogues & BGMs
			DialoguesBGMsContainer dialogues_bgms = null;
			var n_dialogues_bgms = data_in.ReadInt32();


			if((spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				// Gap between level sound effects and dialogues/BGMs
				var end_gap = data_in.ReadUInt32();
				Utils.Assert((end_gap != 0) == ((spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0));
				//logging.debug(f"END (DNE) gap: {end_gap}")

				var sounds = new DialogueBGMSound[n_dialogues_bgms];
				for(int i=0; i<n_dialogues_bgms; i++)
				{
					sounds[i] = DialogueBGMSound.parse(data_in, conf);
				}
				dialogues_bgms = new DialoguesBGMsContainer(sounds);

				// Common sound effects audio data
				var common_sfx_total_size = data_in.ReadInt32();
				Utils.Assert(common_sfx_total_size == common_sfx.size);
				common_sfx.parse_vags(data_in, conf);
			}
			// Ambient tracks audio data
			if((spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			{
				var ambient_tracks_total_size = data_in.ReadInt32();
				Utils.Assert(ambient_tracks_total_size == ambient_tracks.size);
				ambient_tracks.parse_vags(data_in, conf);
			}
			this.check_size(size, start, data_in.Position);
			return new SPSXSection(spsx_flags, common_sfx, ambient_tracks, level_sfx_groups, level_sfx_mapping, idk1, idk2, dialogues_bgms);
		}
	}
	public sealed class SPSXSection:BaseWADSection
	{
		public readonly SPSXFlags spsx_flags;
		public readonly CommonSFXContainer common_sfx;
		public readonly AmbientContainer ambient_tracks;
		public readonly LevelSFXContainer level_sfx_groups;
		public readonly LevelSFXMapping level_sfx_mapping;
		public readonly int? idk1;
		public readonly int? idk2;
		public readonly DialoguesBGMsContainer dialogues_bgms;

		public SPSXSection(SPSXFlags spsx_flags, CommonSFXContainer common_sfx, AmbientContainer ambient_tracks, LevelSFXContainer level_sfx_groups, LevelSFXMapping level_sfx_mapping, int? idk1, int? idk2, DialoguesBGMsContainer dialogues_bgms):base(SPSXSectionInfo.Instance)
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

		public int end_gap => this.level_sfx_groups.Groups.Sum(group => Utils.round_up_padding(group.size));

		public override void serialize(Serializer data_out, Configuration conf)
		{
			var start = base.serializeInner(data_out, conf);

			data_out.WriteUInt32((uint)this.spsx_flags);
			data_out.WriteInt32(this.n_common_sfx);
			if((this.spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				this.common_sfx.serialize(data_out, conf);
			}
			if((this.spsx_flags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			{
				data_out.WriteInt32((20 * this.n_ambient_tracks));
				this.ambient_tracks.serialize(data_out, conf);
			}
			if((this.spsx_flags&SPSXFlags.HAS_LEVEL_SFX)!=0)
			{
				data_out.WriteInt32(this.n_level_sfx_groups);
				data_out.WriteInt32(this.idk1.Value);
				data_out.WriteInt32(this.idk2.Value);
				data_out.WriteInt32(this.level_sfx_mapping.n_unique_level_sfx);

				this.level_sfx_groups.serialize(data_out, conf);
				foreach(var group in this.level_sfx_groups.Groups)
				{
					group.serialize_children(data_out, conf);
				}
				this.level_sfx_mapping.serialize(data_out, conf, level_sfx_groups:this.level_sfx_groups);
			}
			data_out.WriteInt32(this.n_dialogues_bgms);
			if((this.spsx_flags&SPSXFlags.HAS_COMMON_SFX_AND_DIALOGUES_BGMS)!=0)
			{
				data_out.WriteInt32(this.end_gap);
				this.dialogues_bgms.serialize(data_out, conf);
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
			SerializeSectionSize(data_out, start);
		}
	}
}