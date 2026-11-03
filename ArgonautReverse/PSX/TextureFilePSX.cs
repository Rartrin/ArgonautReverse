using System.Drawing;
using System.Drawing.Imaging;

using ArgonautReverse.WadChunks.PSX;

namespace ArgonautReverse.PSX
{
	public sealed class TextureFilePSX
	{
		public const int PageWidth = 256;
		public const int PageHeight = 256;
		public static readonly (int Width, int Height) image_dimensions = (1024, 1024);
		public static readonly int image_bytes_size = image_dimensions.Width * image_dimensions.Height / 2;//Floor division

		public readonly int n_rows;
		public readonly byte[] textures_data;
		public readonly bool legacy_alpha;
		public readonly bool has_alpha;
		public IReadOnlyList<TextureDataPSX> Textures{get;}

		private TextureFilePSX(int n_rows, byte[] textures_data, bool legacy_alpha, TextureDataPSX[] textures)
		{
			this.n_rows = n_rows;
			this.textures_data = textures_data;
			this.legacy_alpha = legacy_alpha;  //TODO Legacy alpha (Croc 2)
			has_alpha = !legacy_alpha;//TODO Remove. Patch that disables bugged Croc 2 textures transparency export
			Textures = textures;
		}

		public unsafe static TextureFilePSX parse(TPSXChunk chunk, bool compressed16bit, bool legacyAlpha)
		{
			var textures = new TextureDataPSX[chunk.TextureHeaders.Count];
			for(int texture_id = 0; texture_id < chunk.TextureHeaders.Count; texture_id++)
			{
				textures[texture_id] = TextureDataPSX.Parse(chunk.TextureHeaders[texture_id]);
			}

			//Pad to 4 pages
			var textureData = new byte[sizeof(ushort) * (4 * PageWidth * PageHeight)];
			fixed(byte* textureData0 = textureData)
			{
				chunk.TextureData.CopyTo(new Span<ushort>(textureData0, chunk.TextureData.Length));
			}

			return new TextureFilePSX(chunk.PageCount, textureData, legacyAlpha, textures);
		}

		/// <summary>Draws a complete colored texture image (composed of multiple single textures).</summary>
		public unsafe Bitmap ToColorizedTexture()
		{
			var res = new Bitmap(image_dimensions.Width, image_dimensions.Height, PixelFormat.Format32bppArgb);

			var textures_data_4bit = Utils.ParsePixelDataIndex4Bit(textures_data);
			Bitmap im_4bits_paletted;
			fixed(byte* textures_data_4bit0 = textures_data_4bit)
			{
				im_4bits_paletted = new Bitmap(image_dimensions.Width, image_dimensions.Height, image_dimensions.Width / 2, PixelFormat.Format4bppIndexed, (nint)textures_data_4bit0);
			}
			Bitmap im_8bits_paletted;
			Bitmap im_high_color;
			fixed(byte* textures_data0 = textures_data)
			{
				im_8bits_paletted = new Bitmap(image_dimensions.Width, image_dimensions.Height, image_dimensions.Width / 2, PixelFormat.Format8bppIndexed, (nint)textures_data0);
				im_high_color = new Bitmap(image_dimensions.Width, image_dimensions.Height, image_dimensions.Width / 4 /* * (has_alpha?4:3)*/, PixelFormat.Format32bppArgb, (nint)textures_data0);
			}

			var resGraphics = Graphics.FromImage(res);

			for(int t = 0; t < Textures.Count; t++)
			{
				var texture = Textures[t];

				var box = texture.GetInputBox();
				if(box.TL == box.BL || box.TR == box.BR)
				{
					Console.WriteLine($"Invalid texture dimentions {box.TL},{box.TR},{box.BL},{box.BR}");
					continue;
				}

				var rect = Rectangle.FromLTRB(box.TL, box.TR, box.BL, box.BR);

				Bitmap texture_image;
				if(texture.flags.HasNoPaletted)// True color (no palette)
				{
					texture_image = im_high_color.Clone(rect, PixelFormat.Format32bppArgb);
				}
				else
				{
					int paletteSize;
					if(texture.flags.Has256ColorPalette)  // 256-colors paletted
					{
						texture_image = im_8bits_paletted.Clone(rect, PixelFormat.Format8bppIndexed);
						paletteSize = 256;
					}
					else// 16-colors paletted
					{
						texture_image = im_4bits_paletted.Clone(rect, PixelFormat.Format4bppIndexed);
						paletteSize = 16;
					}
					var palette = texture_image.Palette;

					var paletteColors = Utils.ParsePalette
					(
						textures_data.AsSpan(texture.palette_start ?? 0, 2*paletteSize),
						has_alpha,
						legacy_alpha
					);

					for(int p = 0; p < palette.Entries.Length; p++)
					{
						palette.Entries[p] = paletteColors[p].ToSystem();
					}
					texture_image.Palette = palette;
				}
				var drawPos = texture.output_top_left_corner;
				resGraphics.DrawImageUnscaled(texture_image, drawPos.X, drawPos.Y);
			}
			return res;
		}

		public unsafe void SaveRawTextureData(string path)
		{
			var textureData4bit = Utils.ParsePixelDataIndex4Bit(textures_data);
			fixed(byte* texturesData0 = textureData4bit)
			{
				var ret = new Bitmap(image_dimensions.Width, image_dimensions.Height, image_dimensions.Width/2, PixelFormat.Format4bppIndexed, (nint)texturesData0);
				var palette = ret.Palette;
				for(int p = 0; p < palette.Entries.Length; p++)
				{
					var shade = (255*p) / palette.Entries.Length;
					palette.Entries[p] = Color.FromArgb(shade, shade, shade);
				}
				ret.Palette = palette;
				ret.Save(path, ImageFormat.Bmp);
			}
		}
	}
}