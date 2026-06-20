using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadChunks;

namespace ArgonautReverse.Files
{
	public static class DIR_DAT
	{
		public static DATFile ParseDatFile(Configuration conf, string name, byte[] data)
		{
			var suffix = Path.GetExtension(name)[1..];
			var stem = Path.ChangeExtension(name, null);

			return DATFileType.ParseDatFile(conf, stem, suffix, data);
		}

		private static void FindDirDatFiles(string inputPath, Configuration conf, out string dirPath, out string datPath)
		{
			if(Directory.Exists(inputPath))
			{
				if(conf.ReadVersion.FilenameDIR == null)
				{
					throw new Exception("ReadVersion doesn't have known DIR file.");
				}
				dirPath = Path.Combine(inputPath, conf.ReadVersion.FilenameDIR);
				datPath = Path.Combine(inputPath, conf.ReadVersion.FilenameDAT);
			}
			else if(File.Exists(inputPath))
			{
				if(string.Equals(Path.GetExtension(inputPath), ".DIR", StringComparison.OrdinalIgnoreCase))
				{
					dirPath = inputPath;
					datPath = Path.ChangeExtension(inputPath, ".DAT");
				}
				else
				{
					dirPath = Path.ChangeExtension(inputPath, ".DIR");
					datPath = inputPath;
				}
			}
			else
			{
				throw new FileNotFoundException(inputPath);
			}
		}

		public static IReadOnlyList<DATFile> FromDirDat(Configuration conf, string inputPath)
		{
			FindDirDatFiles(inputPath, conf, out var dirPath, out var datPath);
			var files = new List<DATFile>();

			using(var datData = new FileReader(File.OpenRead(datPath)))
			{
				if(dirPath is not null)
				{
					using var dirData = new FileReader(File.OpenRead(dirPath));
					var fileCount = dirData.Read<int>();
					for(int i = 0; i < fileCount; i++)
					{
						conf.ReadVersion.DirFormat!.Unpack(dirData, out var name, out var size, out var start);
						datData.Position = start;
						files.Add(ParseDatFile(conf, name.Trim('\0'), datData.ReadArray<byte>(size)));
					}
				}
			}
			return files;
		}

		public static IReadOnlyList<DATFile> SearchDat(Configuration conf, string datPath)
		{
			if(!File.Exists(datPath))
			{
				throw new FileNotFoundException(datPath);
			}
			var files = new List<DATFile>();

			using(var datData = new FileReader(File.OpenRead(datPath)))
			{
				while(datData.Position < datData.Length)
				{
					var name = datData.Position.ToString("X8");
					var startingPos = datData.Position;
					var size = datData.Read<int>();//Size of data, not including this field
					if(size == 0)
					{
						Console.WriteLine("Empty file found in DAT");
						break;
					}

					// PSX WADs generally start with TPSX but can start with CWAD for compression
					var chunkType = (ChunkType)datData.Read<uint>();
					string suffix;
					if(chunkType == ChunkType.ID_PSX_CWAD || chunkType == ChunkType.ID_PSX_TEXT)
					{
						suffix = "WAD";
					}
					//Demos seem to start with 0
					else if(chunkType == 0)
					{
						suffix = "DEM";
					}
					else
					{
						throw new Exception();
					}
						
					datData.Position = startingPos;
					var data = datData.ReadArray<byte>(size + sizeof(int));//Add in an int32 for the size field

					var padding = Utils.PadIn2048Bytes(datData);
					if(Enumerable.Any(padding, b => b!=0))
					{
						throw new Exception("Not empty padding");
					}

					files.Add(ParseDatFile(conf, $"{startingPos:X8}.{suffix}", data));
				}
			}
			return files;
		}

		public static IReadOnlyList<DATFile> FromFiles(Configuration conf, string[] paths)
		{
			var filePaths = new List<string>();
			foreach(var path in paths)
			{
				if(Directory.Exists(path))
				{
					filePaths.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories).Where(File.Exists));
				}
				else if(File.Exists(path))
				{
					filePaths.Add(path);
				}
				else
				{
					throw new FileNotFoundException();
				}
			}
			var files = new DATFile[filePaths.Count];
			for(int i = 0; i < filePaths.Count; i++)
			{
				var filePath = filePaths[i];
				files[i] = ParseDatFile(conf, Path.GetFileName(filePath), File.ReadAllBytes(filePath));
			}
			return files;
		}
		public static void Serialize(IReadOnlyList<DATFile> files, Configuration conf, string outputFolder)
		{
			if(File.Exists(outputFolder))
			{
				throw new Exception($"Not a directory: {outputFolder}");
			}
			else if(!Directory.Exists(outputFolder))
			{
				Directory.CreateDirectory(outputFolder);
			}

			var dirStream = new MemoryStream();
			var datStream = new MemoryStream();

			if(conf.WriteVersion != CROC_1_PS1.DatVersion)
			{
				var dirOutput = new IO.StreamWriter(dirStream, (int)dirStream.Position);
				//TODO: Make this part of DirFormat
				dirOutput.WriteInt32(files.Count);

			}
			foreach(var file in files)
			{
				var writeVersion = conf.WriteVersion!.GetWadVersion(file.Stem);

				var dirOutput = new WadWriter(null, conf, writeVersion, dirStream, (int)dirStream.Position);
				var datOutput = new WadWriter(null, conf, writeVersion, datStream, (int)datStream.Position);

				var start = datOutput.Position;
				file.Serialize(datOutput);
				var size = datOutput.Position - start;
				Utils.PadOut2048Bytes(datOutput);
				conf.WriteVersion.DirFormat!.Pack(dirOutput, file.Name, size, start);
			}
			dirStream.CopyTo(File.OpenWrite(Path.Join(outputFolder, conf.WriteVersion!.FilenameDIR)));
			datStream.CopyTo(File.OpenWrite(Path.Join(outputFolder, conf.WriteVersion.FilenameDAT)));
		}
	}
}