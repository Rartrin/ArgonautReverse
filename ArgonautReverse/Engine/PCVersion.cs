namespace ArgonautReverse.Engine
{
	public abstract class DatVersionPC:DatVersion
	{
		public abstract WadVersion WadVersion{get;}

		public sealed override string? FilenameDAT => null;
		public sealed override string? FilenameDIR => null;
		public sealed override DirFormat? DirFormat => null;

		public sealed override Platform Platform => Platform.PC;

		public sealed override IReadOnlyCollection<WadVersion> WadVersions => [WadVersion];
		public sealed override WadVersion GetWadVersion(string? wadName) => WadVersion;
	}
}