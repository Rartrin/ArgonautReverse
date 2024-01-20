//using ArgonautReverse.Files;
//using ArgonautReverse.IO;

//namespace ArgonautReverse
//{
//	public static class ExtractFilesFromData
//	{
//		public static void Run(string[] args)
//		{
//			//parser = argparse.ArgumentParser(
//			//    description="Utility to extract WAD files from PS1 Argonaut games like Croc 2 or Harry Potter. By OverSurge."
//			//)
//			//parser.add_argument(
//			//    "game",
//			//    type=str,
//			//    choices=[game.title for game in Configuration.SUPPORTED_GAMES],
//			//    help="The game the files are from. If it is not listed, choose one you think is the closest.",
//			//)
//			//parser.add_argument("dirdat", type=str, help="Where the DIR/DAT files are located.")
//			//parser.add_argument("output_dir", type=str, help="Where to extract the WADs.")
//			//args = parser.parse_args()
//			var parsedArgs = new Dictionary<string,string>();
//			for(int i= 0; i < args.Length; i++)
//			{
//				switch(args[i])
//				{
//					case "-extract-files":continue;
//					case "-dirdat":
//					case "-game":
//					case "-output_dir":
//					{
//						parsedArgs[args[i]] = args[i+1];
//						i++;
//						break;
//					}
//					default:throw new Exception("Unknown args: "+args[i]);
//				}
//			}
//			Extract(parsedArgs);
//		}

//		public static void Extract(Dictionary<string,string> args)
//		{
//			var game = Configuration.SUPPORTED_GAMES.Single(game => game.Title == args["-game"]);
//			var conf = new Configuration(game, true);

//			var input_path = args["-dirdat"];
//			var outputPath = args["-output_dir"];

//			if(File.Exists(outputPath))
//			{
//				throw new Exception("output is a file, not a directory");
//			}

//			var dir_dat = DIR_DAT.FromDirDat(input_path, conf);

//			if(!Directory.Exists(outputPath))
//			{
//				Directory.CreateDirectory(outputPath);
//			}
//			foreach(var datFile in dir_dat.Files)
//			{
//				using var data_out = new Serializer(File.OpenWrite(Path.Join(outputPath, datFile.Name)));
//				datFile.Serialize(data_out, conf);
//			}
//			Console.Write($"{dir_dat.Files.Count} files successfully extracted to {outputPath}");
//		}
//	}
//}