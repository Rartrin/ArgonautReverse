namespace ArgonautReverse.Engine
{
	public abstract class DatVersion
	{
		public abstract string Title{get;}

		public abstract string FilenameDAT{get;}
		public abstract string FilenameDIR{get;}
		public abstract DirFormat DirFormat{get;}

		public abstract IReadOnlyCollection<WadVersion> WadVersions{get;}

		public abstract WadVersion GetWadVersion(string wadName);
	}
}
