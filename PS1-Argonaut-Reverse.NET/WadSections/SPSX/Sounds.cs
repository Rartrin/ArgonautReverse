using ArgonautReverse.IO;

namespace ArgonautReverse.WadSections.SPSX
{
	public enum SoundEffectsAmbientFlags:uint{}


	public enum DialoguesBGMsSoundFlags:ushort
	{
		IS_STEREO = 0x1,
		IS_MONO = 0x2,
		IS_BACKGROUND_MUSIC = 0x4,
	}

	public abstract class Sound
	{
		public readonly uint sampling_rate;
		public VAGSoundData vag;
		protected int? _size;

		public Sound(uint sampling_rate, VAGSoundData vag = null, int? size = null)
		{
			this.sampling_rate = sampling_rate;
			this._size = size;
			this.vag = vag;
		}
		
		public int size => this.vag!=null ? this.vag.size : (this._size ?? 0);

		public virtual void parse_vag(WadReader data_in)
		{
			this._size = null;
		}

		public void serialize_vag(Serializer data_out) => this.vag.serialize(data_out);

		public abstract void serialize(Serializer data_out, uint end_section_offset);
	}

	public abstract class _AmbientOrEffectSound:Sound
	{
		public static uint[] known_values_1st_flags_byte = {0x00000000, 0x00000001};
		public static uint[] known_values_2nd_2rd_flags_bytes = {0x00000000, 0x00010100};

		public readonly SoundEffectsAmbientFlags flagsAndLoop;
		public readonly int volume_level;
		public readonly ushort uk1;
		public readonly ushort uk2;
		
		public _AmbientOrEffectSound(uint sampling_rate, int volume_level, SoundEffectsAmbientFlags flags, ushort uk1, ushort uk2, int size, VAGSoundData vag = null):base(sampling_rate, vag, size)
		{
			this.flagsAndLoop = flags;
			this.volume_level = volume_level;
			this.uk1 = uk1;
			this.uk2 = uk2;
		}
		
		protected static void ParseInner(WadReader data_in, out uint sampling_rate, out short volume_level, out SoundEffectsAmbientFlags flags, out ushort uk1, out ushort uk2, out int size)
		{
			sampling_rate = data_in.ReadUInt32();
			//TODO: Seek
			var pitch = data_in.ReadInt16();// "Compressed" sampling rate, see SPSX's documentation
			volume_level = data_in.ReadInt16();
			flags = (SoundEffectsAmbientFlags)data_in.ReadUInt32();
			uk1 = data_in.ReadUInt16();
			uk2 = data_in.ReadUInt16();
			size = data_in.ReadInt32();
			//return new cls(sampling_rate, volume_level, flags, uk1, uk2, size);
		}

		public override void serialize(Serializer data_out, uint end_section_offset)
		{
			var rounded_sampling_rate = (ushort)Math.Round((this.sampling_rate * 4096) / 44100.0);
			PackIHHI2s2sI
			(
				data_out,
				this.sampling_rate,
				rounded_sampling_rate,
				(ushort)this.volume_level,
				this.flagsAndLoop,
				this.uk1,
				this.uk2,
				(uint)this.size
			);
		}
		public override void parse_vag(WadReader data_in)
		{
			this.vag = VAGSoundData.parse
			(
				data_in,
				size:this._size.Value,
				n_channels:VAGSoundData.MONO,
				sampling_rate:this.sampling_rate
			);
			base.parse_vag(data_in);
		}

		protected static void PackIHHI2s2sI(Serializer writer, uint samplingRate, ushort roundedSamplingRate, ushort volumeLevel, SoundEffectsAmbientFlags flags, ushort uk1, ushort uk2, uint size)
		{
			//<IHHI2s2sI
			writer.WriteUInt32(samplingRate);
			writer.WriteUInt16(roundedSamplingRate);
			writer.WriteUInt16(volumeLevel);
			writer.WriteUInt32((uint)flags);
			writer.WriteUInt16(uk1);
			writer.WriteUInt16(uk2);
			writer.WriteUInt32(size);
		}
	}

	public sealed class AmbientSound:_AmbientOrEffectSound
	{
		private AmbientSound(uint sampling_rate, int volume_level, SoundEffectsAmbientFlags flags, ushort uk1, ushort uk2, int size, VAGSoundData vag = null) : base(sampling_rate, volume_level, flags, uk1, uk2, size, vag){}

		public static AmbientSound parse(WadReader data_in)
		{
			ParseInner(data_in, out var sampling_rate, out var volume_level, out var flags, out var uk1, out var uk2, out var size);
			return new AmbientSound(sampling_rate, volume_level, flags, uk1, uk2, size);
		}

		public override void serialize(Serializer data_out, uint end_section_offset)
		{
			var rounded_sampling_rate = (ushort)Math.Round((this.sampling_rate * 4096) / 48000.0);
			PackIHHI2s2sI
			(
				data_out,
				this.sampling_rate,
				rounded_sampling_rate,
				(ushort)this.volume_level,
				this.flagsAndLoop,
				this.uk1,
				this.uk2,
				(uint)this.size
			);
		}
	}


	public sealed class EffectSound:_AmbientOrEffectSound
	{
		private EffectSound(uint sampling_rate, int volume_level, SoundEffectsAmbientFlags flags, ushort uk1, ushort uk2, int size, VAGSoundData vag = null) : base(sampling_rate, volume_level, flags, uk1, uk2, size, vag){}

		public static EffectSound parse(WadReader data_in)
		{
			ParseInner(data_in, out var sampling_rate, out var volume_level, out var flags, out var uk1, out var uk2, out var size);
			var res = new EffectSound(sampling_rate, volume_level, flags, uk1, uk2, size);
			Utils.Assert(known_values_1st_flags_byte.Contains((uint)res.flagsAndLoop & 0x000000FF));
			Utils.Assert(known_values_2nd_2rd_flags_bytes.Contains((uint)res.flagsAndLoop & 0x00FFFF00));
			Utils.Assert(res.uk2 == 0x0042);
			return res;
		}
	}

	public sealed class DialogueBGMSound:Sound
	{
		public readonly DialoguesBGMsSoundFlags flagsAndLoop;
		public readonly uint uk1;

		private DialogueBGMSound(uint sampling_rate, DialoguesBGMsSoundFlags flags, uint uk1, int size, VAGSoundData vag = null):base(sampling_rate, vag, size)
		{
			this.flagsAndLoop = flags;
			this.uk1 = uk1;
		}

		public static DialogueBGMSound parse(WadReader data_in)
		{
			data_in.Seek(4, SeekOrigin.Current);// END section offset
			var sampling_rate = (uint)Math.Round((data_in.ReadUInt16() * 44100) / 4096.0);
			var flags = (DialoguesBGMsSoundFlags)data_in.ReadUInt16();
			var uk1 = data_in.ReadUInt32();
			var size = data_in.ReadInt32();
			return new DialogueBGMSound(sampling_rate, flags, uk1, size);
		}

		public override void serialize(Serializer data_out, uint end_section_offset)
		{
			var rounded_sampling_rate = (ushort)Math.Round((this.sampling_rate * 4096) / 44100.0);
			PackIHH4sI
			(
				data_out,
				end_section_offset,
				rounded_sampling_rate,
				this.flagsAndLoop,
				this.uk1,
				(uint)this.size
			);
		}
		public override void parse_vag(WadReader data_in)
		{
			this.vag = VAGSoundData.parse
			(
				data_in,
				size:this._size.Value,
				sampling_rate:this.sampling_rate,
				n_channels: (this.flagsAndLoop&DialoguesBGMsSoundFlags.IS_STEREO)!= 0 ? VAGSoundData.STEREO : VAGSoundData.MONO
			);
			base.parse_vag(data_in);
		}

		private static void PackIHH4sI(Serializer writer, uint end_section_offset, ushort rounded_sampling_rate, DialoguesBGMsSoundFlags flags, uint uk1, uint size)
		{
			//<IHH4sI
			writer.WriteUInt32(end_section_offset);
			writer.WriteUInt16(rounded_sampling_rate);
			writer.WriteUInt16((ushort)flags);
			writer.WriteUInt32(uk1);
			writer.WriteUInt32(size);
		}
	}
}