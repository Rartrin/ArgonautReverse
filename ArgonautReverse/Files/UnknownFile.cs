
namespace ArgonautReverse.Files
{
	public sealed class UnknownFile:DATFile
	{
		public override string Suffix{get;}
		public UnknownFile(string stem, string suffix, byte[] data) : base(stem, data)
		{
			Suffix = suffix;
		}

		public override void PrintInfo(TextWriter output)
		{
			output.WriteLine($"Unknown file type: {Suffix}");
		}
	}
}
