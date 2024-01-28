using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
    public enum SoundEffectsAmbientFlagsPSX : uint { }


    public enum DialoguesBGMsSoundFlagsPSX : ushort
    {
        IS_STEREO = 0x1,
        IS_MONO = 0x2,
        IS_BACKGROUND_MUSIC = 0x4,
    }

    public abstract class SoundPSX
    {
        public readonly uint sampling_rate;
        public VAGSoundDataPSX vag;
        protected int? _size;

        public SoundPSX(uint sampling_rate, VAGSoundDataPSX vag = null, int? size = null)
        {
            this.sampling_rate = sampling_rate;
            _size = size;
            this.vag = vag;
        }

        public int size => vag != null ? vag.size : _size ?? 0;

        public virtual void parse_vag(WadReader data_in)
        {
            _size = null;
        }

        public void serialize_vag(Serializer data_out) => vag.serialize(data_out);

        public abstract void serialize(Serializer data_out, uint end_section_offset);
    }

    public abstract class _AmbientOrEffectSoundPSX : SoundPSX
    {
        public static uint[] known_values_1st_flags_byte = { 0x00000000, 0x00000001 };
        public static uint[] known_values_2nd_2rd_flags_bytes = { 0x00000000, 0x00010100 };

        public readonly SoundEffectsAmbientFlagsPSX flagsAndLoop;
        public readonly int volume_level;
        public readonly ushort uk1;
        public readonly ushort uk2;

        public _AmbientOrEffectSoundPSX(uint sampling_rate, int volume_level, SoundEffectsAmbientFlagsPSX flags, ushort uk1, ushort uk2, int size, VAGSoundDataPSX vag = null) : base(sampling_rate, vag, size)
        {
            flagsAndLoop = flags;
            this.volume_level = volume_level;
            this.uk1 = uk1;
            this.uk2 = uk2;
        }

        protected static void ParseInner(WadReader data_in, out uint sampling_rate, out short volume_level, out SoundEffectsAmbientFlagsPSX flags, out ushort uk1, out ushort uk2, out int size)
        {
            sampling_rate = data_in.Read<uint>();
            //TODO: Seek
            var pitch = data_in.Read<short>();// "Compressed" sampling rate, see SPSX's documentation
            volume_level = data_in.Read<short>();
            flags = (SoundEffectsAmbientFlagsPSX)data_in.Read<uint>();
            uk1 = data_in.Read<ushort>();
            uk2 = data_in.Read<ushort>();
            size = data_in.Read<int>();
            //return new cls(sampling_rate, volume_level, flags, uk1, uk2, size);
        }

        public override void serialize(Serializer data_out, uint end_section_offset)
        {
            var rounded_sampling_rate = (ushort)Math.Round(sampling_rate * 4096 / 44100.0);
            PackIHHI2s2sI
            (
                data_out,
                sampling_rate,
                rounded_sampling_rate,
                (ushort)volume_level,
                flagsAndLoop,
                uk1,
                uk2,
                (uint)size
            );
        }
        public override void parse_vag(WadReader data_in)
        {
            vag = VAGSoundDataPSX.parse
            (
                data_in,
                size: _size.Value,
                n_channels: VAGSoundDataPSX.MONO,
                sampling_rate: sampling_rate
            );
            base.parse_vag(data_in);
        }

        protected static void PackIHHI2s2sI(Serializer writer, uint samplingRate, ushort roundedSamplingRate, ushort volumeLevel, SoundEffectsAmbientFlagsPSX flags, ushort uk1, ushort uk2, uint size)
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

    public sealed class AmbientSoundPSX : _AmbientOrEffectSoundPSX
    {
        private AmbientSoundPSX(uint sampling_rate, int volume_level, SoundEffectsAmbientFlagsPSX flags, ushort uk1, ushort uk2, int size, VAGSoundDataPSX vag = null) : base(sampling_rate, volume_level, flags, uk1, uk2, size, vag) { }

        public static AmbientSoundPSX parse(WadReader data_in)
        {
            ParseInner(data_in, out var sampling_rate, out var volume_level, out var flags, out var uk1, out var uk2, out var size);
            return new AmbientSoundPSX(sampling_rate, volume_level, flags, uk1, uk2, size);
        }

        public override void serialize(Serializer data_out, uint end_section_offset)
        {
            var rounded_sampling_rate = (ushort)Math.Round(sampling_rate * 4096 / 48000.0);
            PackIHHI2s2sI
            (
                data_out,
                sampling_rate,
                rounded_sampling_rate,
                (ushort)volume_level,
                flagsAndLoop,
                uk1,
                uk2,
                (uint)size
            );
        }
    }


    public sealed class EffectSoundPSX : _AmbientOrEffectSoundPSX
    {
        private EffectSoundPSX(uint sampling_rate, int volume_level, SoundEffectsAmbientFlagsPSX flags, ushort uk1, ushort uk2, int size, VAGSoundDataPSX vag = null) : base(sampling_rate, volume_level, flags, uk1, uk2, size, vag) { }

        public static EffectSoundPSX parse(WadReader data_in)
        {
            ParseInner(data_in, out var sampling_rate, out var volume_level, out var flags, out var uk1, out var uk2, out var size);
            var res = new EffectSoundPSX(sampling_rate, volume_level, flags, uk1, uk2, size);
            Utils.Assert(known_values_1st_flags_byte.Contains((uint)res.flagsAndLoop & 0x000000FF));
            Utils.Assert(known_values_2nd_2rd_flags_bytes.Contains((uint)res.flagsAndLoop & 0x00FFFF00));
            Utils.Assert(res.uk2 == 0x0042);
            return res;
        }
    }

    public sealed class DialogueBGMSoundPSX : SoundPSX
    {
        public readonly DialoguesBGMsSoundFlagsPSX flagsAndLoop;
        public readonly uint uk1;

        private DialogueBGMSoundPSX(uint sampling_rate, DialoguesBGMsSoundFlagsPSX flags, uint uk1, int size, VAGSoundDataPSX vag = null) : base(sampling_rate, vag, size)
        {
            flagsAndLoop = flags;
            this.uk1 = uk1;
        }

        public static DialogueBGMSoundPSX parse(WadReader data_in)
        {
            data_in.SkipBytes(4);// END section offset
            var sampling_rate = (uint)Math.Round(data_in.Read<ushort>() * 44100 / 4096.0);
            var flags = (DialoguesBGMsSoundFlagsPSX)data_in.Read<ushort>();
            var uk1 = data_in.Read<uint>();
            var size = data_in.Read<int>();
            return new DialogueBGMSoundPSX(sampling_rate, flags, uk1, size);
        }

        public override void serialize(Serializer data_out, uint end_section_offset)
        {
            var rounded_sampling_rate = (ushort)Math.Round(sampling_rate * 4096 / 44100.0);
            PackIHH4sI
            (
                data_out,
                end_section_offset,
                rounded_sampling_rate,
                flagsAndLoop,
                uk1,
                (uint)size
            );
        }
        public override void parse_vag(WadReader data_in)
        {
            vag = VAGSoundDataPSX.parse
            (
                data_in,
                size: _size.Value,
                sampling_rate: sampling_rate,
                n_channels: (flagsAndLoop & DialoguesBGMsSoundFlagsPSX.IS_STEREO) != 0 ? VAGSoundDataPSX.STEREO : VAGSoundDataPSX.MONO
            );
            base.parse_vag(data_in);
        }

        private static void PackIHH4sI(Serializer writer, uint end_section_offset, ushort rounded_sampling_rate, DialoguesBGMsSoundFlagsPSX flags, uint uk1, uint size)
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