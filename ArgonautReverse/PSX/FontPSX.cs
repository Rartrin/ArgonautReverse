using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine;

namespace ArgonautReverse.PSX
{
	public sealed class FontPSX:IReadable<FontPSX>,IConvertibleToOSE<FontOSE>
	{
		public readonly ushort Texture;
		public readonly ushort BaseLine;
		public readonly ushort Width;
		public readonly ushort Height;

		private FontPSX(ushort texture, ushort baseLine, ushort width, ushort height)
		{
			Texture = texture;
			BaseLine = baseLine;
			Width = width;
			Height = height;
		}

		public static FontPSX Parse(WadReader parser)
		{
			var texture = parser.Read<ushort>();
			var baseLine = parser.Read<ushort>();
			var width = parser.Read<ushort>();
			var height = parser.Read<ushort>();
			return new FontPSX(texture, baseLine, width, height);
		}

		FontOSE IConvertibleToOSE<FontOSE>.ToOSE() => new FontOSE
		(
			textureId:Texture,
			charOffsetY:BaseLine,
			charWidth:Width,
			charHeight:Height
		);
	}
}
