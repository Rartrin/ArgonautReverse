using ArgonautReverse.Files;

namespace ArgonautReverse
{
	public sealed class ProgramArgs
	{
		public string? ReadFormat;
		public string? PsxDirDat;//Path to PSX DIR/DAT files
		public string? PsxDatSearch;//Path to PSX DAT file without a DIR
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
		public bool ExtractScripts = false;
		public bool HelpMenu = false;
					
		public bool IgnoreWarnings;

		public string GetExtractDirectory(string stem, string subdirectory)
		{
			var path = Path.Join(ExtractPath, stem, subdirectory);
			ExportAssets.CreateExportDirectory(path);
			return path;
		}
	}
	public sealed class Option
	{
		public readonly string Description;
		public readonly int OperandCount;
		public readonly Action<ProgramArgs,ReadOnlySpan<string>> Apply;

		public Option(string description, Action<ProgramArgs> apply)
		{
			Description = description;
			OperandCount = 0;
			Apply = (args, rawArgs) => apply(args);
		}

		public Option(string description, Action<ProgramArgs,string> apply)
		{
			Description = description;
			OperandCount = 1;
			Apply = (args, rawArgs) => apply(args, rawArgs[0]);
		}
	}
	public static class ExportAssets
	{
		private static readonly Dictionary<string,Option> programOptions = new()
		{
			["--read-format"] = new
			(
				"The game format to use when reading files.",
				(args, raw) => args.ReadFormat = raw
			),
			["--read-psx-dirdat"] = new
			(
				"PSX DIR/DAT files to read. Just speicify one file, the other file with be inferred from the provided path.",
				(args, raw) => args.PsxDirDat = raw
			),
			["--search-psx-dat"] = new
			(
				"PSX DAT file to read when the DIR is missing. Will search through to locate WADs.",
				(args, raw) => args.PsxDatSearch = raw
			),
			["--read-wads"] = new
			(
				"Comma separated list of files or directories containing WADs to be read.",
				(args, raw) => args.ReadWads = raw
			),
			["--write-format"] = new
			(
				"Game format to use when writing files. (WIP)",
				(args, raw) => args.WriteFormat = raw
			),
			["--write-wads"] = new
			(
				"Directory for wads to be written to.",
				(args, raw) => args.WriteWads = raw
			),
			["--extract-path"] = new
			(
				"Directory for assets for be extracted to.",
				(args, raw) => args.ExtractPath = raw
			),
			["--extract"] = new
			(
				"Comma separated list of assets that can be extracted. [audio,audioUnpacked,imgs,levels,models,scripts,textures]",
				(args, raw) =>
				{
					var extractions = raw.Split(',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
					foreach(var extraction in extractions)
					{
						switch(extraction)
						{
							case "audio":args.ExtractAudio = true;break;
							case "audioUnpacked":args.UnpackAudio = true;break;
							case "imgs":args.ExtractIMGs = true;break;
							case "levels":args.ExtractLevels = true;break;
							case "models":args.ExtractModels = true;break;
							case "scripts":args.ExtractScripts = true;break;
							case "textures":args.ExtractTextures = true;break;
							default:throw new Exception($"Unknown excration type: {extraction}");
						}
					}
				}
			),
			["--ignore-warnings"] = new
			(
				"Treats some errors as warnings.",
				(args) => args.IgnoreWarnings = true
			),
			["--help"] = new
			(
				"Displays this help menu.",
				(args)=> args.HelpMenu = true
			),
		};

		public static ProgramArgs? ParseArgs(string[] args)
		{
			var parsedArgs = new ProgramArgs();
			for(int i=0; i<args.Length;)
			{
				if(!programOptions.TryGetValue(args[i], out var option))
				{
					Console.WriteLine($"Unknown argument: {args[i]}");
					return null;
				}
				i += 1;

				var operands = args.AsSpan(i, option.OperandCount);
				i += option.OperandCount;

				option.Apply(parsedArgs, operands);
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
			if(args == null){return;}

			if(args.HelpMenu)
			{
				int maxCommandLength = programOptions!.Keys.Max(k => k.Length);
				foreach(var option in programOptions)
				{
					Console.WriteLine($"{option.Key.PadRight(maxCommandLength)} - {option.Value.Description}");
				}
				return;
			}

			if(args.ReadFormat == null)
			{
				throw new Exception("Missing read-format argument.");
			}

			var readFormat = Configuration.SliceableGames[args.ReadFormat];
			var writeFormat = args.WriteFormat!=null ? Configuration.SliceableGames[args.WriteFormat] : null;
			if(!Configuration.AllParsableGames.Contains(readFormat))
			{
				throw new NotImplementedException("Files from this game can be extracted, but not reversed (yet). If you just want to extract them, use the extract_files_from_dat.py script.");
			}

			var conf = new Configuration(readFormat, writeFormat, args.IgnoreWarnings);

			IReadOnlyList<DATFile> files;
			if(args.PsxDirDat is string psxDirDat)
			{
				files = DIR_DAT.FromDirDat(conf, psxDirDat);
			}
			else if(args.PsxDatSearch is string psxDat)
			{
				files = DIR_DAT.SearchDat(conf, psxDat);
			}
			else if(args.ReadWads is string readWads)
			{
				files = DIR_DAT.FromFiles(conf, readWads.Split(','));
			}
			else
			{
				Console.WriteLine("Missing either --read-wads, --read-psx-dirdat, or --search-psx-dat");
				return;
			}
			if(args.ExtractPath != null)
			{
				CreateExportDirectory(args.ExtractPath);
			}
			Console.WriteLine("--Parsing--");
			var parsedSuccessfully = new List<DATFile>();
			RunOnFiles(files, datFile =>
			{
				datFile.Parse(args, conf);
				datFile.PrintInfo(Console.Out);
				parsedSuccessfully.Add(datFile);
			});

			if(args.ExtractScripts)
			{
				Console.WriteLine("--Processing Strats--");
				RunOnFiles(parsedSuccessfully, datFile =>
				{
					if(datFile is not WADFile wadFile){return;}
					wadFile.ProcessScripts();
				});
			}

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
				Console.WriteLine("--Converting to OSE--");
				var convertedWads = new List<WADFile>();
				RunOnFiles(parsedSuccessfully, datFile =>
				{
					if(datFile is not WADFile wadFile){return;}
					//TODO: Convert
				});

				Console.WriteLine("--Writing WADs--");
				RunOnFiles(convertedWads, wadFile =>
				{
					wadFile.Write(args, conf);
				});
			}
		}
		private static void RunOnFiles<T>(IReadOnlyList<T> files, Action<T> action) where T:DATFile
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