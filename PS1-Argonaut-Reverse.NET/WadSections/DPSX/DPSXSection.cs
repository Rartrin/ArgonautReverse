namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class DPSXSectionInfo:BaseWADSectionInfo<DPSXSection>
	{
		public static readonly DPSXSectionInfo Instance = new DPSXSectionInfo();
		public override string codename_str => "DPSX";//"XSPD"
													  // FIXME DEBUG
		public override Game[] supported_games{get;} =
		{
			CROC_2_PS1.Instance,
			CROC_2_DEMO_PS1_DUMMY.Instance,
			HARRY_POTTER_1_PS1.Instance,
			HARRY_POTTER_2_PS1.Instance
		};
		public override string section_content_description => "3D models, animations & level geometry";

		public override DPSXSection Parse(Parser data_in, Configuration conf)
		{
			var fallback_data = fallback_parse_data(data_in);
			var (size, start) = base.parseInner(data_in, conf);
			var idk1 = data_in.ReadBytes(4);
			var n_idk_unique_textures = data_in.ReadInt32();

			//TODO:Fonts and Sprites?
			if(conf.game != CROC_2_DEMO_PS1_DUMMY.Instance)
			{
				data_in.Seek(2048, SeekOrigin.Current);
			}
			else
			{
				data_in.Seek(2052, SeekOrigin.Current);
			}

			var n_models_3d = data_in.ReadInt32();
			var models_3d = new Object3DData[n_models_3d];
			for(int i=0; i<n_models_3d; i++)
			{
				models_3d[i]=Object3DData.Parse(data_in, conf);
			}

			var n_animations = data_in.ReadInt32();
			var animations = new AnimationData[n_animations];
			for(int i=0; i<n_animations; i++)
			{
				animations[i] = AnimationData.parse(data_in, conf);
			}

			//TODO: Cutscene data?
			if(conf.game==CROC_2_PS1.Instance || conf.game==CROC_2_DEMO_PS1.Instance)
			{
				var n_dpsx_legacy_textures = data_in.ReadInt32();
				data_in.Seek(n_dpsx_legacy_textures * 3072, SeekOrigin.Current);
			}

			var n_actors = data_in.ReadInt32();
			var actors = new ActorData[n_actors];
			for(int i=0; i<n_actors; i++)
			{
				actors[i] = ActorData.Parse(data_in, conf);
			}

			var level_file = LevelFile.parse(data_in, conf);

			// FIXME End of Croc 2 & Croc 2 Demo Dummy's level files aren't reversed yet
			if(conf.game!=CROC_2_PS1.Instance && conf.game!=CROC_2_DEMO_PS1_DUMMY.Instance)
			{
				check_size(size, start, data_in.Position);
			}
			return new DPSXSection(models_3d, animations, actors, level_file, fallback_data);
		}
	}

	public sealed class DPSXSection:BaseWADSection
	{
		public readonly IReadOnlyList<Object3DData> models_3d;
		public readonly IReadOnlyList<AnimationData> animations;
		public readonly IReadOnlyList<ActorData> actors;
		public readonly LevelFile level_file;

		public DPSXSection(Object3DData[] models_3d, AnimationData[] animations,  ActorData[] actors, LevelFile level_file, byte[] fallback_data = null):base(DPSXSectionInfo.Instance, fallback_data)
		{
			this.models_3d = models_3d;
			this.animations = animations;
			this.actors = actors;
			this.level_file = level_file;
		}
	}
}