namespace ArgonautReverse.Engine.Versions
{
	public sealed class HARRY_POTTER_1_PS1:VersionInfo
	{
		public static HARRY_POTTER_1_PS1 Instance{get;} = new HARRY_POTTER_1_PS1();

		public override string Title => "Harry Potter 1 PS1";
		public override DateTime BuildDate => new DateTime(2001, 12, 1);
		public override string FilenameDAT => "POTTER.DAT";
		public override string FilenameDIR => "POTTER.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		public override bool NEW_COLLISION => true;

		private HARRY_POTTER_1_PS1(){}
	}
}