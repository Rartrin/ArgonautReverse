using System.Drawing;
using System.Drawing.Imaging;

namespace ArgonautReverse.Files
{
	public sealed class ImageType//(Enum):
	{
		public int bytes_size;
		public XY dimensions;
		public int n_palette_colors;
		public bool has_alpha;

		public ImageType(int bytes_size, (int X, int Y) dimensions, int n_palette_colors, bool has_alpha=true)
		{
			this.bytes_size = bytes_size;
			this.dimensions = new XY(dimensions.X,dimensions.Y);
			this.n_palette_colors = n_palette_colors;
			this.has_alpha = has_alpha;

			if(n_palette_colors == 0)
			{
				Utils.Assert(2 * dimensions.X * dimensions.Y == bytes_size);
			}
			else if(n_palette_colors == 16)
			{
				Utils.Assert(dimensions.X * dimensions.Y == 2 * (bytes_size - 32));
			}
			else if(n_palette_colors == 256)
			{
				Utils.Assert(dimensions.X * dimensions.Y == (bytes_size - 512 - ((this == SKY)?256:0)));
			}
			else
			{
				throw new Exception("Unsupported color palette size");
			}
		}

		public static ImageType guess_from_bytes_size(int bytes_size)
		{
			foreach(var member in lookup)
			{
				if(member.bytes_size == bytes_size)
				{
					return member;
				}
			}
			throw new Exception();
		}

		public static readonly ImageType LOAD = new ImageType(262144, (512, 256), 0, false);
		public static readonly ImageType STORY = new ImageType(123392, (512, 240), 256);
		public static readonly ImageType TEXT = new ImageType(72192, (448, 160), 256);
		public static readonly ImageType BADGES1 = new ImageType(229376, (448, 256), 0);
		public static readonly ImageType BADGES2 = new ImageType(215040, (448, 240), 0);
		public static readonly ImageType FLAG1 = new ImageType(49664, (128, 384), 256);
		public static readonly ImageType FLAG2 = new ImageType(25088, (128, 192), 256);
		public static readonly ImageType FORKLIGT = new ImageType(42804, (218, 194), 256);
		public static readonly ImageType PAGE = new ImageType(1536, (32, 32), 256);
		public static readonly ImageType PLAY = new ImageType(20992, (256, 80), 256);
		public static readonly ImageType SKY = new ImageType(131840, (512, 256), 256);
		public static readonly ImageType SKY2 = new ImageType(131584, (512, 256), 256);
		public static readonly ImageType CARD = new ImageType(6656, (96, 64), 256);
		public static readonly ImageType DADA = new ImageType(608, (48, 24), 16);
		public static readonly ImageType REFL = new ImageType(4128, (64, 128), 16);
		public static readonly ImageType LBAR = new ImageType(336, (12, 14), 0);
		public static readonly ImageType SAVE = new ImageType(9728, (96, 96), 256);
		public static readonly ImageType AM = new ImageType(31232, (320, 96), 256);
		public static readonly ImageType GLOW = new ImageType(832, (40, 40), 16);
		public static readonly ImageType GRADE = new ImageType(288, (32, 16), 16);
		public static readonly ImageType WIZ = new ImageType(2080, (64, 64), 16);
		public static readonly ImageType WIZPAGE = new ImageType(2080, (128, 32), 16);

		public static ImageType[] lookup = new ImageType[]
		{
			LOAD,
			STORY,
			TEXT,
			BADGES1,
			BADGES2,
			FLAG1,
			FLAG2,
			FORKLIGT,
			PAGE,
			PLAY,
			SKY,
			SKY2,
			CARD,
			DADA,
			REFL,
			LBAR,
			SAVE,
			AM,
			GLOW,
			GRADE,
			WIZ,
			WIZPAGE,
		};
	}

	public sealed class IMGFile:DATFile, BaseDataClass
	{
		//suffix = "IMG"
		public IReadOnlyList<Bitmap> Images{get;private set;}

		public IMGFile(string stem, string suffix = null, byte[] data = null):base(stem, suffix, data:data){}
	
		public override string ToString()
		{
			var dimensions = string.Join(", ", Images.Select(x => $"({x.Width}x{x.Height} px)"));
			var res = "Menu image";
			if(!string.IsNullOrEmpty(dimensions))
			{
				res += $"\n{dimensions}";
			}
			return res;
		}
		public override void Parse(Configuration conf/*, string kwargs_stem*/)
		{
			ArraySegment<byte>[] images_data;
			if(this.Stem == "REPORT")// Patch for REPORT.IMG that contains multiple images
			{
				var imageSizes = new[]{608, 288, 288, 288, 608, 608, 608, 608, 608, 608, 608, 608};
				//This starts as the space between offsets. Then we make each offset the sum all values up to this one.

				images_data = new ArraySegment<byte>[imageSizes.Length];
				int curOffset = imageSizes[0];
				for(int i=1; i<imageSizes.Length; i++)
				{
					images_data[i-1] = new ArraySegment<byte>(this._data, curOffset, curOffset+imageSizes[i]);
					curOffset += imageSizes[i];
				}
				//images_data = [this._data[offsets[i - 1] : offsets[i]] for i in range(1, offsets.Length)];
			}
			else
			{
				images_data = new ArraySegment<byte>[1]{this._data};
			}

			var images = new List<Bitmap>();
			foreach(var image_data in images_data)
			{
				if(ImageType.lookup.Any(image_type => image_type.bytes_size == image_data.Count))//(image_data.Count in [image_type.bytes_size for image_type in ImageType])
				{
					ImageType image_type;
					// Fix for WIZPAGE.IMG that has the same bytes length than other WIZ files but not the same dimensions
					//if(kwargs_stem == "WIZPAGE")
					//{
					//	image_type = ImageType.WIZPAGE;
					//}
					//else
					{
						image_type = ImageType.guess_from_bytes_size(image_data.Count);
					}
					Color[] palette;
					if(image_type.n_palette_colors != 0)
					{
						palette = Utils.parse_palette(image_data, image_type.n_palette_colors, image_type.has_alpha);
					}
					else
					{
						palette = null;
					}
					images.Add(
						IMGFile.to_full_colorized(
							image_data,
							image_type.dimensions,
							palette,
							image_type.n_palette_colors,
							image_type.has_alpha
						)
					);
				}
				else
				{
					throw new Exception("Unknown image size");
				}
			}
			this.Images = images;
		}

		public static unsafe Bitmap to_full_colorized(ArraySegment<byte> data, XY dimensions, Color[] palette, int n_palette_colors, bool has_alpha)
		{
			var pixels_data = data.AsSpan(0, 2*n_palette_colors);
			fixed(byte* pixels_data0 = pixels_data)
			{
				if(n_palette_colors != 0)
				{
					Bitmap image;
					if(n_palette_colors == 16)
					{
						var pixels_data_4bit = Utils.parse_4bits_paletted(pixels_data);
						fixed(byte* pixels_data_4bit0 = pixels_data_4bit)
						{
							//TODO: Y,X? Notice stride
							image = new Bitmap(dimensions.Y, dimensions.X, dimensions.Y, PixelFormat.Format4bppIndexed, (IntPtr)pixels_data_4bit0);
							//var pixels = np.reshape(np.array(Utils.parse_4bits_paletted(pixels_data), dtype:np.uint8), (dimensions.Y, dimensions.X));
						}
					}
					else
					{
						image = new Bitmap(dimensions.X, dimensions.Y, dimensions.X, PixelFormat.Format8bppIndexed, (IntPtr)pixels_data0);
					}
					var imagePalette = image.Palette;
					for(int i=0; i<n_palette_colors; i++)
					{
						imagePalette.Entries[i] = palette[i];
					}
					image.Palette = imagePalette;
					return image;
				}
				else
				{
					var highColor = Utils.parse_high_color(pixels_data, has_alpha);
					fixed(Color* highColor0 = highColor)
					{
						return new Bitmap(dimensions.Y, dimensions.X, dimensions.Y, PixelFormat.Format32bppArgb, (IntPtr)highColor0);
						//var pixels = np.reshape(np.array(Utils.parse_high_color(pixels_data, has_alpha), dtype:np.uint8),(dimensions.Y, dimensions.X, has_alpha?4:3));
						//return Image.fromarray(pixels, mode);
					}
				}
			}
		}
	}
}