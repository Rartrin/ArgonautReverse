namespace ArgonautReverse.Engine
{
	public abstract class WadVersion
	{
		/// <summary>A unique date for indicating a version</summary>
		public abstract DateTime BuildDate{get;}

		#region Macros
		//TODO: Find way to determine NEW_COLLISION programmatically?
		public abstract bool NEW_COLLISION{get;}
		#endregion

		#region Comparison
		public bool IsOlderOrSame(WadVersion version) => this.BuildDate <= version.BuildDate;
		public bool IsNewerOrSame(WadVersion version) => this.BuildDate >= version.BuildDate;

		public bool Is(WadVersion version) => this == version;
		public bool Is(params WadVersion[] versions) => versions.Contains(this);
		#endregion
	}
}
