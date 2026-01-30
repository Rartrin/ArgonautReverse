using ArgonautReverse.IO;
using ArgonautReverse.Universal;

namespace ArgonautReverse.PC
{
	public enum TextureFlagsPC:int
	{
		TEXTURE_FLAG_NONE = 0,
		TEXTURE_FLAG_80 = 0x80,//Compressed
	}

	public sealed class BrTexturePalettePC:IReadable<BrTexturePalettePC>,IWritable
	{
		public int renderArray3Index;
		public int count1;
		//public PALETTEENTRY[] ddPaletteArray;
		public byte bField0;
		public byte bField1;
		public byte bField2;
		public byte bField3;
		//public IDirectDrawPalette* ddPalette4;
		public ColorABGR555[] array5;
		public byte[] array6;

		public static BrTexturePalettePC Parse(WadReader reader)
		{
			var palette = new BrTexturePalettePC();
			palette.renderArray3Index = reader.Read<int>();

			var count1 = reader.Read<int>();

			palette.array5 = reader.ReadArray<ColorABGR555>(count1);
			palette.array6 = reader.ReadArray<byte>(count1);

			return palette;
		}

		public void Write(WadWriter writer)
		{
			writer.Write<int>(renderArray3Index);
			writer.Write<int>(count1);
			writer.WriteArray(array5);
			writer.WriteArray(array6);
		}
	}

	public sealed class UnknownRenderStruct3PC
	{
		//public DDPIXELFORMAT* textureFormat;
		//public bool (BR_CALLBACK* BltFunc)(void*, int, ColorRGB555*, int, int, int, int, int, unsigned int, bool, DDPIXELFORMAT*);
		//public bool (BR_CALLBACK* PaletteBltFunc)(void*, int, byte*, int, int, int, int, int, int, int, ColorRGB555*, int, bool, DDPIXELFORMAT*);
		//public void (BR_CALLBACK* CreatePaletteFunc)(PALETTEENTRY*, ColorRGB555*, byte*, int, bool);
		public bool bField0;
		public bool bField1;
		public bool paletteAlpha;
		public bool bField3;
		public bool bField4;
		public int field5;
		public int field6;
	}

	public sealed class TextureStructPC:IReadable<TextureStructPC>,IWritable
	{
		public TextureFlagsPC Flags;
		public int Width;
		public int Height;
		//public IDirectDrawSurface4* ddSurface;
		//public IDirect3DTexture2* ddTexture;
		//public BrTexturePalette brPalette;

		private ushort[]? compressedPixels;
		public ushort[] pixels;

		//public object obj7;

		private TextureStructPC(TextureFlagsPC flags, int width, int height, ushort[]? compressedPixels, ushort[] pixels)
		{
			this.Flags = flags;
			Width = width;
			Height = height;
			this.compressedPixels = compressedPixels;
			this.pixels = pixels;
		}

		public static TextureStructPC Parse(WadReader reader)
		{
			var flags = (TextureFlagsPC)reader.Read<int>();
			var width = reader.Read<int>();
			var height = reader.Read<int>();
			int pixelCount = width * height;
			ushort[] pixels;
			ushort[]? compressedPixels = null;
			if(flags != 0)
			{
				if((flags & TextureFlagsPC.TEXTURE_FLAG_80) != 0)
				{
					pixels = new ushort[pixelCount];

					var srcSize = reader.Read<int>();
					if(srcSize < 0 || srcSize % sizeof(ushort) != 0)
					{
						throw new Exception();
					}
					compressedPixels = reader.ReadArray<ushort>(srcSize / sizeof(ushort));
					TexturePC.DecompressPixels(compressedPixels, pixels, width, height);
				}
				else
				{
					pixels = reader.ReadArray<ushort>(pixelCount);
				}
			}
			else
			{
				if(pixelCount < 0 || pixelCount % sizeof(ushort) != 0)
				{
					throw new Exception("This should the byte length of a ushort array");
				}
				pixels = reader.ReadArray<ushort>(pixelCount / sizeof(ushort));
			}

			return new(flags, width, height, compressedPixels, pixels);
		}

		public void Write(WadWriter writer)
		{
			writer.Write((int)Flags);
			writer.Write<int>(Width);
			writer.Write<int>(Height);
			if(Flags != 0)
			{
				if((Flags & TextureFlagsPC.TEXTURE_FLAG_80) != 0)
				{
					writer.Write<int>(sizeof(ushort) * compressedPixels!.Length);
					writer.WriteArray<ushort>(compressedPixels);
				}
				else
				{
					writer.WriteArray<ushort>(pixels);
				}
			}
			else
			{
				writer.WriteArray<ushort>(pixels);
			}
		}
	}

	public static class TexturePC
	{
		public static void DecompressPixels(ushort[] src, ushort[] dst, int width, int height)
		{
			int srcIndex = 0;
			int dstIndex = 0;
			int lastIndex = height * width - 1;
			while(dstIndex < lastIndex)
			{
				//This must be signed
				short pixelData = (short)src[srcIndex++];
				if(pixelData >= 0)
				{
					//Copy some number of pixels
					short copyCount = pixelData;
					Array.Copy(src, srcIndex, dst, dstIndex, copyCount);
					dstIndex += copyCount;
					srcIndex += copyCount;
				}
				else
				{
					//Repeat one pixel multiple times
					var repeatCount = (short)-pixelData;
					var repeatPixel = src[srcIndex++];
					for(int i = 0; i < repeatCount; i++)
					{
						dst[dstIndex++] = repeatPixel;
					}
				}
			}
		}
	}
}
