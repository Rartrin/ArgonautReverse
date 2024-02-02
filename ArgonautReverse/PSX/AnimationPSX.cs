using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine;
using ArgonautReverse.PSX.LibGTE;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PSX
{
	public readonly struct QuatPSX:IReadable<QuatPSX>,IConvertibleOSE<QuatOSE>
	{
		public readonly short W,X,Y,Z;

		public QuatPSX(short w, short x, short y, short z)
		{
			W = w;
			X = x;
			Y = y;
			Z = z;
		}

		public static QuatPSX Parse(WadReader reader)
		{
			var w = reader.Read<short>();
			var x = reader.Read<short>();
			var y = reader.Read<short>();
			var z = reader.Read<short>();
			return new QuatPSX(w, x, y, z);
		}

		public QuatOSE ToOSE() => new QuatOSE(W, X, Y, Z);
	}

	public sealed class AnimTriggerPSX:IReadable<AnimTriggerPSX>, IConvertibleOSE<AnimTriggerOSE>
	{
		public readonly ushort Frame;
		public readonly ushort Trigger;

		public AnimTriggerPSX(ushort frame, ushort trigger)
		{
			Frame = frame;
			Trigger = trigger;
		}

		public static AnimTriggerPSX Parse(WadReader reader)
		{
			var frame = reader.Read<ushort>();
			var trigger = reader.Read<ushort>();
			return new AnimTriggerPSX(frame, trigger);
		}

		public AnimTriggerOSE ToOSE() => new AnimTriggerOSE(Frame, Trigger);
	}

	public sealed class KeydataPSX:IReadable<KeydataPSX>,IConvertibleOSE<KeydataOSE>
	{
		public readonly QuatPSX Rotation;
		public readonly Vector3<short> Position;
		public readonly ushort Frame;

		public KeydataPSX(QuatPSX rotation, Vector3<short> position, ushort frame)
		{
			Rotation = rotation;
			Position = position;
			Frame = frame;
		}

		public static KeydataPSX Parse(WadReader reader)
		{
			var rotation = reader.Read<QuatPSX>();
			var position = reader.Read<Vector3<short>>();
			var frame = reader.Read<ushort>();
			return new KeydataPSX(rotation, position, frame);
		}

		public KeydataOSE ToOSE() => new KeydataOSE
		(
			Rotation.ToOSE(),
			Position,
			Frame
		);
	}

	public sealed class AnimationPSX:IReadableArrayMultipass<AnimationPSX>,IConvertibleOSE<AnimationOSE>
	{
		public int TriggerCount;//n_flags
		public IReadOnlyList<AnimTriggerPSX> Triggers;
		public int FrameCount;//n_total_frames

		public bool Flag;//Unioned with PositionDeltas
		public IReadOnlyList<SVECTOR> PositionDeltas;//flags
		
		public int MorphCount;//n_inter_frames
		public IReadOnlyList<IReadOnlyList<short>> Morphs;
		public int BoneCount;//n_vertex_groups

		//Harry Potter games supported Quaternions for bones too.
		public IReadOnlyList<IReadOnlyList<SMATRIX>> Bones;
		//public IReadOnlyList<IReadOnlyList<BoneQuat>> BonesQuat;

		#region KEYFRAME_STUFF
		public int KeyframeCount;
		public IReadOnlyList<IReadOnlyList<KeydataPSX>> Keydata;
		//public IReadOnlyList<SMATRIX> CacheEntry;
		//public uint CacheStatus;
		#endregion

		public static AnimationPSX ParseStruct(WadReader reader)
		{
			var animation = new AnimationPSX();
			animation.TriggerCount = reader.Read<int>();
			reader.AssertRead<uint>(0);//Trigger placeholder
			animation.FrameCount = reader.Read<int>();
			animation.Flag = reader.Read<int>() switch
			{
				0 => false,
				1 => true,
				_ => throw new Exception()
			};
			animation.MorphCount = reader.Read<int>();
			reader.AssertRead<uint>(0);//Morphs placeholder
			animation.BoneCount = reader.Read<int>();
			reader.AssertRead<uint>(0);//Bones placeholder

			if(reader.ReadVersion.KEYFRAME_STUFF)
			{
				animation.KeyframeCount = reader.Read<int>();
				
				reader.AssertRead<uint>(0);//Keydata placeholder
				reader.AssertRead<uint>(0);//CacheEntry placeholder
				reader.AssertRead<uint>(0);//Cache status. Should start empty.
			}
			else
			{
				animation.KeyframeCount = 0;
			}

			return animation;
		}

		public static void ParseData(WadReader reader, AnimationPSX animation)
		{
			animation.Triggers = reader.ReadArray<AnimTriggerPSX>(animation.TriggerCount);
			if(!animation.Flag)
			{
				animation.PositionDeltas = reader.ReadArray(SVECTOR.ParseWithImportantPadding, animation.FrameCount);
			}
			short[][] morphs = null;
			if(animation.MorphCount != 0)
			{
				//Morphs array placeholder
				reader.AssertEmptyReadData<int>(animation.FrameCount);
				morphs = new short[animation.FrameCount][];
			}
			SMATRIX[][] bones = null;
			if(animation.FrameCount!=0)
			{
				//Bones array placeholder
				reader.AssertEmptyReadData<uint>(animation.FrameCount);
				bones = new SMATRIX[animation.FrameCount][];
			}
			KeydataPSX[][] keydata = null;
			if(reader.ReadVersion.KEYFRAME_STUFF && animation.KeyframeCount!=0)
			{
				//Keydata array placeholder
				reader.AssertEmptyReadData<uint>(animation.KeyframeCount);
				keydata = new KeydataPSX[animation.KeyframeCount][];
			}
			for(int frameIndex=0; frameIndex<animation.FrameCount; frameIndex++)
			{
				if(animation.MorphCount != 0 && animation.MorphCount != -1)
				{
					if(frameIndex == 0)
					{
						//First value is a Vector4<short>
						morphs[frameIndex] = reader.ReadArray<short>(4 * animation.MorphCount);
					}
					else
					{
						morphs[frameIndex] = reader.ReadArray<short>(animation.MorphCount);
						if((animation.MorphCount & 1) != 0)
						{
							reader.AssertRead<short>(0);//Padding?
						}
					}
				}
				if(!reader.ReadVersion.KEYFRAME_STUFF || animation.KeyframeCount==0)
				{
					bones[frameIndex] = reader.ReadArray<SMATRIX>(animation.BoneCount);
				}
				else
				{
					bones[frameIndex] = null;
				}
			}
			animation.Morphs = morphs;
			animation.Bones = bones;

			if(reader.ReadVersion.KEYFRAME_STUFF)
			{
				for(int i=0; i<animation.KeyframeCount; i++)
				{
					keydata[i] = reader.ReadArray<KeydataPSX>(animation.BoneCount);
				}
			}
			animation.Keydata = keydata;
		}

		public AnimationOSE ToOSE()
		{
			var oseTriggers = Triggers.ToOSE();
			var oseFrameCount = FrameCount;
			var osePositionDeltas = PositionDeltas.ToOSE<SVECTOR,Vector3I>();
			var oseMorphs = Morphs;
			var oseBones = Bones.ToOSE<SMATRIX,Matrix4x4F>();
			var oseKeyframes = Keydata.ToOSE<KeydataPSX,KeydataOSE>();
			return new AnimationOSE
			(
				oseTriggers,
				oseFrameCount,
				osePositionDeltas,
				oseMorphs,
				oseBones,
				oseKeyframes
			);
		}
	}
}