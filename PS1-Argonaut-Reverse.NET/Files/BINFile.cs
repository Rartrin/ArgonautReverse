namespace ArgonautReverse.Files
{
	public sealed class BINFile:DATFile
	{
		public BINFile(string stem, string suffix = null, byte[] data = null) : base(stem, suffix, data){}

		//suffix = "BIN"

		public override string ToString() => "Translated text";
	}
}