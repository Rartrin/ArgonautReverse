using System.Runtime.InteropServices;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.TEXT
{
	public enum SpriteFlags:ushort
	{
		None = 0,
		//Represents a color instead of a texture
		HasColor = 0x1,
		HasAlpha = 0x2,
		_20 = 0x20,
	}
	public sealed class SpriteStruct:IReadable<SpriteStruct>
	{
		[StructLayout(LayoutKind.Explicit)]
		public struct SourceValue//union
		{
			[FieldOffset(0)]public float F32;
			[FieldOffset(0)]public byte U8;
		}

		public SpriteFlags flags;
		public sbyte sourceTexture;
		public sbyte paletteIndex;

		public SourceValue sourceMinX;
		public ref byte ColorR => ref sourceMinX.U8;
		public SourceValue sourceMaxX;
		public ref byte ColorG => ref sourceMaxX.U8;
		public SourceValue sourceMinY;
		public ref byte ColorB => ref sourceMinY.U8;
		public SourceValue sourceMaxY;
		public ref byte ColorAlpha => ref sourceMaxY.U8;

		public static SpriteStruct Parse(WadReader reader)
		{
			var sprite = new SpriteStruct();
			sprite.flags = (SpriteFlags)reader.Read<ushort>();
			sprite.sourceTexture = reader.Read<sbyte>();
			sprite.paletteIndex = reader.Read<sbyte>();
			sprite.sourceMinX.U8 = reader.Read<byte>();
			sprite.sourceMaxX.U8 = reader.Read<byte>();
			sprite.sourceMinY.U8 = reader.Read<byte>();
			sprite.sourceMaxY.U8 = reader.Read<byte>();
			return sprite;
		}
	}

	public enum EffectType:byte
	{
		NOP = 0,
		MoveDown = 1,
		MoveUp = 2,
		Anim = 5,
		AnimPrelit = 6,
		RangeCycle = 7,
		Anim2 = 8,
		RangeCycle256 = 9,
		MultiMoveUp = 10,
		MultiMoveDown = 11,
	}

	public class Effect:IReadable<Effect>
	{
		public ushort spriteIndex;
		public EffectType Type;
		public byte bField1;
		public byte bSomeMaxIndex2;
		public byte frameCount0;
		public byte currentFrame;
		public byte FirstFrameIndex;
		public byte frameCount1;
		public byte bSomeCount7;
		public byte bField8;
		public byte bSomeCount9;
		public SpriteStruct[] frames;

		public static Effect Parse(WadReader reader)
		{
			var effect = new Effect();
			effect.spriteIndex = reader.Read<ushort>();
			effect.Type = (EffectType)reader.Read<byte>();
			switch(effect.Type)
			{
				case EffectType.MoveDown:
				case EffectType.MoveUp:
					effect.bSomeMaxIndex2 = reader.Read<byte>();
					effect.frameCount0 = reader.Read<byte>();
					effect.FirstFrameIndex = reader.Read<byte>();
					effect.currentFrame = 0;
					break;
				case EffectType.RangeCycle:
				case EffectType.RangeCycle256:
					effect.bSomeMaxIndex2 = reader.Read<byte>();
					effect.frameCount0 = reader.Read<byte>();
					effect.currentFrame = reader.Read<byte>();
					effect.FirstFrameIndex = reader.Read<byte>();
					effect.frameCount1 = reader.Read<byte>();
					effect.bSomeCount7 = reader.Read<byte>();
					effect.bSomeCount9 = 0;
					break;
				case EffectType.Anim2:
					effect.bSomeMaxIndex2 = reader.Read<byte>();
					effect.frameCount0 = reader.Read<byte>();
					effect.frameCount1 = reader.Read<byte>();
					effect.frames = new SpriteStruct[effect.frameCount0];
					for(int j=0; j<effect.frameCount0; j++)
					{
						effect.frames[j] = new SpriteStruct
						{
							flags = (SpriteFlags)reader.Read<ushort>()
						};
					}
					effect.FirstFrameIndex = 0;
					effect.currentFrame = 0;
					effect.bSomeCount7 = 0;
					break;
				case EffectType.MultiMoveUp:
				case EffectType.MultiMoveDown:
					effect.bSomeMaxIndex2 = reader.Read<byte>();
					effect.frameCount0 = reader.Read<byte>();
					effect.FirstFrameIndex = reader.Read<byte>();
					effect.frameCount1 = reader.Read<byte>();
					effect.frames = new SpriteStruct[effect.frameCount1];
					for(int j=0; j<effect.frameCount1; j++)
					{
						effect.frames[j] = new SpriteStruct
						{
							flags = (SpriteFlags)reader.Read<ushort>()
						};
					}
					effect.currentFrame = 0;
					effect.bSomeCount7 = 0;
					break;
				default:throw new NotImplementedException("Missing effect type: "+effect.Type);
			}
			return effect;
		}
	}

}
