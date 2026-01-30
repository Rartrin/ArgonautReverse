using System.Numerics;

namespace ArgonautReverse.PSX
{
	public sealed class AnimationDataPSX(int boneCount, IReadOnlyList<int> frameIndexes, IReadOnlyList<IReadOnlyList<Matrix4x4>> frames)
	{
		public readonly int n_vertices_groups = boneCount;
		public readonly IReadOnlyList<int> FrameIndexes = frameIndexes;
		public readonly IReadOnlyList<IReadOnlyList<Matrix4x4>> Frames = frames;

		public static unsafe AnimationDataPSX Parse(AnimationPSX animation)
		{
			IReadOnlyList<int> frameIndexes;
			Matrix4x4[][] frames;
			if(animation.KeyframeCount==0)
			{
				//Matrix transforms
				frameIndexes = Enumerable.Range(0, animation.FrameCount).ToArray();

				frames = new Matrix4x4[animation.FrameCount][];
				for(int frame_id = 0; frame_id < animation.FrameCount; frame_id++)
				{
					var frame = new Matrix4x4[animation.BoneCount];
					for(int i = 0; i < animation.BoneCount; i++)
					{
						var frameMatrix = animation.Bones[frame_id][i];
						//matrix = np.divide((sub_frame[..3], sub_frame[3..6], sub_frame[6..9]), 4096).T;  // Need to be reversed
						var matrix = new Matrix4x4//TODO: Still need to be reversed?
						(
							m11: frameMatrix.m[0] / 4096f,
							m12: frameMatrix.m[1] / 4096f,
							m13: frameMatrix.m[2] / 4096f,
							m14: 0,
							m21: frameMatrix.m[3] / 4096f,
							m22: frameMatrix.m[4] / 4096f,
							m23: frameMatrix.m[5] / 4096f,
							m24: 0,
							m31: frameMatrix.m[6] / 4096f,
							m32: frameMatrix.m[7] / 4096f,
							m33: frameMatrix.m[8] / 4096f,
							m34: 0,
							//Translation
							m41: frameMatrix.t[0] / 4096f,
							m42: frameMatrix.t[1] / 4096f,
							m43: frameMatrix.t[2] / 4096f,
							m44: 0
						);
						frame[i] = matrix;
					}
					frames[frame_id] = frame;
				}
			}
			else
			{
				//Keyframes
				var frameIndexList = new List<int>();
				frames = new Matrix4x4[animation.KeyframeCount][];
				for(int frame_id = 0; frame_id < animation.KeyframeCount; frame_id++)
				{
					var frame = new Matrix4x4[animation.BoneCount];
					
					for(int i = 0; i < animation.BoneCount; i++)
					{
						var keydata = animation.Keydata[frame_id][i];

						//TODO: The old Quaternion was w,x,y,z. This one is x,y,z,w
						var matrix = Matrix4x4.CreateFromQuaternion(new Quaternion(keydata.Rotation.W, keydata.Rotation.X, keydata.Rotation.Y, keydata.Rotation.Z));
						matrix.Translation = new Vector3(keydata.Position.X, keydata.Position.Y, keydata.Position.Z);
						if(animation.FrameCount != animation.KeyframeCount)
						{
							frameIndexList.Add(keydata.Frame);
						}
						frame[i] = matrix;
					}
					frames[frame_id] = frame;
				}
				frameIndexes = frameIndexList;
			}
			return new AnimationDataPSX(animation.BoneCount, frameIndexes, frames);
		}
	}
}