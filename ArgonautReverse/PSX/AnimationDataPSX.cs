using System.Numerics;
using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public sealed class AnimationDataPSX
	{
		public readonly AnimationHeaderPSX header;
		public readonly IReadOnlyList<IReadOnlyList<Matrix4x4>> frames;

		public AnimationDataPSX(AnimationHeaderPSX header, IReadOnlyList<IReadOnlyList<Matrix4x4>> frames)
		{
			this.frames = frames;
			this.header = header;
		}

		public IReadOnlyList<Matrix4x4> this[int item] => frames[item];

		public int n_vertices_groups => header.n_vertices_groups;

		public static AnimationDataPSX Parse(WadReader data_in)
		{
			//base.parse(data_in, conf);
			var header = AnimationHeaderPSX.parse(data_in);

			var frame_indexes = new List<int>();
			if(header.n_total_frames == header.n_stored_frames)
			{
				frame_indexes.AddRange(Enumerable.Range(0, header.n_total_frames));
			}

			var inter_frames_size = 4 * (int)Math.Ceiling(header.n_inter_frames * 2 / 4f);
			var frames = new IReadOnlyList<Matrix4x4>[header.n_stored_frames];

			for(int frame_id = 0; frame_id < header.n_stored_frames; frame_id++)
			{
				var frame = new Matrix4x4[header.n_vertices_groups];
				for(int i = 0; i < header.n_vertices_groups; i++)
				{
					var sub_frame = new short[header.sub_frame_size / 2];//Floor division
					for(int j = 0; j < sub_frame.Length; j++)
					{
						sub_frame[j] = data_in.Read<short>();//Signed
					}
					Matrix4x4 matrix;
					if(header.old_animation_format)
					{
						//matrix = np.divide((sub_frame[..3], sub_frame[3..6], sub_frame[6..9]), 4096).T;  // Need to be reversed
						matrix = new Matrix4x4//TODO: Still need to be reversed?
						(
							m11: sub_frame[0] / 4096f,
							m12: sub_frame[1] / 4096f,
							m13: sub_frame[2] / 4096f,
							m14: 0,
							m21: sub_frame[3] / 4096f,
							m22: sub_frame[4] / 4096f,
							m23: sub_frame[5] / 4096f,
							m24: 0,
							m31: sub_frame[6] / 4096f,
							m32: sub_frame[7] / 4096f,
							m33: sub_frame[8] / 4096f,
							m34: 0,
							//Translation
							m41: sub_frame[9] / 4096f,
							m42: sub_frame[10] / 4096f,
							m43: sub_frame[11] / 4096f,
							m44: 0
						);
					}
					else
					{
						//TODO: The old Quaternion was w,x,y,z. This one is x,y,z,w
						matrix = Matrix4x4.CreateFromQuaternion(new Quaternion(sub_frame[0], sub_frame[1], sub_frame[2], sub_frame[3]));//.rotation_matrix;
						matrix.Translation = new Vector3(sub_frame[4], sub_frame[5], sub_frame[6]);
						if(header.n_total_frames != header.n_stored_frames)
						{
							frame_indexes.Add(sub_frame[7]);
						}
					}
					frame[i] = matrix;
				}
				if(header.n_inter_frames != 0 && frame_id != header.n_stored_frames - 1)
				{
					data_in.SkipBytes(inter_frames_size);
				}
				frames[frame_id] = frame;
			}
			return new AnimationDataPSX(header, frames);
		}
	}
}