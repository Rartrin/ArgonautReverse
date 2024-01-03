using System.Collections;
using ArgonautReverse.Files;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse
{
	public sealed class DIR_DAT:IReadOnlyList<DATFile>
	{
		public int Count => list.Count;

		public DATFile this[int index] => list[index];

		private readonly List<DATFile> list = new List<DATFile>();

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

		public DIR_DAT(IEnumerable<DATFile> files = null)
		{
			if(files != null)
			{
				list.AddRange(files);
			}
		}
	
		public static (string dir_path,string dat_path) find_dir_dat_files(string input_path, Configuration conf)
		{
			string dir_path;
			string dat_path;
			if(Directory.Exists(input_path))
			{
				// CROC 2 DEMO DUMMY file has no .DIR file
				dir_path = conf.game != G.CROC_2_DEMO_PS1_DUMMY ? Path.Combine(input_path, conf.game.FilenameDIR) : null;
				dat_path = Path.Combine(input_path, conf.game.FilenameDAT);
			}
			else if(File.Exists(input_path))
			{
				if(conf.game != G.CROC_2_DEMO_PS1_DUMMY)
				{
					if(Path.GetExtension(input_path) == ".DIR")
					{
						dir_path = input_path;
						dat_path = Path.ChangeExtension(input_path, ".DAT");
					}
					else
					{
						dir_path = Path.ChangeExtension(input_path, ".DIR");
						dat_path = input_path;
					}
				}
				else
				{
					dir_path = null;
					dat_path = input_path;
				}
			}
			else
			{
				throw new FileNotFoundException(input_path);
			}
			return (dir_path, dat_path);
		}

		public static DIR_DAT from_dir_dat(string input_path, Configuration conf)
		{
			var (dir_path, dat_path) = find_dir_dat_files(input_path, conf);
			var files = new List<DATFile>();

			using(var dat_data = new Parser(File.OpenRead(dat_path)))
			{
				if(dir_path is not null)
				{
					using var dir_data = new Parser(File.OpenRead(dir_path));
					var n_files = dir_data.ReadInt32();
					for(int i=0; i<n_files; i++)
					{
						var (name, size, start) = conf.game.UnpackDIR(dir_data);
						dat_data.Seek(start, SeekOrigin.Begin);
						files.Add(parse_dat_file(name.Trim('\0')/*.decode("ASCII")*/, dat_data.ReadBytes(size)));
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

		public static DIR_DAT from_files(params string[] files)
		{
			var all_files = new List<string>();
			foreach(var file in files)
			{
				if(Directory.Exists(file))
				{
					//all_files.AddRange(file for file in file.rglob("*") if file.is_file());
					throw new NotImplementedException();
				}
				else if(File.Exists(file))
				{
					all_files.Add(file);
				}
				else
				{
					throw new FileNotFoundException();
				}
			}
			return new DIR_DAT(all_files.Select(file => parse_dat_file(Path.GetFileName(file), File.ReadAllBytes(file))));
		}
		public void serialize(string output_folder, Configuration conf)
		{
			using var dir_output = new BinaryWriter(new MemoryStream());//BytesIO
			using var dat_output = new BinaryWriter(new MemoryStream());//BytesIO

			if(File.Exists(output_folder))
			{
				throw new Exception($"Not a directory: {output_folder}");
			}
			else if(!Directory.Exists(output_folder))
			{
				Directory.CreateDirectory(output_folder);
				//output_folder.mkdir(parents=True)
			}
			if(conf.game != G.CROC_1_PS1)
			{
				dir_output.Write((int)this.Count);
			}
			foreach(var file in this.list)
			{
				var start = (int)dat_output.BaseStream.Position;
				file.serialize(dat_output, conf);
				var size = (int)(dat_output.BaseStream.Position - start);
				Utils.pad_out_2048_bytes(dat_output.BaseStream);
				conf.game.PackDIR(dir_output, file.name/*.encode("ASCII")*/, size, start);
			}
			using(var dir_file = File.OpenWrite(Path.Join(output_folder, conf.game.FilenameDIR)))
			{
				dir_output.BaseStream.Position = 0;
				dir_output.BaseStream.CopyTo(dir_file);
			}
			using(var dat_file = File.OpenWrite(Path.Join(output_folder, conf.game.FilenameDAT)))
			{
				dat_output.BaseStream.Position = 0;
				dat_output.BaseStream.CopyTo(dat_file);
			}
		}

		public IEnumerator<DATFile> GetEnumerator() => list.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
	}
}