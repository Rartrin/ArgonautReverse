namespace ArgonautReverse.Files
{
	public sealed class DATFileType
	{
		public delegate DATFile DelCreateDatFile(Configuration conf, string stem, byte[] data);

		public static readonly DATFileType BIN = new DATFileType
		(
			"BIN",
			(conf, stem, data) => new BINFile(stem, data)
		);
		public static readonly DATFileType DEM = new DATFileType
		(
			"DEM",
			(conf, stem, data) => new DEMFile(stem, data)
		);
		public static readonly DATFileType IMG = new DATFileType
		(
			"IMG",
			(conf, stem, data) => new IMGFile(stem, data),
			"SECURITY", "KEEP"
		);
		public static readonly DATFileType WAD = new DATFileType
		(
			"WAD",
			(conf, stem, data) => new WADFile(conf.ReadVersion.GetWadVersion(stem), stem, data),
			"FESOUND", "FETHUND"
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

		public static DATFile ParseDatFile(Configuration conf, string stem, string suffix, byte[] data)
		{
			foreach(var fileType in fileTypes)
			{
				if(suffix == fileType.Suffix && !fileType.ExcludedStems.Contains(stem))
				{
					return fileType.CreateDatFile(conf, stem, data);
				}
			}
			return new UnknownFile(stem, suffix, data);
		}
	}
}