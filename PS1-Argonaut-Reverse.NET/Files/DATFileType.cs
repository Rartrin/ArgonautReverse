namespace ArgonautReverse.Files
{
	public sealed class DATFileType
	{
		public static readonly DATFileType BIN = new DATFileType("BIN",typeof(BINFile));
		public static readonly DATFileType DEM = new DATFileType("DEM",typeof(DEMFile));
		public static readonly DATFileType IMG = new DATFileType("IMG",typeof(IMGFile), "SECURITY", "KEEP");
		public static readonly DATFileType WAD = new DATFileType("WAD",typeof(WADFile), "FESOUND", "FETHUND");
		public static readonly DATFileType NON_PARSABLE = new DATFileType("NON_PARSABLE");

		public static readonly Dictionary<string,DATFileType> Members = new Dictionary<string,DATFileType>
		{
			["BIN"] = BIN,
			["DEM"] = DEM,
			["IMG"] = IMG,
			["WAD"] = WAD,
			["NON_PARSABLE"] = NON_PARSABLE
		};

		public readonly Type file_class;
		public readonly string[] excluded_stems;
		public readonly string suffix;

		public DATFileType(string suffix, Type file_class=null, params string[] excluded_stems)
		{
			this.suffix = suffix;
			this.file_class = file_class;
			this.excluded_stems = excluded_stems ?? Array.Empty<string>();
		}

		public static DATFileType guess_dat_file_type(string stem, string suffix)
		{
			foreach(var (dat_file_type_suffix,dat_file_type) in Members)// type: DATFileType
			{
				if(suffix == dat_file_type_suffix && !dat_file_type.excluded_stems.Contains(stem))
				{
					return dat_file_type;
				}
			}
			return NON_PARSABLE;
		}
	}
}