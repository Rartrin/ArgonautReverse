namespace ArgonautReverse
{
	public static class ExtractFilesFromData
	{
		public static void Run(string[] args)
		{
			//parser = argparse.ArgumentParser(
			//    description="Utility to extract WAD files from PS1 Argonaut games like Croc 2 or Harry Potter. By OverSurge."
			//)
			//parser.add_argument(
			//    "game",
			//    type=str,
			//    choices=[game.title for game in Configuration.SLICEABLE_GAMES],
			//    help="The game the files are from. If it is not listed, choose one you think is the closest.",
			//)
			//parser.add_argument("dirdat", type=str, help="Where the DIR/DAT files are located.")
			//parser.add_argument("output_dir", type=str, help="Where to extract the WADs.")
			//args = parser.parse_args()
			var parsedArgs = new Dictionary<string,string>();
			for(int i= 0; i < args.Length; i++)
			{
				switch(args[i])
				{
					case "-extract-files":continue;
					case "-dirdat":
					case "-game":
					case "-output_dir":
					{
						parsedArgs[args[i]] = args[i+1];
						i++;
						break;
					}
					default:throw new Exception("Unknown args: "+args[i]);
				}
			}
			Extract(parsedArgs);
		}

		public static void Extract(Dictionary<string,string> args)
		{
			var game = Configuration.SUPPORTED_GAMES.Single(game => game.Title == args["-game"]);
			var conf = new Configuration(game, true, false);

			var input_path = args["-dirdat"];
			var output_path = args["-output_dir"];

			if(File.Exists(output_path))
			{
				throw new Exception("output is a file, not a directory");
			}

			var dir_dat = DIR_DAT.from_dir_dat(input_path, conf);

			if(!Directory.Exists(output_path))
			{
				Directory.CreateDirectory(output_path);
			}
			foreach(var dat_file in dir_dat)
			{
				dat_file.serialize(Path.Join(output_path, dat_file.name), conf);
			}
			Console.Write($"{dir_dat.Count} files successfully extracted to {output_path}");
		}
	}
}