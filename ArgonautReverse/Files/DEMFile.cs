namespace ArgonautReverse.Files
{
	public sealed class DEMFile(string stem, byte[] data):DATFile(stem, data)
	{
		public override string Suffix => "DEM";

		public override void PrintInfo(TextWriter output)
		{
			output.WriteLine("Demonstration (DEMO) script");
		}
	}
}