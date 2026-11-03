using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine.Chunks;
using ArgonautReverse.PSX;

namespace ArgonautReverse.WadChunks.PSX
{
	public enum TextureFlag:int
	{
		None = 0,
		CompressedTPage = 1 << 0,
		Compressed16Bit = 1 << 1,
		HasLevelName = 1 << 2,
		HasMemoryCardIcons = 1 << 3,
		HasLongLevelName = 1 << 4,//Includes translated names
	}
	public sealed class TPSXChunkInfo:BaseWADChunkInfo<TPSXChunk>
	{
		public static readonly TPSXChunkInfo Instance = new TPSXChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PSX_TEXT;
		public override WadVersion[] SupportedWadVersions => Configuration.ParsableWadsPSX;
		public override string ChunkDescription => "textures";

		public override TPSXChunk Parse(WadReader reader)
		{
			TextureFlag chunkFlags;
			if(reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				chunkFlags = TextureFlag.None;
			}
			else
			{
				chunkFlags = (TextureFlag)reader.Read<int>();
			}
			bool hasLongLevelName = (chunkFlags & TextureFlag.HasLongLevelName) != 0;
			bool hasMemoryCardIcons = (chunkFlags & TextureFlag.HasMemoryCardIcons) != 0;
			bool hasLevelName = (chunkFlags & TextureFlag.HasLevelName) != 0;
			bool compressed16bit = (chunkFlags & TextureFlag.Compressed16Bit) != 0;
			
			string[]? titles = null;
			if(hasLongLevelName)
			{
				var titleCount = reader.Read<int>();
				titles = new string[titleCount];
				for(int i = 0; i < titleCount; i++)
				{
					titles[i] = reader.ReadString(48).Trim('\0');
				}
			}
			else if(hasLevelName)
			{
				titles = [reader.ReadString(32).Trim('\0')];
			}

			int? spriteOffset = null;
			FontPSX[]? fontLookup = null;
			if(titles != null)
			{
				//TODO: Why are these also in DPSX?
				spriteOffset = reader.Read<int>();

				fontLookup = reader.ReadArray<FontPSX>(256);
			}
			int textureCount = reader.Read<int>();
			int pageCount = reader.Read<int>();
			if(pageCount < 0 || 4 < pageCount)
			{
				throw new Exception($"Invalid page count {pageCount}. Must be between 0 and 4 inclusively.");
			}

			//In Harry Potter, the last 16 textures are empty (full of 00 bytes).
			var textureHeaders = new TextureHeaderPSX[textureCount];
			for(int i = 0; i < textureCount; i++)
			{
				textureHeaders[i] = reader.Read<TextureHeaderPSX>();
			}

			int effectIntDataSize = reader.Read<int>();//Size in int32s.
			int effectCount = reader.Read<int>();
			var effectData = reader.ReadArray<uint>(effectIntDataSize);

			SimpleImagePSX[]? memoryCardIcons = null;
			if(hasMemoryCardIcons)
			{
				memoryCardIcons = reader.ReadArray<SimpleImagePSX>(5);
			}

			int totalAvailablePixels = pageCount * (TextureFilePSX.PageWidth * TextureFilePSX.PageHeight);
			var textureData = new ushort[totalAvailablePixels];
			if(compressed16bit)
			{
				int index = 0;
				int lastIndex = totalAvailablePixels - 1;
				while(index < lastIndex)
				{
					short run = reader.Read<short>();
					if(run < 0)
					{
						//Repeat
						var element = reader.Read<ushort>();
						for(int count = -run; count > 0; count--)
						{
							textureData[index++] = element;
						}
					}
					else if(run > 0) 
					{
						//Copy
						reader.ReadArray<ushort>(textureData.AsSpan(index, run));
						index += run;
					}
					else
					{
						throw new Exception("Run must not be zero.");
					}
				}
				if(reader.ReadVersion == CROC_2_DEMO_PS1.WadVersion)// Patch for Croc 2 Demo (non-dummy) last end offset error
				{
					reader.Position -= 2;
				}
			}
			else
			{
				reader.ReadArray<ushort>(textureData);
			}

			var tpsxChunk = new TPSXChunk(reader.GetAllWadData())
			{
				Titles = titles,
				SpriteOffset = spriteOffset,
				FontLookup = fontLookup,
				PageCount = pageCount,
				TextureHeaders = textureHeaders,
				EffectCount = effectCount,
				EffectData = effectData,
				MemoryCardIcons = memoryCardIcons,
				TextureData = textureData,
			};
			bool legacyAlpha = reader.ReadVersion == CROC_2_DEMO_PS1.WadVersion || reader.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion;
			tpsxChunk.TextureFile = TextureFilePSX.parse(tpsxChunk, compressed16bit, legacyAlpha);
			reader.AssertEndOfChunk(ChunkType, warn:true);//TODO: Incorrect data size on HP games?
			return tpsxChunk;
		}
	}

	public sealed class TPSXChunk(byte[]? fallback_data = null):BaseWadChunk(TPSXChunkInfo.Instance, fallback_data),IConvertibleToOSE<IReadOnlyList<ChunkOSE>>
	{
		public required IReadOnlyList<string>? Titles{get;init;}
		public required int? SpriteOffset{get;init;}
		public required IReadOnlyList<FontPSX>? FontLookup{get;init;}
		public required int PageCount{get;init;}
		public required IReadOnlyList<TextureHeaderPSX> TextureHeaders{get;init;}
		public required int EffectCount{get;init;}
		public required uint[] EffectData{get;init;}
		public required SimpleImagePSX[]? MemoryCardIcons{get;init;}
		public required ushort[] TextureData{get;init;}

		//Data for exporting
		public TextureFilePSX TextureFile;

		protected override void WriteData(ChunkWriter writer)
		{
			if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion){throw new NotImplementedException("DUMMY wad writing not supported.");}

			//TODO: Add argument for compressing textures.
			bool CompressTextures = false;

			var chunkFlags = TextureFlag.None;
			if(CompressTextures)
			{
				chunkFlags |= TextureFlag.Compressed16Bit;
			}
			bool hasLongLevelName = false;
			bool hasLevelName = false;
			if(Titles != null)
			{
				hasLevelName = true;
				chunkFlags |= TextureFlag.HasLevelName;
				if(Titles.Count > 1 || Titles[0].Length > 32)
				{
					hasLongLevelName = true;
					chunkFlags |= TextureFlag.HasLongLevelName;
				}
			}
			if(MemoryCardIcons != null)
			{
				chunkFlags |= TextureFlag.HasMemoryCardIcons;
			}
			writer.Write<int>((int)chunkFlags);
			
			if(hasLongLevelName)
			{
				writer.Write<int>(Titles!.Count);
				for(int i = 0; i < Titles.Count; i++)
				{
					writer.WriteCStringPadded(48, Titles[i]);
				}
			}
			else if(hasLevelName)
			{
				writer.WriteCStringPadded(32, Titles![0]);
			}

			if(Titles != null)
			{
				writer.Write<int>(SpriteOffset!.Value);
				writer.WriteSizedArray<FontPSX>(256, FontLookup!);
			}
			writer.Write<int>(TextureHeaders.Count);
			writer.Write<int>(PageCount);
			for(int i = 0; i < TextureHeaders.Count; i++)
			{
				writer.Write<TextureHeaderPSX>(TextureHeaders[i]);
			}

			writer.Write<int>(EffectData.Length);//Size in int32s.
			writer.Write<int>(EffectCount);
			writer.WriteArray<uint>(EffectData);

			if(MemoryCardIcons != null)
			{
				writer.WriteSizedArray<SimpleImagePSX>(5, MemoryCardIcons!);
			}

			if(CompressTextures)
			{
				throw new NotImplementedException();
			}
			else
			{
				writer.WriteArray<ushort>(TextureData);
			}
		}

		public override IReadOnlyList<ChunkOSE> ToOSE()
		{
			//TODO: Can be null in the dummy wad.
			var fontChunk = new FontChunkOSE(FontLookup.ToOSE());

			//TODO: Textures
			//TODO: Titles?

			return
			[
				fontChunk,
			];
		}
	}
}