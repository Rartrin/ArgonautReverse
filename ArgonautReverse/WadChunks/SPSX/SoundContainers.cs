using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.SPSX
{
	public abstract class SoundsContainer
	{
		public List<Sound> Sounds{get;}

		public SoundsContainer(IReadOnlyList<Sound> sounds = null)
		{
			Sounds = sounds!=null ? new List<Sound>(sounds) : new List<Sound>();
		}

		public int size => Sounds.Sum(sound => sound.size);

		public IEnumerable<VAGSoundData> vags => Sounds.Select(sound => sound.vag);

		public virtual void serialize(Serializer data_out)
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

		public void serialize_vags(Serializer data_out)
		{
			foreach(var sound in Sounds)
			{
				sound.serialize_vag(data_out);
			}
		}
	}

	public sealed class CommonSFXContainer:SoundsContainer
	{
		public CommonSFXContainer(IReadOnlyList<Sound> sounds = null) : base(sounds){}
	}

	public sealed class AmbientContainer:SoundsContainer
	{
		public AmbientContainer(IReadOnlyList<Sound> sounds = null) : base(sounds){}
	}

	public sealed class LevelSFXGroupContainer:SoundsContainer
	{
		private int? _n_sound_effects;

		public LevelSFXGroupContainer(IReadOnlyList<Sound> sounds = null, int? n_sound_effects = null):base(sounds)
		{
			this._n_sound_effects = n_sound_effects;
		}

		//public int size => Sounds.Sum(sound => sound.size);

		public static LevelSFXGroupContainer parse(WadReader data_in)
		{
			data_in.SkipBytes(4);//Group header offset
			int n_sound_effects = data_in.Read<int>();
			data_in.SkipBytes(8);// End offset (4 bytes) | Sum of group VAGs' sizes (4 bytes)
			return new LevelSFXGroupContainer(Array.Empty<Sound>(), n_sound_effects);
		}

		public override void serialize(Serializer data_out) => throw new Exception();
		public void serialize(Serializer data_out, int group_header_offset, int end_offset)
		{
			data_out.WriteInt32(group_header_offset);
			data_out.WriteInt32(Sounds.Count);
			data_out.WriteInt32(end_offset);
			data_out.WriteInt32(this.size);
		}

		public void parse_children(WadReader data_in)
		{
			for(int i=0; i<_n_sound_effects; i++)
			{
				Sounds.Add(EffectSound.parse(data_in));
			}
			this._n_sound_effects = null;
		}

		public void serialize_children(Serializer data_out)
		{
			foreach(var sound in Sounds)
			{
				sound.serialize(data_out, 0);
			}
		}
	}
	public sealed class LevelSFXContainer
	{
		public IReadOnlyList<LevelSFXGroupContainer> Groups{get;}

		public LevelSFXContainer(IReadOnlyList<LevelSFXGroupContainer> groups = null)
		{
			Groups = groups ?? Array.Empty<LevelSFXGroupContainer>();
		}
		
		public int n_sounds => Groups.Sum(group => group.Sounds.Count);

		public int size => Groups.Sum(group => group.size);

		public IEnumerable<Sound> Sounds => Groups.SelectMany(group => group.Sounds);

		public IEnumerable<VAGSoundData> vags => Groups.SelectMany(group => group.Sounds.Select(sound => sound.vag));

		public void serialize(Serializer data_out)
		{
			int group_header_offset = 0;
			int end_offset = 0;
			foreach(var group in Groups)
			{
				group.serialize(
					data_out,
					group_header_offset:group_header_offset,
					end_offset:end_offset
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
		public void serialize_vags(Serializer data_out)
		{
			foreach(var group in Groups)
			{
				data_out.Position = 2048 * (int)Math.Ceiling(data_out.Position / 2048.0);
				group.serialize_vags(data_out);
			}
		}
	}

	public sealed class DialoguesBGMsContainer:SoundsContainer
	{
		public DialoguesBGMsContainer(IReadOnlyList<Sound> sounds = null):base(sounds){}

		public override void parse_vags(WadReader data_in)
		{
			foreach(var sound in this.Sounds)
			{
				data_in.AbsolutePosition = 2048 * (int)Math.Ceiling(data_in.AbsolutePosition / 2048.0);
				sound.parse_vag(data_in);
			}
		}

		public override void serialize(Serializer data_out)
		{
			int end_section_offset = 0;
			foreach(var sound in Sounds)
			{
				sound.serialize(data_out, end_section_offset:(uint)end_section_offset);
				end_section_offset += sound.size;
			}
		}
	}
}