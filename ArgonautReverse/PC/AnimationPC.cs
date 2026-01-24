using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PC
{
	public readonly struct AnimTriggerPC:IReadable<AnimTriggerPC>
	{
		public readonly ushort Frame;
		public readonly ushort Trigger;

		public AnimTriggerPC(ushort wField0, ushort wField1)
		{
			this.Frame = wField0;
			this.Trigger = wField1;
		}

		public static AnimTriggerPC Parse(WadReader reader)
		{
			var wField0 = reader.Read<ushort>();
			var wField1 = reader.Read<ushort>();
			return new AnimTriggerPC(wField0, wField1);
		}
	}

	public readonly struct AnimationStruct1_PC:IReadable<AnimationStruct1_PC>
	{
		public readonly short wField0;
		public readonly short wField1;
		public readonly short wField2;
		public readonly short wField3;

		public AnimationStruct1_PC(short wField0, short wField1, short wField2, short wField3)
		{
			this.wField0 = wField0;
			this.wField1 = wField1;
			this.wField2 = wField2;
			this.wField3 = wField3;
		}

		public static AnimationStruct1_PC Parse(WadReader reader)
		{
			var wField0 = reader.Read<short>();
			var wField1 = reader.Read<short>();
			var wField2 = reader.Read<short>();
			var wField3 = reader.Read<short>();
			return new AnimationStruct1_PC(wField0, wField1, wField2, wField3);
		}
	}

	public sealed class AnimationStructPC:IReadableArrayMultipass<AnimationStructPC>
	{
		public int TriggerCount;
		public IReadOnlyList<AnimTriggerPC> Triggers;
		public int FrameCount;
		
		public bool Flag;//Unioned with PositionDeltas
		public IReadOnlyList<AnimationStruct1_PC>? PositionDeltas;

		public int MorphCount;
		public IReadOnlyList<IReadOnlyList<short>>? Morphs;
		public int BoneCount;
		public IReadOnlyList<IReadOnlyList<Matrix4x4F>>? Bones;

		public static AnimationStructPC ParseStruct(WadReader reader)
		{
			var animation = new AnimationStructPC();
			
			animation.TriggerCount = reader.Read<int>();
			reader.AssertRead<uint>(0);//Triggers placeholder
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

			return animation;
		}

		public static void ParseData(WadReader reader, AnimationStructPC animation)
		{
			animation.Triggers = reader.ReadArray<AnimTriggerPC>(animation.TriggerCount);
			if(!animation.Flag)
			{
				animation.PositionDeltas = reader.ReadArray<AnimationStruct1_PC>(animation.FrameCount);
			}
			short[][]? morphs = null;
			if(animation.MorphCount!=0 && animation.FrameCount!=0)
			{
				//Morphs array placeholder
				reader.AssertEmptyReadData<int>(animation.FrameCount);//Type would actually be a short*
				morphs = new short[animation.FrameCount][];
			}
			Matrix4x4F[][]? bones = null;
			if(animation.FrameCount!=0)
			{
				//Bones array placeholder
				reader.AssertEmptyReadData<int>(animation.FrameCount);//Type would actually be a Matrix4x4F*
				bones = new Matrix4x4F[animation.FrameCount][];
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
				if(animation.BoneCount>0)
				{
					bones[frameIndex] = reader.ReadArray<Matrix4x4F>(animation.BoneCount);
				}
			}
			animation.Morphs = morphs;
			animation.Bones = bones;
		}
	}
}
