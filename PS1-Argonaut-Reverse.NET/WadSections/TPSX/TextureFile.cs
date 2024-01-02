//import warnings
//from collections.abc import Iterable
//from io import BufferedIOBase, BytesIO, SEEK_CUR

//import numpy as np
//from PIL import Image

//from ps1_argonaut.BaseDataClasses import BaseDataClass
//from ps1_argonaut.configuration import Configuration, G
//from ps1_argonaut.errors_warnings import TexturesWarning, ZeroRunLengthError
//from ps1_argonaut.utils import parse_4bits_paletted, parse_high_color, parse_palette
//from ps1_argonaut.wad_sections.TPSX.TextureData import TextureData
//from ps1_argonaut.wad_sections.TPSX.TextureFlags import TextureFlags


using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArgonautReverse.WadSections.TPSX
{
	public sealed class TextureFile:IReadOnlyList<TextureData>, BaseDataClass
	{
		public const int image_header_size = 4;
		public const int rle_size = 2;
		public static readonly (int Width,int Height) image_dimensions = (1024, 1024);
		public static readonly int image_bytes_size = image_dimensions.Width * image_dimensions.Height / 2;//Floor division

		private readonly List<TextureData> list = new List<TextureData>();

		public readonly int n_rows;
		public readonly byte[] textures_data;
		public readonly bool legacy_alpha;
		public readonly bool has_alpha;
		public readonly List<TextureData> textures = new List<TextureData>();

		public int Count => list.Count;
		public TextureData this[int index] => list[index];
		public IEnumerator<TextureData> GetEnumerator() => list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

		public TextureFile(int n_rows, byte[] textures_data, bool legacy_alpha, IEnumerable<TextureData>textures = null)
		{
			if(textures != null)
			{
				list.AddRange(textures);
				this.textures.AddRange(textures);
			}
			this.n_rows = n_rows;
			this.textures_data = textures_data;
			this.legacy_alpha = legacy_alpha;  //TODO Legacy alpha (Croc 2)
			this.has_alpha = !legacy_alpha;//TODO Remove. Patch that disables bugged Croc 2 textures transparency export
		}

		//@property
		public int n_textures => list.Count;

		//@classmethod
		public static TextureFile parse(Parser data_in, Configuration conf/*, *args, **kwargs*/, bool has_legacy_textures, int end)//BufferedIOBase
		{
			//base.parse(data_in, conf);
			//bool has_legacy_textures = kwargs["has_legacy_textures"];
			//int end = kwargs["end"];
			bool rle =
			(
				conf.game == G.CROC_2_PS1
				|| conf.game == G.CROC_2_DEMO_PS1
				|| conf.game == G.HARRY_POTTER_1_PS1
				|| conf.game == G.HARRY_POTTER_2_PS1
			);

			var textures = new List<TextureData>();
			int n_textures = data_in.ReadInt32();
			int n_rows = data_in.ReadInt32();

			if(n_textures > 4000 || (0 > n_rows || n_rows > 4))
			{
				if(conf.ignore_warnings)
				{
					warnings.warn($"Too much textures ({n_textures}, or incorrect row count {n_rows}. It is most probably caused by an inaccuracy in my reverse engineering of the textures format.");
				}
				else
				{
					throw new TexturesWarning((int)data_in.Position, n_textures, n_rows);
				}
			}

			// In Harry Potter, the last 16 textures are empty (full of 00 bytes)
			int n_stored_textures = n_textures;
			if(conf.game == G.HARRY_POTTER_1_PS1 || conf.game==G.HARRY_POTTER_2_PS1)
			{
				n_stored_textures = n_textures - 16;
			}
			for(int texture_id=0; texture_id<n_stored_textures; texture_id++)
			{
				textures.Add(TextureData.parse(data_in, conf));
			}
			if(conf.game == G.HARRY_POTTER_1_PS1 || conf.game==G.HARRY_POTTER_2_PS1)
			{
				data_in.Position += 192; // 16 textures x 12 bytes
			}
			var n_idk_yet_1 = data_in.ReadInt32();
			var number_effects = data_in.ReadInt32(); // Name found in the debug symbols
			data_in.Position += n_idk_yet_1 * image_header_size;

			if(has_legacy_textures)// Patch for legacy textures, see Textures documentation
			{
				data_in.Position += 15360;
			}
			byte[] textures_data;
			if(rle)
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
						throw new ZeroRunLengthError((int)data_in.Position);
					}
				}
				raw_texturesStream.Position = 0;
				textures_data = raw_texturesStream.ToArray();//.read();
				raw_texturesStream.Close();
				if (conf.game == G.CROC_2_DEMO_PS1)  // Patch for Croc 2 Demo (non-dummy) last end offset error
				{
					data_in.Position -= 2;
				}
			}
			else
			{
				var image_size = n_rows * (image_bytes_size / 4);//Floor division
				var padding_size = image_bytes_size - image_size;
				textures_data = new byte[image_size + padding_size];
				data_in.Read(textures_data, 0, image_size);
			}
			bool legacy_alpha = (conf.game==G.CROC_2_DEMO_PS1) && (conf.game==G.CROC_2_DEMO_PS1_DUMMY);
			return new TextureFile(n_rows, textures_data, legacy_alpha, textures);
		}

		/// <summary>Draws a complete colored texture image (composed of multiple single textures).</summary>
		public Bitmap to_colorized_texture()
		{
			var rgba = "RGBA";
			var rgb = "RGB";

			var res = new Bitmap(image_dimensions.Width, image_dimensions.Height,  System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		
			var im_4bits_paletted = Image.fromarray(
				np.array(Utils.parse_4bits_paletted(this.textures_data), dtype:np.uint8).reshape(
					image_dimensions.Height, image_dimensions.Width
				),
				"P"
			);
			var im_8bits_paletted = Image.fromarray(
				np.array(list(this.textures_data), dtype:np.uint8).reshape(
					(image_dimensions.Height, image_dimensions.Width / 2)//Floor division
				),
				"P"
			);
			var im_high_color = Image.fromarray(
				np.array(
					Utils.parse_high_color(this.textures_data, true), dtype:np.uint8
				).reshape((image_dimensions.Height, image_dimensions.Width / 4, 4)),//Floor division
				rgba
			);
		
			var texture_mode = this.has_alpha ? rgba : rgb;
			foreach(var texture in this.textures)
			{
				var box = texture.input_box;
				if((texture.flags&TextureFlags.IS_NOT_PALETTED)==0)
				{
					if((texture.flags&TextureFlags.HAS_256_COLORS_PALETTE)!=0)  // 256-colors paletted
					{
						var texture_image = im_8bits_paletted.crop(box);
						texture_image.putpalette(
							Utils.parse_palette(
								this.textures_data,
								256,
								this.has_alpha,
								this.legacy_alpha,
								texture.palette_start ?? 0
							),
							texture_mode
						);
					}
					else// 16-colors paletted
					{
						texture_image = im_4bits_paletted.crop(box);
						texture_image.putpalette(
							Utils.parse_palette(
								this.textures_data,
								16,
								this.has_alpha,
								this.legacy_alpha,
								texture.palette_start ?? 0
							),
							texture_mode
						);
					}
				}
				else// True color (no palette)
				{
					texture_image = im_high_color.crop(box);
				}
				res.paste(texture_image, texture.output_top_left_corner);
			}
			return res;
		}
	}
}