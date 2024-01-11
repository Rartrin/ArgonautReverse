namespace ArgonautReverse.Files
{
	public sealed class DATFileType
	{
		public delegate DATFile DelCreateDatFile(string stem, byte[] data);

		public static readonly DATFileType BIN = new DATFileType
		(
			"BIN",
			(stem, data) => new BINFile(stem, data)
		);
		public static readonly DATFileType DEM = new DATFileType
		(
			"DEM",
			(stem, data) => new DEMFile(stem, data)
		);
		public static readonly DATFileType IMG = new DATFileType
		(
			"IMG",
			(stem, data) => new IMGFile(stem, data),
			"SECURITY", "KEEP"
		);
		public static readonly DATFileType WAD = new DATFileType
		(
			"WAD",
			(stem, data) => new WADFile(stem, data),
			"FESOUND", "FETHUND"
		);
		public static readonly DATFileType NON_PARSABLE = new DATFileType
		(
			"NON_PARSABLE",
			null
		);

		private static readonly DATFileType[] fileTypes = new DATFileType[]
		{
			BIN,
			DEM,
			IMG,
			WAD
		};

		public readonly DelCreateDatFile CreateDatFile;
		public readonly string[] ExcludedStems;
		public readonly string Suffix;

		private DATFileType(string suffix, DelCreateDatFile createDatFile, params string[] excludedStems)
		{
			Suffix = suffix;
			CreateDatFile = createDatFile;
			ExcludedStems = excludedStems;
		}

		public static DATFile ParseDatFile(string stem, string suffix, byte[] data)
		{
			foreach(var fileType in fileTypes)
			{
				if(suffix == fileType.Suffix && !fileType.ExcludedStems.Contains(stem))
				{
					return fileType.CreateDatFile(stem, data);
				}
			}
			return new UnknownFile(stem, suffix, data);
		}
	}
}