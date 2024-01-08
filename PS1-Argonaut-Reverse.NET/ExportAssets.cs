using System.Drawing.Imaging;
using ArgonautReverse.Files;
using ArgonautReverse.WadSections.DPSX;
using ArgonautReverse.WadSections.SPSX;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse
{
	public static class ExportAssets
	{
		public static Dictionary<string,string> ParseArgs(string[] args)
		{
			var parsedArgs = new Dictionary<string,string>();
			for(int i=0; i<args.Length; i++)
			{
				switch(args[i])
				{
					case "-export-assets":continue;
					case "-game":
					case "-dirdat"://Path to DIR/DAT files
					case "-files"://Path to WAD files
					case "-textures"://Output path for textures
					case "-models"://Output path for models
					case "-audio"://Output path for WAV audio
					case "-unpack-audio"://Output path for unpacked PS1 WAG audio
					case "-levels"://Output path for level's model geometry
					case "-images"://Output path for images
					case "-actors"://Output path for actors
						parsedArgs[args[i]] = args[++i];
						break;
					case "-v":
					case "--ignore-warnings":
					case "--no-confirm":
						parsedArgs[args[i]] = "true";
						break;
				}
			}
			return parsedArgs;
		}

		public static void CreateExportDirectory(string path)
		{
			if(File.Exists(path))
			{
				throw new Exception();
			}
			else if(!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		public static void ExportImagesFromImg(IMGFile img_file, string output_dir)
		{
			if(img_file.Images.Count == 1)
			{
				img_file.Images[0].Save(Path.Join(output_dir, $"{img_file.Stem}.PNG"));
			}
			else
			{
				for(int i=0; i<img_file.Images.Count; i++)
				{
					img_file.Images[i].Save(Path.Join(output_dir, $"{img_file.Stem}_{i}.PNG"));
				}
			}
		}

		public static void ExportAssetsFromWad(WADFile wad_file, Dictionary<string,string> args, Configuration conf)
		{
			if(TPSXSectionInfo.Instance.supported_games.Contains(conf.game))
			{
				if(args.TryGetValue("-textures", out var export_textures))
				{
					wad_file.tpsx.TextureFile.to_colorized_texture().Save(Path.Join(export_textures, $"{wad_file.Stem}.PNG"), ImageFormat.Png);
				}
			}

			if(SPSXSectionInfo.Instance.supported_games.Contains(conf.game))
			{
				if(args.TryGetValue("-audio", out var export_audio))
				{
					var wad_audio_export_folder_path = Path.Join(export_audio, wad_file.Stem);
					CreateExportDirectory(wad_audio_export_folder_path);
					wad_file.export_audio_to_wav(wad_audio_export_folder_path, wad_file.Stem);
				}
				if(args.TryGetValue("-unpack_audio", out var unpack_audio))
				{
					var wad_audio_unpack_folder_path = Path.Join(unpack_audio, wad_file.Stem);
					CreateExportDirectory(wad_audio_unpack_folder_path);
					wad_file.export_audio_to_vag(wad_audio_unpack_folder_path, wad_file.Stem);
				}
			}
			if(DPSXSectionInfo.Instance.supported_games.Contains(conf.game))
			{
				if(args.TryGetValue("-actors", out var export_actors))
				{
					var wad_actors_folder_path = Path.Join(export_actors, wad_file.Stem);
					CreateExportDirectory(wad_actors_folder_path);
					wad_file.ExportActors(wad_actors_folder_path, wad_file.Stem);
				}
				if(args.TryGetValue("-models", out var export_models))
				{
					var wad_models_3d_folder_path = Path.Join(export_models, wad_file.Stem);
					CreateExportDirectory(wad_models_3d_folder_path);
					wad_file.export_experimental_models(wad_models_3d_folder_path, wad_file.Stem);
				}
				if(args.TryGetValue("-levels", out var export_levels))
				{
					var wad_level_folder_path = Path.Join(export_levels, wad_file.Stem);
					CreateExportDirectory(wad_level_folder_path);
					wad_file.export_level(wad_level_folder_path, wad_file.Stem);
				}
			}
		}

		public static void Run(string[] args)
		{
			var parsedArgs = ParseArgs(args);
			if(parsedArgs.ContainsKey("-export_models") && !parsedArgs.ContainsKey("--no_confirm"))
			{
				Console.WriteLine("Models export is VERY EXPERIMENTAL, some models will be completely broken or even missing.");
				Console.WriteLine("Press <Enter> to continue.");
				Console.ReadLine();
			}

			var game = Configuration.SUPPORTED_GAMES.SingleOrDefault(g => g.Title == parsedArgs["-game"]);
			if(!Configuration.PARSABLE_GAMES.Contains(game))
			{
				throw new NotImplementedException("Files from this game can be extracted, but not reversed (yet). If you just want to extract them, use the extract_files_from_dat.py script.");
			};

			var conf = new Configuration(game, parsedArgs.ContainsKey("-ignore_warnings"));

			DIR_DAT dir_dat;
			if(parsedArgs.TryGetValue("-dirdat", out var dirdat))
			{
				dir_dat = DIR_DAT.FromDirDat(dirdat, conf);
			}
			else if(parsedArgs.TryGetValue("-files", out var files))
			{
				dir_dat = DIR_DAT.FromFiles(files.Split(','));
			}
			else
			{
				Console.WriteLine("Missing either -dirdat or -files");
				return;
			}

			var export_paths = new string[]
			{
				parsedArgs.GetValueOrDefault("-images"),
				parsedArgs.GetValueOrDefault("-textures"),
				parsedArgs.GetValueOrDefault("-models"),
				parsedArgs.GetValueOrDefault("-audio"),
				parsedArgs.GetValueOrDefault("-unpack_audio"),
				parsedArgs.GetValueOrDefault("-levels"),
			};
			bool wads_parsing_needed =
			(
				parsedArgs.ContainsKey("-textures") ||
				parsedArgs.ContainsKey("-models") ||
				parsedArgs.ContainsKey("-audio") ||
				parsedArgs.ContainsKey("-unpack_audio") ||
				parsedArgs.ContainsKey("-levels")
			);
			foreach(var export_part in export_paths)
			{
				if(export_part != null)
				{
					CreateExportDirectory(export_part);
				}
			}
		
			//Parse files
			var n_files = dir_dat.Files.Count;
			var n_digits = n_files.ToString().Length;
			for(int i=0; i<dir_dat.Files.Count; i++)
			{
				var datFile = dir_dat.Files[i];
				Console.Write($"[{(i + 1).ToString().PadLeft(n_digits)}/{n_files}] {datFile.Name:12}: ");
				try
				{
					if(datFile is IMGFile imgFile && parsedArgs.TryGetValue("-images", out var export_images))
					{
						imgFile.Parse(conf);
						ExportImagesFromImg(imgFile, export_images);
					}
					else if(datFile is WADFile wadFile && wads_parsing_needed)
					{
						wadFile.Parse(conf);
						ExportAssetsFromWad(wadFile, parsedArgs, conf);
					}
					Console.WriteLine(datFile);
				}
				catch(Exception e)
				{
					Console.WriteLine("FAILED!");
					Console.WriteLine(e.ToString());
				}
				Console.WriteLine();
			}

			Console.WriteLine("No error encountered.");
		}
	}
}