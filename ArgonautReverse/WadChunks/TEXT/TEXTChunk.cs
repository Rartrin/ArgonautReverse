using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.TEXT
{
	public sealed class TEXTChunkInfo:BaseWADChunkInfo
	{
		public static readonly TEXTChunkInfo Instance = new TEXTChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "";
		public override ChunkType ChunkType => ChunkType.ID_PC_TEXT;

		public override BaseWadChunk Parse(WadReader reader)
		{
			int paletteCount = reader.Read<int>();
			var palettes = reader.ReadArray<BrTexturePalette>(paletteCount);

			int textureCount = reader.Read<int>();
			var textures = reader.ReadArray<TextureStruct>(textureCount);

			int spriteCount = reader.Read<int>();
			var sprites = reader.ReadArray<SpriteStruct>(spriteCount);

			int effectCount = reader.Read<int>();
			var effects = reader.ReadArray<Effect>(effectCount);

			reader.AssertEndOfChunk(ChunkType);
			return new TEXTChunk(this, palettes, textures, sprites, effects, reader.GetAllWadData());
		}
	}
	public sealed class TEXTChunk:BaseWadChunk
	{
		public IReadOnlyList<BrTexturePalette> Palettes{get;}
		public IReadOnlyList<TextureStruct> Textures{get;}
		public IReadOnlyList<SpriteStruct> Sprites{get;}
		public IReadOnlyList<Effect> Effects{get;}

		public TEXTChunk(BaseWADChunkInfo info, IReadOnlyList<BrTexturePalette> palette, IReadOnlyList<TextureStruct> textures, IReadOnlyList<SpriteStruct> sprites, IReadOnlyList<Effect> effects, byte[] data = null) : base(info, data)
		{
			Palettes = palette;
			Textures = textures;
			Sprites = sprites;
			Effects = effects;
		}
	}
}
