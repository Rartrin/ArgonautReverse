namespace ArgonautReverse.Engine.Versions
{
	public sealed class CROC_2_DEMO_PS1:VersionInfo
	{
		//Croc 2 PS1 US Demo
		public static CROC_2_DEMO_PS1 Instance{get;} = new CROC_2_DEMO_PS1();

		public override string Title => "Croc 2 Demo PS1";
		public override DateTime BuildDate => new DateTime(1999, 3, 4);
		public override string FilenameDAT => "CROCII.DAT";
		public override string FilenameDIR => "CROCII.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		public override bool NEW_COLLISION => false;

		private CROC_2_DEMO_PS1(){}
	}
}