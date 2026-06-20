using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine
{
	public abstract class WadVersion
	{
		/// <summary>A unique date for indicating a version</summary>
		public abstract DateTime BuildDate{get;}

		#region Macros
		//TODO: Find way to determine these programmatically?
		public abstract bool NEW_COLLISION{get;}
		public abstract bool KEYFRAME_STUFF{get;}
		public abstract bool HAS_SPLINE_POINTS{get;}
		public abstract bool HAS_STRAT_ARRAY_POOL{get;}
		#endregion

		#region Comparison
		public bool IsOlderOrSame(WadVersion version) => this.BuildDate <= version.BuildDate;
		public bool IsNewerOrSame(WadVersion version) => this.BuildDate >= version.BuildDate;

		public bool Is(WadVersion version) => this == version;
		public bool Is(params WadVersion[] versions) => versions.Contains(this);
		#endregion

		public abstract InstructionOpcode MapOpcode(int value);
		public abstract TriggerType MapTriggerType(int value);
	}
}