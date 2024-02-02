using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public abstract class SoundsContainerPSX
	{
		public List<SoundPSX> Sounds{get;}

		public SoundsContainerPSX(IReadOnlyList<SoundPSX> sounds = null)
		{
			Sounds = sounds != null ? new List<SoundPSX>(sounds) : new List<SoundPSX>();
		}

		public int size => Sounds.Sum(sound => sound.size);

		public IEnumerable<VAGSoundDataPSX> vags => Sounds.Select(sound => sound.vag);

		public virtual void serialize(WadWriter data_out)
		{
			foreach(var sound in Sounds)
			{
				sound.serialize(data_out, 0);
			}
		}

		public virtual void parse_vags(WadReader data_in)
		{
			foreach(var sound in Sounds)
			{
				sound.parse_vag(data_in);
			}
		}

		public void serialize_vags(WadWriter data_out)
		{
			foreach(var sound in Sounds)
			{
				sound.serialize_vag(data_out);
			}
		}
	}

	public sealed class CommonSFXContainerPSX:SoundsContainerPSX
	{
		public CommonSFXContainerPSX(IReadOnlyList<SoundPSX> sounds = null) : base(sounds) { }
	}

	public sealed class AmbientContainerPSX:SoundsContainerPSX
	{
		public AmbientContainerPSX(IReadOnlyList<SoundPSX> sounds = null) : base(sounds) { }
	}

	public sealed class LevelSFXGroupContainerPSX:SoundsContainerPSX
	{
		private int? _n_sound_effects;

		public LevelSFXGroupContainerPSX(IReadOnlyList<SoundPSX> sounds = null, int? n_sound_effects = null) : base(sounds)
		{
			_n_sound_effects = n_sound_effects;
		}

		//public int size => Sounds.Sum(sound => sound.size);

		public static LevelSFXGroupContainerPSX parse(WadReader data_in)
		{
			data_in.SkipBytes(4);//Group header offset
			int n_sound_effects = data_in.Read<int>();
			data_in.SkipBytes(8);// End offset (4 bytes) | Sum of group VAGs' sizes (4 bytes)
			return new LevelSFXGroupContainerPSX(Array.Empty<SoundPSX>(), n_sound_effects);
		}

		public override void serialize(WadWriter data_out) => throw new Exception();
		public void serialize(WadWriter data_out, int group_header_offset, int end_offset)
		{
			data_out.WriteInt32(group_header_offset);
			data_out.WriteInt32(Sounds.Count);
			data_out.WriteInt32(end_offset);
			data_out.WriteInt32(size);
		}

		public void parse_children(WadReader data_in)
		{
			for(int i = 0; i < _n_sound_effects; i++)
			{
				Sounds.Add(EffectSoundPSX.parse(data_in));
			}
			_n_sound_effects = null;
		}

		public void serialize_children(WadWriter data_out)
		{
			foreach(var sound in Sounds)
			{
				sound.serialize(data_out, 0);
			}
		}
	}
	public sealed class LevelSFXContainerPSX
	{
		public IReadOnlyList<LevelSFXGroupContainerPSX> Groups { get; }

		public LevelSFXContainerPSX(IReadOnlyList<LevelSFXGroupContainerPSX> groups = null)
		{
			Groups = groups ?? Array.Empty<LevelSFXGroupContainerPSX>();
		}

		public int n_sounds => Groups.Sum(group => group.Sounds.Count);

		public int size => Groups.Sum(group => group.size);

		public IEnumerable<SoundPSX> Sounds => Groups.SelectMany(group => group.Sounds);

		public IEnumerable<VAGSoundDataPSX> vags => Groups.SelectMany(group => group.Sounds.Select(sound => sound.vag));

		public void serialize(WadWriter data_out)
		{
			int group_header_offset = 0;
			int end_offset = 0;
			foreach(var group in Groups)
			{
				group.serialize(
					data_out,
					group_header_offset: group_header_offset,
					end_offset: end_offset
				);
				group_header_offset += 20 * group.Sounds.Count;
				end_offset += Utils.RoundUpPadding(group.size);
			}
		}

		public void parse_groups(WadReader data_in)
		{
			foreach(var group in Groups)
			{
				group.parse_children(data_in);
			}
		}
		public void parse_vags(WadReader data_in)
		{
			foreach(var group in Groups)
			{
				data_in.AbsolutePosition = 2048 * (int)Math.Ceiling(data_in.AbsolutePosition / 2048.0);
				group.parse_vags(data_in);
			}
		}
		public void serialize_vags(WadWriter data_out)
		{
			foreach(var group in Groups)
			{
				data_out.Position = 2048 * (int)Math.Ceiling(data_out.Position / 2048.0);
				group.serialize_vags(data_out);
			}
		}
	}

	public sealed class DialoguesBGMsContainerPSX:SoundsContainerPSX
	{
		public DialoguesBGMsContainerPSX(IReadOnlyList<SoundPSX> sounds = null) : base(sounds) { }

		public override void parse_vags(WadReader data_in)
		{
			foreach(var sound in Sounds)
			{
				data_in.AbsolutePosition = 2048 * (int)Math.Ceiling(data_in.AbsolutePosition / 2048.0);
				sound.parse_vag(data_in);
			}
		}

		public override void serialize(WadWriter data_out)
		{
			int end_section_offset = 0;
			foreach(var sound in Sounds)
			{
				sound.serialize(data_out, end_section_offset: (uint)end_section_offset);
				end_section_offset += sound.size;
			}
		}
	}
}