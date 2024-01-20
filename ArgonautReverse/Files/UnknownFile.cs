
namespace ArgonautReverse.Files
{
	public sealed class UnknownFile:DATFile
	{
		public override string Suffix{get;}
		public UnknownFile(string stem, string suffix, byte[] data) : base(stem, data)
		{
			Suffix = suffix;
		}

		public override string ToString() => $"Unknown file type: {Suffix}";
	}
}
