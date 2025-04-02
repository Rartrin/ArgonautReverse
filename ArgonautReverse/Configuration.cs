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
		}

		public static readonly DatVersion[] SUPPORTED_GAMES =
		[
			CROC_1_PS1.DatVersion,
			CROC_2_PS1.DatVersion,
			CROC_2_DEMO_PS1.DatVersion,
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion,
			Croc2_PC.DatVersion,
		];

		//Croc 1 parsing is not supported, but it can be sliced
		public static readonly DatVersion[] PSX_PARSABLE_GAMES =
		[
			CROC_2_PS1.DatVersion,
			CROC_2_DEMO_PS1.DatVersion,
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion,
		];

		public static readonly DatVersion[] PC_PARSABLE_GAMES =
		[
			Croc2_PC.DatVersion,
		];

		public static readonly DatVersion[] ALL_PARSABLE_GAMES = PSX_PARSABLE_GAMES.Concat(PC_PARSABLE_GAMES).ToArray();

		
		public static readonly WadVersion[] ALL_WADS = SUPPORTED_GAMES.SelectMany(game => game.WadVersions).ToArray();
		
		public static readonly WadVersion[] PSX_PARSABLE_WADS = PSX_PARSABLE_GAMES.SelectMany(game => game.WadVersions).ToArray();
		public static readonly WadVersion[] PC_PARSABLE_WADS = PC_PARSABLE_GAMES.SelectMany(game => game.WadVersions).ToArray();
		public static readonly WadVersion[] ALL_PARSABLE_WADS = ALL_PARSABLE_GAMES.SelectMany(game => game.WadVersions).ToArray();
	}
}