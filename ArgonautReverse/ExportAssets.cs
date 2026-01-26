using ArgonautReverse.Files;

namespace ArgonautReverse
{
	public sealed class ProgramArgs
	{
		public string ReadFormat;
		public string? PsxDirDat;//Path to PSX DIR/DAT files
		public string? ReadWads;//Path to WAD files or directories containing WADs

		public string? WriteFormat;
		public string? WriteWads;//Output path for converted wads

		public string? ExtractPath;//Output path for extracted assets
		public bool ExtractTextures = false;
		public bool ExtractModels = false;
		public bool ExtractAudio = false;//Extract WAV audio
		public bool UnpackAudio = false;//Extract and unpacked PS1 WAG audio
		public bool ExtractLevels = false;
		public bool ExtractIMGs = false;
		public bool ExtractActors = false;
					
		public bool IgnoreWarnings;

		public string GetExtractDirectory(string stem, string subdirectory)
		{
			var path = Path.Join(ExtractPath, stem, subdirectory);
			ExportAssets.CreateExportDirectory(path);
			return path;
		}
	}
	public static class ExportAssets
	{
		public static ProgramArgs ParseArgs(string[] args)
		{
			var parsedArgs = new ProgramArgs();
			for(int i=0; i<args.Length; i++)
			{
				switch(args[i])
				{
					case "--read-format":parsedArgs.ReadFormat = args[++i];break;
					case "--read-psx-dirdat":parsedArgs.PsxDirDat = args[++i];break;
					case "--read-wads":parsedArgs.ReadWads = args[++i];break;

					case "--write-format":parsedArgs.WriteFormat = args[++i];break;
					case "--write-wads":parsedArgs.WriteWads = args[++i];break;

					case "--extract-path":parsedArgs.ExtractPath = args[++i];break;

					case "--extract":
					{
						var extractions = args[++i].Split(',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
						foreach(var extraction in extractions)
						{
							switch(extraction)
							{
								case "actors":parsedArgs.ExtractActors = true;break;
								case "audio":parsedArgs.ExtractAudio = true;break;
								case "audioUnpacked":parsedArgs.UnpackAudio = true;break;
								case "imgs":parsedArgs.ExtractIMGs = true;break;
								case "levels":parsedArgs.ExtractLevels = true;break;
								case "models":parsedArgs.ExtractModels = true;break;
								case "textures":parsedArgs.ExtractTextures = true;break;
								default:throw new Exception($"Unknown excration type: {extraction}");
							}
						}
						break;
					}
					case "--ignore-warnings":parsedArgs.IgnoreWarnings = true;break;

					default:throw new Exception("Unknown argument: " + args[i]);
				}
			}
			if(parsedArgs.ReadFormat == null)
			{
				throw new Exception("Missing read-format argument.");
			}

			return parsedArgs;
		}

		public static void CreateExportDirectory(string path)
		{
			if(File.Exists(path))
			{
				throw new Exception("Export path must be a directory but already exists a.");
			}
			else if(!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		public static void Run(string[] rawArgs)
		{
			var args = ParseArgs(rawArgs);

			var readFormat = Configuration.SUPPORTED_GAMES[args.ReadFormat];
			var writeFormat = args.WriteFormat!=null ? Configuration.SUPPORTED_GAMES[args.WriteFormat] : null;
			if(!Configuration.ALL_PARSABLE_GAMES.Contains(readFormat))
			{
				throw new NotImplementedException("Files from this game can be extracted, but not reversed (yet). If you just want to extract them, use the extract_files_from_dat.py script.");
			}

			var conf = new Configuration(readFormat, writeFormat, args.IgnoreWarnings);

			DIR_DAT dir_dat;
			if(args.PsxDirDat is string psxDirDat)
			{
				dir_dat = DIR_DAT.FromDirDat(conf, psxDirDat);
			}
			else if(args.ReadWads is string readWads)
			{
				dir_dat = DIR_DAT.FromFiles(conf, readWads.Split(','));
			}
			else
			{
				Console.WriteLine("Missing either --read-wads or --read-psx-dirdat");
				return;
			}
			if(args.ExtractPath != null)
			{
				CreateExportDirectory(args.ExtractPath);
			}
			Console.WriteLine("--Parsing--");
			var parsedSuccessfully = new List<DATFile>();
			RunOnFiles(dir_dat.Files, datFile =>
			{
				datFile.Parse(args, conf);
				datFile.PrintInfo(Console.Out);
				parsedSuccessfully.Add(datFile);
			});

			if(args.ExtractPath != null)
			{
				Console.WriteLine("--Extracting--");
				RunOnFiles(parsedSuccessfully, datFile =>
				{
					datFile.ExtractAssets(args, conf);
				});
			}

			if(args.WriteFormat != null)
			{
				Console.WriteLine("--Converting WADs--");
				RunOnFiles(parsedSuccessfully, datFile =>
				{
					if(datFile is not WADFile wadFile){return;}
					//wadFile.Serialize()
				});
			}
		}
		private static void RunOnFiles(IReadOnlyList<DATFile> files, Action<DATFile> action)
		{
			var n_digits = files.Count.ToString().Length;
			for(int i=0; i<files.Count; i++)
			{
				var datFile = files[i];
				Console.WriteLine($"[{(i + 1).ToString().PadLeft(n_digits)}/{files.Count}] {datFile.Name:12}");
				try
				{
					action(datFile);
				}
				catch(Exception e)
				{
					Console.WriteLine($"FAILED!");
					Console.WriteLine(e.ToString());
				}
				Console.WriteLine();
			}
		}
	}
}