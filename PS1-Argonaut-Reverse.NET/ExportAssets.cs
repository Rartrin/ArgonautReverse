using System.Drawing.Imaging;
using ArgonautReverse.Files;
using ArgonautReverse.WadSections.DPSX;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse
{
	public static class ExportAssets
	{
		public static Dictionary<string,string> parse_args(string[] args)
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
					case "-strats"://Output path for strats
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

		public static void create_export_directory(string path)
		{
			if(File.Exists(path))
			{
				throw new Exception();
			}
			else if(!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);//(parents=True)
			}
		}

		public static void export_images_from_img(IMGFile img_file, string output_dir)
		{
			if(img_file.Count == 1)
			{
				img_file[0].Save(Path.Join(output_dir, $"{img_file.stem}.PNG"));
			}
			else
			{
				for(int i=0; i<img_file.Count; i++)
				{
					img_file[i].Save(Path.Join(output_dir, $"{img_file.stem}_{i}.PNG"));
				}
			}
		}

		public static void export_assets_from_wad(WADFile wad_file, Dictionary<string,string> args, Configuration conf)
		{
			if(TPSXSectionInfo.Instance.supported_games.Contains(conf.game))
			{
				if(args.TryGetValue("-textures", out var export_textures))
				{
					wad_file.tpsx.TextureFile.to_colorized_texture().Save(Path.Join(export_textures, $"{wad_file.stem}.PNG"), ImageFormat.Png);
				}
			}

			//TODO: Sound
			if(false)//if(SPSXSectionInfo.Instance.supported_games.Contains(conf.game))
			{
				if(args.TryGetValue("-audio", out var export_audio))
				{
					var wad_audio_export_folder_path = Path.Join(export_audio, wad_file.stem);
					create_export_directory(wad_audio_export_folder_path);
					wad_file.export_audio_to_wav(wad_audio_export_folder_path, wad_file.stem);
				}
				if(args.TryGetValue("-unpack_audio", out var unpack_audio))
				{
					var wad_audio_unpack_folder_path = Path.Join(unpack_audio, wad_file.stem);
					create_export_directory(wad_audio_unpack_folder_path);
					wad_file.export_audio_to_vag(wad_audio_unpack_folder_path, wad_file.stem);
				}
			}
			if(DPSXSectionInfo.Instance.supported_games.Contains(conf.game))
			{
				if(args.TryGetValue("-strats", out var export_strats))
				{
					var wad_strats_folder_path = Path.Join(export_strats, wad_file.stem);
					create_export_directory(wad_strats_folder_path);
					wad_file.ExportStrats(wad_strats_folder_path, wad_file.stem);
				}
				if(args.TryGetValue("-models", out var export_models))
				{
					var wad_models_3d_folder_path = Path.Join(export_models, wad_file.stem);
					create_export_directory(wad_models_3d_folder_path);
					wad_file.export_experimental_models(wad_models_3d_folder_path, wad_file.stem);
				}
				if(args.TryGetValue("-levels", out var export_levels))
				{
					var wad_level_folder_path = Path.Join(export_levels, wad_file.stem);
					create_export_directory(wad_level_folder_path);
					wad_file.export_level(wad_level_folder_path, wad_file.stem);
				}
			}
		}

		public static void export_assets(Dictionary<string,string> args)
		{
			var game = Configuration.SUPPORTED_GAMES.SingleOrDefault(g => g.Title == args["-game"]);
			if(!Configuration.PARSABLE_GAMES.Contains(game))
			{
				throw new NotImplementedException("Files from this game can be extracted, but not reversed (yet). If you just want to extract them, use the extract_files_from_dat.py script.");
			};

			var conf = new Configuration(game, args.ContainsKey("-ignore_warnings"));

			DIR_DAT dir_dat;
			if(args.TryGetValue("-dirdat", out var dirdat))
			{
				dir_dat = DIR_DAT.from_dir_dat(dirdat, conf);
			}
			else// args.files
			{
				dir_dat = DIR_DAT.from_files(args["-files"].Split(','));
			}

			var export_paths = new string[]
			{
				args.GetValueOrDefault("-images"),
				args.GetValueOrDefault("-textures"),
				args.GetValueOrDefault("-models"),
				args.GetValueOrDefault("-audio"),
				args.GetValueOrDefault("-unpack_audio"),
				args.GetValueOrDefault("-levels"),
			};
			bool wads_parsing_needed =
			(
				args.ContainsKey("-textures") ||
				args.ContainsKey("-models") ||
				args.ContainsKey("-audio") ||
				args.ContainsKey("-unpack_audio") ||
				args.ContainsKey("-levels")
			);
			foreach(var export_part in export_paths)
			{
				if(export_part != null)
				{
					create_export_directory(export_part);
				}
			}
		
			//Parse files
			var n_files = dir_dat.Count;
			var n_digits = n_files.ToString().Length;
			for(int i=0; i<dir_dat.Count; i++)// type: int, DATFile
			{
				var dat_file = dir_dat[i];
				Console.Write($"[{(i + 1).ToString().PadLeft(n_digits)}/{n_files}] {dat_file.name:12}: ");
				try
				{
					if(dat_file is IMGFile && args.TryGetValue("-images", out var export_images))
					{
						dat_file.parse(conf);
						export_images_from_img((IMGFile)dat_file, export_images);
					}
					else if(dat_file is WADFile && wads_parsing_needed)
					{
						dat_file.parse(conf);
						export_assets_from_wad((WADFile)dat_file, args, conf);
					}
					Console.WriteLine(dat_file);
				}
				catch(Exception e)
				{
					Console.WriteLine("FAILED!");
					Console.WriteLine(e.ToString());
				}
				Console.WriteLine();
			}
		}

		public static void Run(string[] args)
		{
			var parsedArgs = parse_args(args);
			if(parsedArgs.ContainsKey("-export_models") && !parsedArgs.ContainsKey("--no_confirm"))
			{
				Console.WriteLine("Models export is VERY EXPERIMENTAL, some models will be completely broken or even missing.");
				Console.WriteLine("Press <Enter> to continue.");
				Console.ReadLine();
			}

			export_assets(parsedArgs);

			Console.WriteLine("No error encountered.");
		}
	}
}