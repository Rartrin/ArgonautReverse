namespace ArgonautReverse.Files
{
	public sealed class BINFile:DATFile
	{
		public override string Suffix => "BIN";
		public BINFile(string stem, byte[] data) : base(stem, data){}

		public override string ToString() => "Translated text";
	}
}