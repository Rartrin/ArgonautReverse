using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadChunks;

namespace ArgonautReverse.Files
{
	public sealed class DIR_DAT(IReadOnlyList<DATFile> files)
	{
		public readonly IReadOnlyList<DATFile> Files = files;

		public static DATFile ParseDatFile(Configuration conf, string name, byte[] data)
		{
			var suffix = Path.GetExtension(name)[1..];
			var stem = Path.ChangeExtension(name, null);

			return DATFileType.ParseDatFile(conf, stem, suffix, data);
		}

		private static void FindDirDatFiles(string inputPath, Configuration conf, out string? dirPath, out string datPath)
		{
			if(Directory.Exists(inputPath))
			{
				// CROC 2 DEMO DUMMY file has no .DIR file
				if(conf.ReadVersion.FilenameDIR != null)
				{
					dirPath = Path.Combine(inputPath, conf.ReadVersion.FilenameDIR);
				}
				else
				{
					dirPath = null;
				}
				datPath = Path.Combine(inputPath, conf.ReadVersion.FilenameDAT!);
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
					if(conf.ReadVersion.DirFormat != null)
					{
						dirPath = Path.ChangeExtension(inputPath, ".DIR");
					}
					else
					{
						dirPath = null;
					}
					datPath = inputPath;
				}
			}
			else
			{
				throw new FileNotFoundException(inputPath);
			}
		}

		public static DIR_DAT FromDirDat(Configuration conf, string inputPath)
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
				else// Croc 2 Demo DUMMY
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

						// WADs generally start with TPS but can start with CWAD for compression
						var chunkType = (ChunkType)datData.Read<uint>();
						string suffix;
						if(chunkType == ChunkType.ID_PSX_CWAD || chunkType == ChunkType.ID_PSX_TEXT)
						{
							suffix = ".WAD";
						}
						//Demos seem to start with 0
						else if(chunkType == 0)
						{
							suffix = ".DEM";
						}
						else
						{
							throw new Exception();
						}

						datData.Position = startingPos;
						var data = datData.ReadArray<byte>(size + sizeof(int));//Add in an int32 for the size field

						Utils.PadIn2048Bytes(datData);
						files.Add(ParseDatFile(conf, name + suffix, data));
					}
				}
			}
			return new DIR_DAT(files);
		}

		public static DIR_DAT FromFiles(Configuration conf, string[] paths)
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
			return new DIR_DAT(files);
		}
		public void Serialize(Configuration conf, string outputFolder)
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
				dirOutput.WriteInt32(Files.Count);

			}
			foreach(var file in Files)
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