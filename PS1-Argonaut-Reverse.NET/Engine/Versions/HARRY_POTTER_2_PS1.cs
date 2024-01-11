namespace ArgonautReverse.Engine.Versions
{
	public sealed class HARRY_POTTER_2_PS1:VersionInfo
	{
		public static HARRY_POTTER_2_PS1 Instance{get;} = new HARRY_POTTER_2_PS1();

		public override string Title => "Harry Potter 2 PS1";
		public override DateTime BuildDate => new DateTime(2002, 11, 5);
		public override string FilenameDAT => "POTTER.DAT";
		public override string FilenameDIR => "POTTER.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		public override bool NEW_COLLISION => true;

		private HARRY_POTTER_2_PS1(){}
	}
}