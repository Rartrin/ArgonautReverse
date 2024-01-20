namespace ArgonautReverse.Files
{
	public sealed class DEMFile:DATFile
	{
		public override string Suffix => "DEM";

		public DEMFile(string stem, byte[] data) : base(stem, data){}

		public override void PrintInfo(TextWriter output)
		{
			output.WriteLine("Demonstration (DEMO) script");
		}
	}
}