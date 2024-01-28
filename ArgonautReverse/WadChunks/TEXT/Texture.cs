using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.TEXT
{
	public enum TextureFlags : int
	{
		TEXTURE_FLAG_NONE = 0,
		TEXTURE_FLAG_80 = 0x80,
	}

	public sealed class BrTexturePalette:IReadable<BrTexturePalette>
	{
		public int renderArray3Index;
		public int count1;
		//public PALETTEENTRY[] ddPaletteArray;
		public byte bField0;
		public byte bField1;
		public byte bField2;
		public byte bField3;
		//public IDirectDrawPalette* ddPalette4;
		public ColorRGB555[] array5;
		public byte[] array6;

		public static BrTexturePalette Parse(WadReader reader)
		{
			var palette	= new BrTexturePalette();
			palette.renderArray3Index = reader.Read<int>();

			palette.count1 = reader.Read<int>();
				
			palette.array5 = reader.ReadArray<ColorRGB555>(palette.count1);
			palette.array6 = reader.ReadArray<byte>(palette.count1);

			return palette;
		}
	}

	public sealed class UnknownRenderStruct3
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

	public sealed class TextureStruct:IReadable<TextureStruct>
	{
		public TextureFlags flags;
		public int Width;
		public int Height;
		//public IDirectDrawSurface4* ddSurface;
		//public IDirect3DTexture2* ddTexture;
		//public BrTexturePalette brPalette;
		
		public ushort[] pixels;
		
		//public object obj7;

		public static TextureStruct Parse(WadReader reader)
		{
			var texture = new TextureStruct();;
			texture.flags = (TextureFlags)reader.Read<int>();
			texture.Width = reader.Read<int>();
			texture.Height = reader.Read<int>();
			int pixelCount = texture.Width * texture.Height;
			if(texture.flags != 0)
			{
				if((texture.flags & TextureFlags.TEXTURE_FLAG_80) != 0)
				{
					texture.pixels = new ushort[pixelCount];
						
					var srcSize = reader.Read<int>();
					if(srcSize<0 || srcSize%sizeof(ushort) != 0)
					{
						throw new Exception();
					}
					var src = reader.ReadArray<ushort>(srcSize/sizeof(ushort));
					Texture.DecompressPixels(src, texture.pixels, texture.Width, texture.Height);
				}
				else
				{
					texture.pixels = reader.ReadArray<ushort>(pixelCount);
				}
			}
			else
			{
				if(pixelCount<0 || pixelCount%sizeof(ushort) != 0)
				{
					throw new Exception("This should the byte length of a ushort array");
				}
				texture.pixels = reader.ReadArray<ushort>(pixelCount/sizeof(ushort));
			}

			return texture;
		}
	}

	public static class Texture
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
					var repeatCount = (short)(-pixelData);
					var repeatPixel = src[srcIndex++];
					for(int i=0; i<repeatCount; i++)
					{
						dst[dstIndex++] = repeatPixel;
					}
				}
			}
		}
	}
}
