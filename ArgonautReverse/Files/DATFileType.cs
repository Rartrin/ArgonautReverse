namespace ArgonautReverse.Files
{
	public sealed class DATFileType
	{
		public delegate DATFile DelCreateDatFile(Configuration conf, string stem, byte[] data);

		private static readonly DATFileType[] fileTypes =
		[
			new DATFileType
			(
				"BIN",
				(conf, stem, data) => new BINFile(stem, data)
			),
			new DATFileType
			(
				"DEM",
				(conf, stem, data) => new DEMFile(stem, data)
			),
			new DATFileType
			(
				"IMG",
				(conf, stem, data) => new IMGFile(stem, data),
				"SECURITY", "KEEP"
			),
			new DATFileType
			(
				"WAD",
				(conf, stem, data) => WADFile.Create(conf.ReadVersion, conf.ReadVersion.GetWadVersion(stem), stem, data),
				"FESOUND", "FETHUND"
			)
		];

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
				if(string.Equals(suffix, fileType.Suffix, StringComparison.OrdinalIgnoreCase) && !fileType.ExcludedStems.Contains(stem))
				{
					return fileType.CreateDatFile(conf, stem, data);
				}
			}
			return new UnknownFile(stem, suffix, data);
		}
	}
}