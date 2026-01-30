namespace ArgonautReverse.Engine
{
	public enum Platform
	{
		UNKNOWN = 0,
		PC,
		PSX,
	}
	public abstract class DatVersion
	{
		public abstract string Title{get;}

		public abstract string? FilenameDAT{get;}
		public abstract string? FilenameDIR{get;}
		public abstract DirFormat? DirFormat{get;}

		public abstract Platform Platform{get;}

		public abstract IReadOnlyCollection<WadVersion> WadVersions{get;}

		//Null for latest
		public abstract WadVersion GetWadVersion(string? wadName);
	}
}