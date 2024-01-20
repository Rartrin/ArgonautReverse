namespace ArgonautReverse.Files
{
	public sealed class DEMFile:DATFile
	{
		public override string Suffix => "DEM";

		public DEMFile(string stem, byte[] data) : base(stem, data){}

		public override string ToString() => "Demonstration (DEMO) script";
	}
}