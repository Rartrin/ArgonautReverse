using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public sealed record class SurfacePC(int Width, int Height, byte Flags, byte[] Pixels):IReadable<SurfacePC>, IWritable
	{
		public static SurfacePC Parse(WadReader reader)
		{
			var width = reader.Read<int>();
			var height = reader.Read<int>();
			var flags = reader.Read<byte>();
			reader.AssertRead<byte>(0);//Gap?//Always 0?
			int dataSize;
			if((flags & 0x80u) != 0)
			{
				//Compressed data
				dataSize = reader.Read<int>();
			}
			else
			{
				dataSize = sizeof(ushort) * width * height;
			}
			var pixels = reader.ReadArray<byte>(dataSize);
			return new
			(
				Width: width,
				Height: height,
				Flags: flags,
				Pixels: pixels
			);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<int>(Width);
			writer.Write<int>(Height);
			writer.Write<byte>(Flags);
			writer.Write<byte>(0);//Gap
			if((Flags & 0x80u) != 0)
			{
				//Compressed data
				writer.Write<int>(Pixels.Length);
			}
			else
			{
				if(Pixels.Length != sizeof(ushort) * Width * Height)
				{
					throw new Exception();
				}
			}
			writer.WriteArray<byte>(Pixels);
		}
	}
}