namespace ArgonautReverse.Engine.Versions
{
	public sealed class CROC_2_DEMO_PS1_DUMMY:VersionInfo
	{
		//Croc 2 PS1 US Demo's DUMMY.DAT
		//The files in here were almost certainly built prior to the rest of the demo
		public static CROC_2_DEMO_PS1_DUMMY Instance{get;} = new CROC_2_DEMO_PS1_DUMMY();

		public override string Title => "Croc 2 Demo PS1 (Dummy)";
		public override DateTime BuildDate => new DateTime(1999, 3, 4);
		public override string FilenameDAT => "DUMMY.DAT";
		public override string FilenameDIR => null;
		public override DirFormat DirFormat => null;

		public override bool NEW_COLLISION => false;

		private CROC_2_DEMO_PS1_DUMMY(){}
	}
}