namespace ArgonautReverse.Engine
{
	public enum Platform
	{
		UNKNOWN = 0,
		PC,
		PSX,
	}
	//DatVersion is generally for each game release where as WadVersion is per wad.
	//Most cases the WadVersion will be connected to the DatVersion except for the DUMMY wads since they are from multiple builds.
	//TODO: Perhaps Make the DatVersion encompase all version of a game for a platform, including demos, then use the WadVersion for the rest? This might require splitting up the DUMMY wads.
	public abstract class DatVersion
	{
		public abstract string Title{get;}

		public abstract string FilenameDAT{get;}
		public abstract string? FilenameDIR{get;}
		public abstract DirFormat? DirFormat{get;}

		public abstract Platform Platform{get;}

		public abstract IReadOnlyCollection<WadVersion> WadVersions{get;}

		//Null for latest
		public abstract WadVersion GetWadVersion(string? wadName);
	}
}