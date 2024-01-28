using System.Runtime.InteropServices;
using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
    public enum SpriteFlagsPC : ushort
    {
        None = 0,
        //Represents a color instead of a texture
        HasColor = 0x1,
        HasAlpha = 0x2,
        _20 = 0x20,
    }
    public sealed class SpriteStructPC : IReadable<SpriteStructPC>
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct SourceValue//union
        {
            [FieldOffset(0)] public float F32;
            [FieldOffset(0)] public byte U8;
        }

        public SpriteFlagsPC flags;
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

        public static SpriteStructPC Parse(WadReader reader)
        {
            var sprite = new SpriteStructPC();
            sprite.flags = (SpriteFlagsPC)reader.Read<ushort>();
            sprite.sourceTexture = reader.Read<sbyte>();
            sprite.paletteIndex = reader.Read<sbyte>();
            sprite.sourceMinX.U8 = reader.Read<byte>();
            sprite.sourceMaxX.U8 = reader.Read<byte>();
            sprite.sourceMinY.U8 = reader.Read<byte>();
            sprite.sourceMaxY.U8 = reader.Read<byte>();
            return sprite;
        }
    }

    public enum EffectTypePC : byte
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

    public class EffectPC : IReadable<EffectPC>
    {
        public ushort spriteIndex;
        public EffectTypePC Type;
        public byte bField1;
        public byte bSomeMaxIndex2;
        public byte frameCount0;
        public byte currentFrame;
        public byte FirstFrameIndex;
        public byte frameCount1;
        public byte bSomeCount7;
        public byte bField8;
        public byte bSomeCount9;
        public SpriteStructPC[] frames;

        public static EffectPC Parse(WadReader reader)
        {
            var effect = new EffectPC();
            effect.spriteIndex = reader.Read<ushort>();
            effect.Type = (EffectTypePC)reader.Read<byte>();
            switch (effect.Type)
            {
                case EffectTypePC.MoveDown:
                case EffectTypePC.MoveUp:
                    effect.bSomeMaxIndex2 = reader.Read<byte>();
                    effect.frameCount0 = reader.Read<byte>();
                    effect.FirstFrameIndex = reader.Read<byte>();
                    effect.currentFrame = 0;
                    break;
                case EffectTypePC.RangeCycle:
                case EffectTypePC.RangeCycle256:
                    effect.bSomeMaxIndex2 = reader.Read<byte>();
                    effect.frameCount0 = reader.Read<byte>();
                    effect.currentFrame = reader.Read<byte>();
                    effect.FirstFrameIndex = reader.Read<byte>();
                    effect.frameCount1 = reader.Read<byte>();
                    effect.bSomeCount7 = reader.Read<byte>();
                    effect.bSomeCount9 = 0;
                    break;
                case EffectTypePC.Anim2:
                    effect.bSomeMaxIndex2 = reader.Read<byte>();
                    effect.frameCount0 = reader.Read<byte>();
                    effect.frameCount1 = reader.Read<byte>();
                    effect.frames = new SpriteStructPC[effect.frameCount0];
                    for (int j = 0; j < effect.frameCount0; j++)
                    {
                        effect.frames[j] = new SpriteStructPC
                        {
                            flags = (SpriteFlagsPC)reader.Read<ushort>()
                        };
                    }
                    effect.FirstFrameIndex = 0;
                    effect.currentFrame = 0;
                    effect.bSomeCount7 = 0;
                    break;
                case EffectTypePC.MultiMoveUp:
                case EffectTypePC.MultiMoveDown:
                    effect.bSomeMaxIndex2 = reader.Read<byte>();
                    effect.frameCount0 = reader.Read<byte>();
                    effect.FirstFrameIndex = reader.Read<byte>();
                    effect.frameCount1 = reader.Read<byte>();
                    effect.frames = new SpriteStructPC[effect.frameCount1];
                    for (int j = 0; j < effect.frameCount1; j++)
                    {
                        effect.frames[j] = new SpriteStructPC
                        {
                            flags = (SpriteFlagsPC)reader.Read<ushort>()
                        };
                    }
                    effect.currentFrame = 0;
                    effect.bSomeCount7 = 0;
                    break;
                default: throw new NotImplementedException("Missing effect type: " + effect.Type);
            }
            return effect;
        }
    }
}
