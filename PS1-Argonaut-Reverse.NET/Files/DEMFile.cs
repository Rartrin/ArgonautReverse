namespace ArgonautReverse.Files
{
	public sealed class DEMFile:DATFile
	{
		//suffix = "DEM"

		public DEMFile(string stem, string suffix = null, byte[] data = null) : base(stem, suffix, data){}

		public override string ToString() => "Demonstration (DEMO) script";
	}
}