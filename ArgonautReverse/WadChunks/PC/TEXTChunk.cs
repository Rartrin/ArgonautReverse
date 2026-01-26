using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class TEXTChunkInfo:BaseWADChunkInfo<TEXTChunk>
	{
		public static readonly TEXTChunkInfo Instance = new TEXTChunkInfo();

		public override WadVersion[] SupportedWadVersions => Configuration.PC_PARSABLE_WADS;
		public override string ChunkDescription => "";
		public override ChunkType ChunkType => ChunkType.ID_PC_TEXT;

		public override TEXTChunk Parse(WadReader reader)
		{
			int paletteCount = reader.Read<int>();
			var palettes = reader.ReadArray<BrTexturePalettePC>(paletteCount);

			int textureCount = reader.Read<int>();
			var textures = reader.ReadArray<TextureStructPC>(textureCount);

			int spriteCount = reader.Read<int>();
			var sprites = reader.ReadArray<SpriteStructPC>(spriteCount);

			int effectCount = reader.Read<int>();
			var effects = reader.ReadArray<EffectPC>(effectCount);

			reader.AssertEndOfChunk(ChunkType);
			return new TEXTChunk(this, palettes, textures, sprites, effects, reader.GetAllWadData());
		}
	}
	public sealed class TEXTChunk(BaseWADChunkInfo info, IReadOnlyList<BrTexturePalettePC> palettes, IReadOnlyList<TextureStructPC> textures, IReadOnlyList<SpriteStructPC> sprites, IReadOnlyList<EffectPC> effects, byte[]? data = null):BaseWadChunk(info, data)
	{
		public readonly IReadOnlyList<BrTexturePalettePC> Palettes = palettes;
		public readonly IReadOnlyList<TextureStructPC> Textures = textures;
		public readonly IReadOnlyList<SpriteStructPC> Sprites = sprites;
		public readonly IReadOnlyList<EffectPC> Effects = effects;

		protected override void WriteData(WadWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}