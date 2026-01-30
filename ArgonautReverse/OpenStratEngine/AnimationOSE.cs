using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.OpenStratEngine
{
	public readonly record struct QuatOSE(short W, short X, short Y, short Z):IWritable
	{
		public void Write(WadWriter writer)
		{
			writer.Write<short>(W);
			writer.Write<short>(X);
			writer.Write<short>(Y);
			writer.Write<short>(Z);
		}
	}

	public sealed class AnimTriggerOSE(ushort frame, ushort trigger):IWritable
	{
		public readonly ushort Frame = frame;
		public readonly ushort Trigger = trigger;

		public void Write(WadWriter writer)
		{
			writer.Write<ushort>(Frame);
			writer.Write<ushort>(Trigger);
		}
	}

	public sealed class KeydataOSE(QuatOSE rotation, Vector3<short> position, ushort frame):IWritable
	{
		public readonly QuatOSE Rotation = rotation;
		public readonly Vector3<short> Position =position;
		public readonly ushort Frame = frame;

		public void Write(WadWriter writer)
		{
			writer.Write<QuatOSE>(Rotation);
			writer.Write<Vector3<short>>(Position);
			writer.Write<ushort>(Frame);
		}
	}

	public sealed class AnimationOSE:IWritableArrayMultipass
	{
		//public int TriggerCount;
		public readonly IReadOnlyList<AnimTriggerOSE> Triggers;

		public readonly int FrameCount;

		public readonly IReadOnlyList<Vector3I> PositionDeltas;
		
		//public int MorphCount;
		public readonly IReadOnlyList<IReadOnlyList<short>> Morphs;//First value should be SVECTOR, the rest are Int16s

		//public int BoneCount;
		public readonly IReadOnlyList<IReadOnlyList<Matrix4x4F>> Bones;

		#region KEYFRAME_STUFF
		//public int KeyframeCount;
		public readonly IReadOnlyList<IReadOnlyList<KeydataOSE>> Keyframes;
		#endregion

		public AnimationOSE
		(
			IReadOnlyList<AnimTriggerOSE> triggers,
			int frameCount,
			IReadOnlyList<Vector3I> positionDeltas,
			IReadOnlyList<IReadOnlyList<short>> morphs,
			IReadOnlyList<IReadOnlyList<Matrix4x4F>> bones,
			IReadOnlyList<IReadOnlyList<KeydataOSE>> keyframes
		)
		{
			Triggers = triggers;
			FrameCount = frameCount;
			PositionDeltas = positionDeltas;
			Morphs = morphs;
			Bones = bones;
			Keyframes = keyframes;
		}

		public void WriteStruct(WadWriter writer)
		{
			writer.Write<int>(Triggers.Count);
			writer.Write<uint>(0);//Triggers placeholder
			writer.Write(FrameCount);
			if(PositionDeltas != null && PositionDeltas.Count>0)
			{
				writer.Write<uint>(0);//PositionDeltas placeholder
			}
			else
			{
				writer.Write<uint>(1);//Indicates PositionDeltas is empty
			}
			writer.Write<int>(Morphs.Count);
			writer.Write<uint>(0);//Morphs placeholder
			writer.Write<int>(Bones.Count);
			writer.Write<uint>(0);//Bones placeholder

			if(writer.WriteVersion.KEYFRAME_STUFF)
			{
				writer.Write(Keyframes.Count);

				writer.Write<uint>(0);//Keydata placeholder
				writer.Write<uint>(0);//CacheEntry placeholder
				writer.Write<uint>(0);//Cache status.
			}
			else
			{
				if(Keyframes.Count!=0)
				{
					//TODO: Convert keydata if needed
					throw new NotImplementedException("Converting keyframes is currently not supported");
				}
			}
		}

		public void WriteData(WadWriter writer)
		{
			writer.WriteArray(Triggers);
			if(PositionDeltas != null && PositionDeltas.Count>0)
			{
				writer.WriteArray(PositionDeltas);
			}
			if(Morphs.Count != 0)
			{
				//Morphs memory space
				writer.WriteEmptyArray<short>(FrameCount);
			}
			//Bones memory space
			writer.WriteEmptyArray<uint>(FrameCount);

			if(writer.WriteVersion.KEYFRAME_STUFF)
			{
				//throw new NotImplementedException("OSE does not currently support keyframes");
				writer.WriteEmptyArray<uint>(Keyframes.Count);
			}
			else if(Keyframes.Count!=0)
			{
				//TODO: Convert keydata if needed
				throw new NotImplementedException("Converting keyframes is currently not supported");
			}

			for(int frameIndex=0; frameIndex<FrameCount; frameIndex++)
			{
				if(Morphs.Count != 0)
				{
					//First value is a list of Vector4<short>, the rest are lists of Int16
					writer.WriteArray<short>(Morphs[frameIndex]);
					
					//Not for the first frame
					if(frameIndex!=0 && (Morphs.Count & 1) != 0)
					{
						writer.Write<short>(0);//Padding?
					}
				}
				if(!writer.WriteVersion.KEYFRAME_STUFF || Keyframes.Count==0)
				{
					writer.WriteArray(Bones[frameIndex]);
				}
			}

			if(writer.WriteVersion.KEYFRAME_STUFF)
			{
				for(int i=0; i<Keyframes.Count; i++)
				{
					writer.WriteArray(Keyframes[i]);
				}
			}
			else
			{
				//TODO: Convert Keydata
				throw new NotImplementedException();
			}
		}
	}
}