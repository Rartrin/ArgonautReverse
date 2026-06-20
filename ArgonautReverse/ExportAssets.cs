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
		public bool ExtractScripts = false;
					
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
		public readonly int OperandCount;
		public readonly Action<ProgramArgs,ReadOnlySpan<string>> Apply;

		public Option(Action<ProgramArgs> apply)
		{
			OperandCount = 0;
			Apply = (args, rawArgs) => apply(args);
		}

		public Option(Action<ProgramArgs,string> apply)
		{
			OperandCount = 1;
			Apply = (args, rawArgs) => apply(args, rawArgs[0]);
		}
	}
	public static class ExportAssets
	{
		private static readonly Dictionary<string,Option> programOptions = new()
		{
			["--read-format"] = new((args, raw) => args.ReadFormat = raw),
			["--read-psx-dirdat"] = new((args, raw) => args.PsxDirDat = raw),
			["--read-wads"] = new((args, raw) => args.ReadWads = raw),
			["--write-format"] = new((args, raw) => args.WriteFormat = raw),
			["--write-wads"] = new((args, raw) => args.WriteWads = raw),
			["--extract-path"] = new((args, raw) => args.ExtractPath = raw),
			["--extract"] = new((args, raw) =>
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
			}),
			["--ignore-warnings"] = new((args) => args.IgnoreWarnings = true),
			["--help"] = new((args)=>
			{
				//TODO: Available options.
			})
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
			if(args == null){return;}

			var readFormat = Configuration.SliceableGames[args.ReadFormat];
			var writeFormat = args.WriteFormat!=null ? Configuration.SliceableGames[args.WriteFormat] : null;
			if(!Configuration.AllParsableGames.Contains(readFormat))
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