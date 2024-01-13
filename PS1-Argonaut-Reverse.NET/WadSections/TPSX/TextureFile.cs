using System.Drawing;
using System.Drawing.Imaging;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadSections.TPSX
{
	public sealed class TextureFile:BaseDataClass
	{
		public const int image_header_size = 4;
		public const int rle_size = 2;
		public static readonly (int Width,int Height) image_dimensions = (1024, 1024);
		public static readonly int image_bytes_size = image_dimensions.Width * image_dimensions.Height / 2;//Floor division

		public readonly int n_rows;
		public readonly byte[] textures_data;
		public readonly bool legacy_alpha;
		public readonly bool has_alpha;
		public IReadOnlyList<TextureData> Textures{get;}

		private TextureFile(int n_rows, byte[] textures_data, bool legacy_alpha, IReadOnlyList<TextureData>textures)
		{
			this.Textures = textures;
			this.n_rows = n_rows;
			this.textures_data = textures_data;
			this.legacy_alpha = legacy_alpha;  //TODO Legacy alpha (Croc 2)
			this.has_alpha = !legacy_alpha;//TODO Remove. Patch that disables bugged Croc 2 textures transparency export
		}

		public int n_textures => Textures.Count;

		public static TextureFile parse(WadReader data_in, bool compressed16bit, bool hasMemoryCardIcons, int end)
		{
			var textures = new List<TextureData>();
			int n_textures = data_in.ReadInt32();
			int n_rows = data_in.ReadInt32();

			if(n_textures > 4000 || ((n_rows<0) || (4<n_rows)))
			{
				if(data_in.Configuration.IgnoreWarnings)
				{
					TexturesWarning.Warn(n_textures, n_rows);
				}
				else
				{
					throw new TexturesWarning(data_in.Position, n_textures, n_rows);
				}
			}

			// In Harry Potter, the last 16 textures are empty (full of 00 bytes)
			int n_stored_textures = n_textures;
			if(data_in.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion==HARRY_POTTER_2_PS1.WadVersion)
			{
				n_stored_textures = n_textures - 16;
			}
			for(int texture_id=0; texture_id<n_stored_textures; texture_id++)
			{
				textures.Add(TextureData.parse(data_in));
			}
			if(data_in.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || data_in.ReadVersion==HARRY_POTTER_2_PS1.WadVersion)
			{
				data_in.Position += 192; // 16 textures x 12 bytes
			}
			var n_idk_yet_1 = data_in.ReadInt32();
			var number_effects = data_in.ReadInt32(); // Name found in the debug symbols
			data_in.Position += n_idk_yet_1 * image_header_size;//image_header_size is just sizeof(int)

			//TODO: Memory Card Icons
			if(hasMemoryCardIcons)
			{
				data_in.Position += 15360;
				for(int i=0; i<5; i++)
				{
					var mcPalette = data_in.ReadUInt32Array(128);
					var mcBitmapData = data_in.ReadUInt32Array(16*40);
				}
			}
			byte[] textures_data;
			if(compressed16bit)
			{
				var raw_textures = new byte[image_bytes_size];
				var raw_texturesStream = new MemoryStream(raw_textures);
				while(data_in.Position < end)
				{
					var run = data_in.ReadInt32();
					if(run < 0)
					{
						var element = data_in.ReadUInt16();//rle_size
						for(int count = Math.Abs(run); count>0; count--)
						{
							raw_texturesStream.WriteByte((byte)element);
							raw_texturesStream.WriteByte((byte)(element>>8));
						}
					}
					else if(run > 0)
					{
						raw_texturesStream.Write(data_in.ReadBytes(rle_size * run));
					}
					else
					{
						throw new ZeroRunLengthError(data_in.Position);
					}
				}
				raw_texturesStream.Position = 0;
				textures_data = raw_texturesStream.ToArray();//.read();
				raw_texturesStream.Close();
				if (data_in.ReadVersion == CROC_2_DEMO_PS1.WadVersion)  // Patch for Croc 2 Demo (non-dummy) last end offset error
				{
					data_in.Position -= 2;
				}
			}
			else
			{
				var image_size = n_rows * (image_bytes_size / 4);//Floor division
				var padding_size = image_bytes_size - image_size;
				textures_data = new byte[image_size + padding_size];
				data_in.ReadArray(textures_data.AsSpan(0, image_size));
			}
			bool legacy_alpha = (data_in.ReadVersion==CROC_2_DEMO_PS1.WadVersion) || (data_in.DatVersion==CROC_2_DEMO_PS1_DUMMY.DatVersion);
			return new TextureFile(n_rows, textures_data, legacy_alpha, textures);
		}

		/// <summary>Draws a complete colored texture image (composed of multiple single textures).</summary>
		public unsafe Bitmap to_colorized_texture()
		{
			var res = new Bitmap(image_dimensions.Width, image_dimensions.Height,  PixelFormat.Format32bppArgb);
			
			fixed(byte* textures_data0 = this.textures_data)
			{
				var textures_data_4bit = Utils.parse_4bits_paletted(this.textures_data);
				Bitmap im_4bits_paletted;
				fixed(byte* textures_data_4bit0 = textures_data_4bit)
				{
					im_4bits_paletted = new Bitmap(image_dimensions.Width, image_dimensions.Height, image_dimensions.Width/2, PixelFormat.Format4bppIndexed, (IntPtr)textures_data_4bit0);
				}
				Bitmap im_8bits_paletted = new Bitmap(image_dimensions.Width, image_dimensions.Height, image_dimensions.Width / 2, PixelFormat.Format8bppIndexed, (IntPtr)textures_data0);
				Bitmap im_high_color = new Bitmap(image_dimensions.Width, image_dimensions.Height, image_dimensions.Width / 4 /* * (has_alpha?4:3)*/, PixelFormat.Format32bppArgb, (IntPtr)textures_data0);

				var resGraphics = Graphics.FromImage(res);

				for(int t=0; t<this.Textures.Count; t++)
				{
					var texture = this.Textures[t];

					var box = texture.input_box;
					if(box._0 == box._2 || box._1 == box._3)
					{
						Console.WriteLine($"Invalid texture dimentions {box._0},{box._1},{box._2},{box._3}");
						continue;
					}

					var rect = Rectangle.FromLTRB(box._0, box._1, box._2, box._3);

					Bitmap texture_image;
					if((texture.flags&TextureFlags.IS_NOT_PALETTED)==0)
					{
						int paletteSize;
						if((texture.flags&TextureFlags.HAS_256_COLORS_PALETTE)!=0)  // 256-colors paletted
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
						
						var paletteColors = Utils.parse_palette
						(
							this.textures_data,
							paletteSize,
							this.has_alpha,
							this.legacy_alpha,
							texture.palette_start ?? 0
						);

						for(int p=0; p<palette.Entries.Length; p++)
						{
							palette.Entries[p] = paletteColors[p];
						}
						texture_image.Palette = palette;
					}
					else// True color (no palette)
					{
						texture_image = im_high_color.Clone(rect, PixelFormat.Format32bppArgb);
					}
					var drawPos = texture.output_top_left_corner;
					resGraphics.DrawImageUnscaled(texture_image, drawPos.X, drawPos.Y);
				}
			}
			return res;
		}
	}
}