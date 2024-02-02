using ArgonautReverse.IO;

namespace ArgonautReverse.Engine
{
	public abstract class DirFormat
	{
		public abstract void Pack(WadWriter writer, string name, int size, int start);
		public abstract void Unpack(BaseReader reader, out string name, out int size, out int start);
	}
}