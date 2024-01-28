using System.Drawing.Imaging;
using ArgonautReverse.Files;
using ArgonautReverse.WadChunks.PSX;

namespace ArgonautReverse
{
    public static class ExportAssets
	{
		public sealed class ProgramArgs
		{
			public string ReadFormat;
			public string WriteFormat;

			public string PsxDirDat;//Path to PSX DIR/DAT files
			public string WadFiles;//Path to WAD files or directories containing WADs

			public string ExportTextures;//Output path for textures
			public string ExportModels;//Output path for models
			public string ExportAudio;//Output path for WAV audio
			public string UnpackAudio;//Output path for unpacked PS1 WAG audio
			public string ExportLevels;//Output path for level's model geometry
			public string ExportImages;//Output path for images
			public string ExportActors;//Output path for actors
					
			public bool IgnoreWarnings;
			public bool NoConfirm;
		}
		public static ProgramArgs ParseArgs(string[] args)
		{
			var parsedArgs = new ProgramArgs();
			for(int i=0; i<args.Length; i++)
			{
				switch(args[i])
				{
					case "--read-format":parsedArgs.ReadFormat = args[++i];break;
					case "--write-format":parsedArgs.WriteFormat = args[++i];break;

					case "--psx-dirdat":parsedArgs.PsxDirDat = args[++i];break;
					case "--wad-files":parsedArgs.WadFiles = args[++i];break;

					case "--export-textures":parsedArgs.ExportTextures = args[++i];break;
					case "--export-models":parsedArgs.ExportModels = args[++i];break;
					case "--export-audio":parsedArgs.ExportAudio = args[++i];break;
					case "--unpack-audio":parsedArgs.UnpackAudio = args[++i];break;
					case "--export-levels":parsedArgs.ExportLevels = args[++i];break;
					case "--export-images":parsedArgs.ExportImages = args[++i];break;
					case "--export-actors":parsedArgs.ExportActors = args[++i];break;

					case "--ignore-warnings":parsedArgs.IgnoreWarnings = true;break;
					case "--no-confirm":parsedArgs.NoConfirm = true;break;

					default:throw new Exception("Unknown argument: " + args[i]);
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

		public static void ExportAssetsFromWad(WADFile wadFile, ProgramArgs args, Configuration conf)
		{
			if(TPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any())
			{
				if(args.ExportTextures is string exportTextures)
				{
					wadFile.TPSX.TextureFile.to_colorized_texture().Save(Path.Join(exportTextures, $"{wadFile.Stem}.PNG"), ImageFormat.Png);
				}
			}

			if(SPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any())
			{
				if(args.ExportAudio is string exportAudio)
				{
					var wad_audio_export_folder_path = Path.Join(exportAudio, wadFile.Stem);
					CreateExportDirectory(wad_audio_export_folder_path);
					wadFile.export_audio_to_wav(wad_audio_export_folder_path, wadFile.Stem);
				}
				if(args.UnpackAudio is string unpackAudio)
				{
					var wad_audio_unpack_folder_path = Path.Join(unpackAudio, wadFile.Stem);
					CreateExportDirectory(wad_audio_unpack_folder_path);
					wadFile.export_audio_to_vag(wad_audio_unpack_folder_path, wadFile.Stem);
				}
			}
			if(DPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any())
			{
				if(args.ExportActors is string exportActors)
				{
					var wad_actors_folder_path = Path.Join(exportActors, wadFile.Stem);
					CreateExportDirectory(wad_actors_folder_path);
					wadFile.ExportActors(wad_actors_folder_path, wadFile.Stem);
				}
				if(args.ExportModels is string exportModels)
				{
					var wad_models_3d_folder_path = Path.Join(exportModels, wadFile.Stem);
					CreateExportDirectory(wad_models_3d_folder_path);
					wadFile.export_experimental_models(wad_models_3d_folder_path, wadFile.Stem);
				}
				if(args.ExportLevels is string exportLevels)
				{
					var wad_level_folder_path = Path.Join(exportLevels, wadFile.Stem);
					CreateExportDirectory(wad_level_folder_path);
					wadFile.export_level(wad_level_folder_path, wadFile.Stem);
				}
			}
		}

		public static void Run(string[] args)
		{
			var parsedArgs = ParseArgs(args);
			if(parsedArgs.ExportModels != null && !parsedArgs.NoConfirm)
			{
				Console.WriteLine("Models export is VERY EXPERIMENTAL, some models will be completely broken or even missing.");
				Console.WriteLine("Press <Enter> to continue.");
				Console.ReadLine();
			}

			var readFormat = Configuration.SUPPORTED_GAMES.SingleOrDefault(g => g.Title == parsedArgs.ReadFormat);
			var writeFormat = Configuration.SUPPORTED_GAMES.SingleOrDefault(g => g.Title == parsedArgs.WriteFormat);
			if(!Configuration.ALL_PARSABLE_GAMES.Contains(readFormat))
			{
				throw new NotImplementedException("Files from this game can be extracted, but not reversed (yet). If you just want to extract them, use the extract_files_from_dat.py script.");
			}

			var conf = new Configuration(readFormat, writeFormat, parsedArgs.IgnoreWarnings);

			DIR_DAT dir_dat;
			if(parsedArgs.PsxDirDat is string dirdat)
			{
				dir_dat = DIR_DAT.FromDirDat(conf, dirdat);
			}
			else if(parsedArgs.WadFiles is string files)
			{
				dir_dat = DIR_DAT.FromFiles(conf, files.Split(','));
			}
			else
			{
				Console.WriteLine("Missing either -dirdat or -files");
				return;
			}

			var export_paths = new string[]
			{
				parsedArgs.ExportImages,
				parsedArgs.ExportTextures,
				parsedArgs.ExportModels,
				parsedArgs.ExportAudio,
				parsedArgs.UnpackAudio,
				parsedArgs.ExportLevels,
			};
			bool wads_parsing_needed = true ||
			(
				parsedArgs.ExportTextures!=null ||
				parsedArgs.ExportModels!=null ||
				parsedArgs.ExportAudio!=null ||
				parsedArgs.UnpackAudio!=null ||
				parsedArgs.ExportLevels!=null
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
				Console.WriteLine($"[{(i + 1).ToString().PadLeft(n_digits)}/{n_files}] {datFile.Name:12}: ");
				try
				{
					if(datFile is IMGFile imgFile && parsedArgs.ExportImages is string export_images)
					{
						imgFile.Parse(conf);
						ExportImagesFromImg(imgFile, export_images);
					}
					else if(datFile is WADFile wadFile && wads_parsing_needed)
					{
						wadFile.Parse(conf);
						ExportAssetsFromWad(wadFile, parsedArgs, conf);
					}
					datFile.PrintInfo(Console.Out);
				}
				catch(Exception e)
				{
					Console.WriteLine("FAILED!");
					Console.WriteLine(e.ToString());
				}
				Console.WriteLine();
			}
		}
	}
}