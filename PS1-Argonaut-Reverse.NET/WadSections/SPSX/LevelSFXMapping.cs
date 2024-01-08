using System.Text;

namespace ArgonautReverse.WadSections.SPSX
{
	public sealed class LevelSFXMapping:BaseDataClass
	{
		public IReadOnlyDictionary<int,int> channel_group_mapping;
		public IReadOnlyList<int> sounds_hashes;
		public IReadOnlyList<string> _mapping;

		public LevelSFXMapping(IReadOnlyList<string> mapping = null)
		{
			this._mapping = mapping;
		}
		
		public int n_unique_level_sfx => this._mapping!=null ? this._mapping.Count : this.sounds_hashes.Count;

		public static LevelSFXMapping parse(Parser data_in, Configuration conf, int n_unique_level_sfx)
		{
			var mapping = new string[n_unique_level_sfx];
			for(int i=0; i<n_unique_level_sfx; i++)
			{
				mapping[i] = Encoding.ASCII.GetString(data_in.ReadBytes(16));
			}
			return new LevelSFXMapping(mapping:mapping);
		}

		public void serialize(Serializer data_out, Configuration conf, LevelSFXContainer level_sfx_groups)
		{
			var mapping = new byte[this.n_unique_level_sfx][];
			for(int i=0; i<this.n_unique_level_sfx; i++)
			{
				mapping[i] = new byte[16];
				Array.Fill<byte>(mapping[i], 255);
			}
			for(int group_id=0; group_id<level_sfx_groups.Groups.Count; group_id++)
			{
				var group = level_sfx_groups.Groups[group_id];
				var channel_id = this.channel_group_mapping[group_id];
				var count = new Dictionary<int,int>();
				for(int sound_id=0; sound_id<group.Sounds.Count; sound_id++)
				{
					var sound = group.Sounds[sound_id];//EffectSound
					var sound_hash = HashCode.Combine(sound.vag.data);//TODO: Proper hashing
					var countValue = count.GetValueOrDefault(sound_hash);
					int unique_sound_id = this.sounds_hashes.Select((int i,int v) => (i,v)).Where(e => e.v == sound_hash).Select(e => e.i).ElementAt(countValue);
					count[sound_hash] = countValue + 1;
					if(sound_id>255)
					{
						throw new Exception("Perhaps this should not be a byte?");
					}
					mapping[unique_sound_id][channel_id] = (byte)sound_id;
				}
			}
			foreach(var e in mapping)
			{
				data_out.WriteBytes(e);
			}
		}
		public void parse_mapping(LevelSFXContainer level_sfx_groups)
		{
			var channel_group_mapping = new Dictionary<int,int>();

			var sounds_hashes = new int[this.n_unique_level_sfx];
			int group_id = 0;

			for(int channel_id=1; channel_id<16; channel_id++)
			{
				bool empty_channel = true;
				for(int unique_sound_id=0; unique_sound_id<this._mapping.Count; unique_sound_id++)
				{
					var sound_id = this._mapping[unique_sound_id][channel_id];
					if(sound_id != 255)
					{
						empty_channel = false;
						//TODO: Proper hashing
						sounds_hashes[unique_sound_id] = HashCode.Combine(level_sfx_groups.Groups[group_id].Sounds[sound_id].vag.data);
					}
				}
				if(!empty_channel)
				{
					channel_group_mapping[group_id] = channel_id;
					group_id += 1;
				}
			}
			this._mapping = null;
			this.channel_group_mapping = channel_group_mapping;
			this.sounds_hashes = sounds_hashes;
		}
	}
}