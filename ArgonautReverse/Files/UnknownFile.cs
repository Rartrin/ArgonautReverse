
namespace ArgonautReverse.Files
{
	public sealed class UnknownFile(string stem, string suffix, byte[] data):DATFile(stem, data)
	{
		public override string Suffix => suffix;

		public override void PrintInfo(TextWriter output)
		{
			output.WriteLine($"Unknown file type: {Suffix}");
		}
	}
}