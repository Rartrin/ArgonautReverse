using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;

namespace ArgonautReverse
{
	public sealed class Configuration(DatVersion input, DatVersion? output, bool ignoreWarnings)
	{
		public readonly DatVersion ReadVersion = input;
		public readonly DatVersion? WriteVersion = output;

		public readonly bool IgnoreWarnings = ignoreWarnings;

		public static readonly DatVersion[] ParsableGamesPC =
		[
			Croc2_PC.DatVersion,
			Aladdin_PC.DatVersion,
		];
		public static readonly WadVersion[] ParsableWadsPC = GetWadVersions(ParsableGamesPC);

		public static readonly DatVersion[] ParsableGamesPSX =
		[
			CROC_2_PS1.DatVersion,
			CROC_2_DEMO_PS1.DatVersion,
			CROC_2_DEMO_PS1_DUMMY.DatVersion,
			HARRY_POTTER_1_PS1.DatVersion,
			HARRY_POTTER_2_PS1.DatVersion,
		];
		public static readonly WadVersion[] ParsableWadsPSX = GetWadVersions(ParsableGamesPSX);

		public static readonly DatVersion[] AllParsableGames =
		[
			..ParsableGamesPC,
			..ParsableGamesPSX,
		];
		public static readonly WadVersion[] AllParsableWads = GetWadVersions(AllParsableGames);

		public static readonly Dictionary<string,DatVersion> SliceableGames = CreateDatVersionLookup
		([
			CROC_1_PS1.DatVersion,//Croc 1 parsing is not supported, but it can be sliced
			..ParsableGamesPC,
			..ParsableGamesPSX,
		]);

		public static readonly WadVersion[] AllWads = GetWadVersions(SliceableGames.Values);

		private static Dictionary<string,DatVersion> CreateDatVersionLookup(ReadOnlySpan<DatVersion> datVersions)
		{
			var ret = new Dictionary<string,DatVersion>();
			foreach(var datVersion in datVersions)
			{
				ret.Add(datVersion.Title, datVersion);
			}
			return ret;
		}

		private static WadVersion[] GetWadVersions(IReadOnlyCollection<DatVersion> datVersions) => datVersions.SelectMany(game => game.WadVersions).ToArray();
	}
}