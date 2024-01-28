using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
    public sealed class AnimationHeaderPSX
    {
        public const int base_frame_data_size = 4;
        public const int inter_frames_header_size = 8;

        public readonly int n_total_frames;
        public readonly int n_stored_frames;
        public readonly int n_vertices_groups;
        public readonly int n_flags;
        public readonly bool has_additional_data;
        public readonly byte[][] flags;
        public readonly bool old_animation_format;
        public readonly int n_inter_frames;
        public readonly int sub_frame_size;

        public AnimationHeaderPSX(
            int n_total_frames,
            int n_stored_frames,
            int n_vertex_groups,
            int n_flags,
            bool has_additional_data,
            byte[][] flags,
            bool old_animation_format,
            int n_inter_frames = 0)
        {
            this.n_total_frames = n_total_frames;
            this.n_stored_frames = n_stored_frames;
            n_vertices_groups = n_vertex_groups;
            this.n_flags = n_flags;
            this.has_additional_data = has_additional_data;
            this.flags = flags;
            this.old_animation_format = old_animation_format;
            this.n_inter_frames = n_inter_frames;
            sub_frame_size = old_animation_format ? 24 : 16;
        }

        public static AnimationHeaderPSX parse(WadReader data_in)
        {
            //base.parse(data_in, conf);
            int n_flags = data_in.Read<int>();
            data_in.Position += 4;
            int n_total_frames = data_in.Read<int>();
            int has_additional_frame_data_value = data_in.Read<int>();
            bool has_additional_data = has_additional_frame_data_value == 0;
            int n_stored_frames = 0;
            int n_inter_frames;
            if (data_in.ReadVersion == CROC_2_PS1.WadVersion || data_in.ReadVersion == CROC_2_DEMO_PS1.WadVersion || data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
            {
                n_inter_frames = data_in.Read<int>();
                if (n_inter_frames != 0)
                {
                    n_stored_frames = n_total_frames;
                }
                data_in.Position += 4;
            }
            else// Harry Potter 1 & 2
            {
                //TODO: What data is here?
                data_in.Position += 8;
                n_inter_frames = 0;
            }
            int n_vertex_groups = data_in.Read<int>();
            data_in.SkipBytes(4);

            if (data_in.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion)
            {
                n_stored_frames = data_in.Read<int>();
                data_in.SkipBytes(12);
            }
            var flags = new byte[n_flags][];
            for (int i = 0; i < n_flags; i++)
            {
                flags[i] = data_in.ReadArray<byte>(4);
            }
            if (has_additional_data)
            {
                data_in.SkipBytes(8 * n_total_frames);
            }
            data_in.SkipBytes(4 * n_total_frames);  // Total frames info
            data_in.SkipBytes(n_inter_frames * inter_frames_header_size);  // Inter-frames header
            if (data_in.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion == HARRY_POTTER_2_PS1.WadVersion || n_inter_frames != 0)
            {
                data_in.SkipBytes(4 * n_stored_frames);  // Stored frames info
            }
            bool old_animation_format;
            if (n_stored_frames == 0 || n_inter_frames != 0)  // Rotation matrices
            {
                old_animation_format = true;
                n_stored_frames = n_total_frames;
            }
            else// Unit quaternions
            {
                old_animation_format = false;
            }
            if (n_total_frames > 500 || n_total_frames == 0)
            {
                if (data_in.Configuration.IgnoreWarnings)
                {
                    AnimationsWarning.Warn(n_total_frames);
                }
                else
                {
                    throw new AnimationsWarning(data_in.AbsolutePosition, n_total_frames);
                }
            }
            return new AnimationHeaderPSX(n_total_frames, n_stored_frames, n_vertex_groups, n_flags, has_additional_data, flags, old_animation_format, n_inter_frames);
        }
    }
}