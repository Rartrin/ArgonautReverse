using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;

namespace ArgonautReverse
{
	public sealed class Configuration
	{
		public readonly VersionInfo InputVersion;

		public readonly bool IgnoreWarnings;
		public Configuration(VersionInfo game, bool ignoreWarnings)
		{
			this.InputVersion = game;
			this.IgnoreWarnings = ignoreWarnings;
			//logging.basicConfig(format="%(message)s", level=logging.DEBUG if debug else logging.WARNING)
		}

		public static readonly VersionInfo[] SUPPORTED_GAMES = new VersionInfo[]
		{
			CROC_1_PS1.Instance,
			CROC_2_PS1.Instance,
			CROC_2_DEMO_PS1.Instance,
			CROC_2_DEMO_PS1_DUMMY.Instance,
			HARRY_POTTER_1_PS1.Instance,
			HARRY_POTTER_2_PS1.Instance,
		};
		//Croc 1 parsing is not supported, but it can be sliced
		public static readonly VersionInfo[] PARSABLE_GAMES = new VersionInfo[]
		{
			CROC_2_PS1.Instance,
			CROC_2_DEMO_PS1.Instance,
			CROC_2_DEMO_PS1_DUMMY.Instance,
			HARRY_POTTER_1_PS1.Instance,
			HARRY_POTTER_2_PS1.Instance,
		};
	}
}