using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;

namespace ArgonautReverse
{
	public sealed class Configuration
	{
		public readonly DatVersion ReadVersion;
		public readonly DatVersion WriteVersion;

		public readonly bool IgnoreWarnings;
		public Configuration(DatVersion input, DatVersion output, bool ignoreWarnings)
		{
			ReadVersion = input;
			WriteVersion = output;
			IgnoreWarnings = ignoreWarnings;
			//logging.basicConfig(format="%(message)s", level=logging.DEBUG if debug else logging.WARNING)
		}

		public static readonly DatVersion[] SUPPORTED_GAMES = new DatVersion[]
		{
			CROC_1_PS1.DatVersion,
			CROC_2_PS1.DatVersion,
			CROC_2_DEMO_PS1.DatVersion,
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion,
		};
		//Croc 1 parsing is not supported, but it can be sliced
		public static readonly DatVersion[] PARSABLE_GAMES = new DatVersion[]
		{
			CROC_2_PS1.DatVersion,
			CROC_2_DEMO_PS1.DatVersion,
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion,
		};

		public static readonly WadVersion[] ALL_WADS = SUPPORTED_GAMES.SelectMany(game => game.WadVersions).ToArray();
		public static readonly WadVersion[] PARSABLE_WADS = PARSABLE_GAMES.SelectMany(game => game.WadVersions).ToArray();
	}
}