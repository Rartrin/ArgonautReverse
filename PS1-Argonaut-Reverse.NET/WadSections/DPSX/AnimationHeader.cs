namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class AnimationHeader:BaseDataClass
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

		public AnimationHeader(
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
			this.n_vertices_groups = n_vertex_groups;
			this.n_flags = n_flags;
			this.has_additional_data = has_additional_data;
			this.flags = flags;
			this.old_animation_format = old_animation_format;
			this.n_inter_frames = n_inter_frames;
			this.sub_frame_size = old_animation_format ? 24 : 16;
		}

		public static AnimationHeader parse(Parser data_in, Configuration conf)
		{
			//base.parse(data_in, conf);
			int n_flags = data_in.ReadInt32();
			data_in.Position += 4;
			int n_total_frames = data_in.ReadInt32();
			int has_additional_frame_data_value = data_in.ReadInt32();
			bool has_additional_data = has_additional_frame_data_value == 0;
			int n_stored_frames = 0;
			int n_inter_frames;
			if(conf.game==G.CROC_2_PS1 || conf.game==G.CROC_2_DEMO_PS1 || conf.game==G.CROC_2_DEMO_PS1_DUMMY)
			{
				n_inter_frames = data_in.ReadInt32();
				if(n_inter_frames != 0)
				{
					n_stored_frames = n_total_frames;
				}
				data_in.Position += 4;
			}
			else// Harry Potter 1 & 2
			{
				data_in.Position += 8;
				n_inter_frames = 0;
			}
			int n_vertex_groups = data_in.ReadInt32();
			data_in.Seek(4, SeekOrigin.Current);

			if(conf.game==G.HARRY_POTTER_1_PS1 || conf.game==G.HARRY_POTTER_2_PS1)
			{
				n_stored_frames = data_in.ReadInt32();
				data_in.Seek(12, SeekOrigin.Current);
			}
			var flags = new byte[n_flags][];
			for(int i=0; i<n_flags; i++)
			{
				flags[i] = data_in.ReadBytes(4);
			}
			if(has_additional_data)
			{
				data_in.Seek(8 * n_total_frames, SeekOrigin.Current);
			}
			data_in.Seek(4 * n_total_frames, SeekOrigin.Current);  // Total frames info
			data_in.Seek(n_inter_frames * inter_frames_header_size, SeekOrigin.Current);  // Inter-frames header
			if ((conf.game==G.HARRY_POTTER_1_PS1 || conf.game==G.HARRY_POTTER_2_PS1) || n_inter_frames != 0)
			{
				data_in.Seek(4 * n_stored_frames, SeekOrigin.Current);  // Stored frames info
			}
			bool old_animation_format;
			if(n_stored_frames == 0 || n_inter_frames != 0)  // Rotation matrices
			{
				old_animation_format = true;
				n_stored_frames = n_total_frames;
			}
			else// Unit quaternions
			{
				old_animation_format = false;
			}
			if(n_total_frames > 500 || n_total_frames == 0)
			{
				if(conf.ignore_warnings)
				{
					AnimationsWarning.Warn(n_total_frames);
				}
				else
				{
					throw new AnimationsWarning(data_in.Position, n_total_frames);
				}
			}
			return new AnimationHeader(n_total_frames, n_stored_frames, n_vertex_groups, n_flags, has_additional_data, flags, old_animation_format, n_inter_frames);
		}
	}
}