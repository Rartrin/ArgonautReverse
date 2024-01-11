using ArgonautReverse.Engine.Versions;
using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse
{
	public sealed class DIR_DAT
	{
		public IReadOnlyList<DATFile> Files{get;}

		public static DATFile parse_dat_file(string name, byte[] data)
		{
			var suffix = Path.GetExtension(name)[1..];
			var stem = Path.ChangeExtension(name, null);

			var dat_class = DATFileType.guess_dat_file_type(stem, suffix).file_class;
			if(dat_class is not null)
			{
				return (DATFile)Activator.CreateInstance(dat_class, stem, suffix, data);
			
			}
			else
			{
				throw new Exception();
				//return new DATFile(stem, suffix, data);
			}
		}

		public DIR_DAT(IReadOnlyList<DATFile> files)
		{
			Files = files;
		}
	
		public static (string dir_path,string dat_path) find_dir_dat_files(string input_path, Configuration conf)
		{
			string dir_path;
			string dat_path;
			if(Directory.Exists(input_path))
			{
				// CROC 2 DEMO DUMMY file has no .DIR file
				dir_path = conf.InputVersion != CROC_2_DEMO_PS1_DUMMY.Instance ? Path.Combine(input_path, conf.InputVersion.FilenameDIR) : null;
				dat_path = Path.Combine(input_path, conf.InputVersion.FilenameDAT);
			}
			else if(File.Exists(input_path))
			{
				if(Path.GetExtension(input_path) == ".DIR")
				{
					dir_path = input_path;
					dat_path = Path.ChangeExtension(input_path, ".DAT");
				}
				else
				{
					if(conf.InputVersion.DirFormat != null)
					{
						dir_path = Path.ChangeExtension(input_path, ".DIR");
					}
					else
					{
						dir_path = null;
					}
					dat_path = input_path;
				}
			}
			else
			{
				throw new FileNotFoundException(input_path);
			}
			return (dir_path, dat_path);
		}

		public static DIR_DAT FromDirDat(string input_path, Configuration conf)
		{
			var (dir_path, dat_path) = find_dir_dat_files(input_path, conf);
			var files = new List<DATFile>();

			using(var dat_data = new WadReader(conf, File.OpenRead(dat_path)))
			{
				if(dir_path is not null)
				{
					using var dir_data = new WadReader(conf, File.OpenRead(dir_path));
					var n_files = dir_data.ReadInt32();
					for(int i=0; i<n_files; i++)
					{
						conf.InputVersion.DirFormat.Unpack(dir_data, out var name, out var size, out var start);
						dat_data.Seek(start, SeekOrigin.Begin);
						files.Add(parse_dat_file(name.Trim('\0'), dat_data.ReadBytes(size)));
					}
				}
				else// Croc 2 Demo DUMMY
				{
					while(dat_data.Position<dat_data.Length)
					{
						var name = dat_data.Position.ToString("X8");
						var startingPos = dat_data.Position;
						//var size_bytes = dat_data.ReadBytes(4);
						//var size = int.from_bytes(size_bytes, "little");
						var size = dat_data.ReadInt32();
						if(size == 0)
						{
							break;
						}
						// WADs start with the 'XSPT' codename
						var codename = dat_data.ReadInt32();
						var suffix = (codename == TPSXSectionInfo.Instance.codename_raw) ? ".WAD" : ".DEM";
					
						//dat_data.BaseStream.Position -= 4;
						//var data = dat_data.ReadBytes(size - 4);
						dat_data.Position = startingPos;
						var data = dat_data.ReadBytes(size);

						Utils.pad_in_2048_bytes(dat_data);
						files.Add(parse_dat_file(name + suffix, /*size_bytes +*/ data));
					}
				}
			}
			return new DIR_DAT(files);
		}

		public static DIR_DAT FromFiles(params string[] paths)
		{
			var filePaths = new List<string>();
			foreach(var path in paths)
			{
				if(Directory.Exists(path))
				{
					filePaths.AddRange(Directory.GetFiles(path, "*",SearchOption.AllDirectories).Where(File.Exists));
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
			for(int i=0; i<filePaths.Count; i++)
			{
				var filePath = filePaths[i];
				files[i] = parse_dat_file(Path.GetFileName(filePath), File.ReadAllBytes(filePath));
			}
			return new DIR_DAT(files);
		}
		public void Serialize(string output_folder, Configuration conf)
		{
			using var dir_output = new Serializer(File.OpenWrite(Path.Join(output_folder, conf.InputVersion.FilenameDIR)));
			using var dat_output = new Serializer(File.OpenWrite(Path.Join(output_folder, conf.InputVersion.FilenameDAT)));

			if(File.Exists(output_folder))
			{
				throw new Exception($"Not a directory: {output_folder}");
			}
			else if(!Directory.Exists(output_folder))
			{
				Directory.CreateDirectory(output_folder);
			}
			if(conf.InputVersion != CROC_1_PS1.Instance)
			{
				dir_output.WriteInt32(Files.Count);
			}
			foreach(var file in this.Files)
			{
				var start = dat_output.Position;
				file.Serialize(dat_output, conf);
				var size = dat_output.Position - start;
				Utils.pad_out_2048_bytes(dat_output);
				conf.InputVersion.DirFormat.Pack(dir_output, file.Name, size, start);
			}
		}
	}
}