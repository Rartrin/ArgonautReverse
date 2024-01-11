namespace ArgonautReverse.Engine
{
	public abstract class VersionInfo
	{
		public abstract string Title{get;}
		public abstract DateTime BuildDate{get;}
		public abstract string FilenameDAT{get; }
		public abstract string FilenameDIR{get;}
		public abstract DirFormat DirFormat{get;}

		//Macros
		public abstract bool NEW_COLLISION{get;}
	}
}
