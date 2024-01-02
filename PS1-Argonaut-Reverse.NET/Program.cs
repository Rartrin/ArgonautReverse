namespace ArgonautReverse
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			switch(args[0])
			{
				case "-export-assets":ExportAssets.Run(args);break;
				case "-extract-files":ExtractFilesFromData.Run(args);break;
			}
		}
	}
}
