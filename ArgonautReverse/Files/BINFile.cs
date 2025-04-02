namespace ArgonautReverse.Files
{
	public sealed class BINFile(string stem, byte[] data):DATFile(stem, data)
	{
		public override string Suffix => "BIN";

		public override void PrintInfo(TextWriter output)
		{
			output.WriteLine("Translated text");
		}
	}
}