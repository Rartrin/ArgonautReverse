using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public sealed class SimpleImagePSX:IReadable<SimpleImagePSX>, IWritable
	{
		public readonly ushort[] Palette;
		public readonly byte[] Bitmap;

		private SimpleImagePSX(ushort[] palette, byte[] bitmap)
		{
			Palette = palette;
			Bitmap = bitmap;
		}

		static SimpleImagePSX IReadable<SimpleImagePSX>.Parse(WadReader reader)
		{
			var palette = reader.ReadArray<ushort>(256);//Palette
			var bitmap = reader.ReadArray<byte>(64 * 40);//Bitmap
			return new(palette, bitmap);
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.WriteSizedArray<ushort>(256, Palette);//Palette
			writer.WriteSizedArray<byte>(64 * 40, Bitmap);//Bitmap
		}
	}
}