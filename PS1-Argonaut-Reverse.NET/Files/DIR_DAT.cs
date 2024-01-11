using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse.Files
{
    public sealed class DIR_DAT
    {
        public IReadOnlyList<DATFile> Files{get;}

        public static DATFile ParseDatFile(string name, byte[] data)
        {
            var suffix = Path.GetExtension(name)[1..];
            var stem = Path.ChangeExtension(name, null);

            return DATFileType.ParseDatFile(stem, suffix, data);
        }

        public DIR_DAT(IReadOnlyList<DATFile> files)
        {
            Files = files;
        }

        private static (string dir_path, string dat_path) FindDirDatFiles(string inputPath, Configuration conf)
        {
            string dirPath;
            string datPath;
            if (Directory.Exists(inputPath))
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
                datPath = Path.Combine(inputPath, conf.ReadVersion.FilenameDAT);
            }
            else if (File.Exists(inputPath))
            {
                if (Path.GetExtension(inputPath) == ".DIR")
                {
                    dirPath = inputPath;
                    datPath = Path.ChangeExtension(inputPath, ".DAT");
                }
                else
                {
                    if (conf.ReadVersion.DirFormat != null)
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
            return (dirPath, datPath);
        }

        public static DIR_DAT FromDirDat(string inputPath, Configuration conf)
        {
            var (dirPath, datPath) = FindDirDatFiles(inputPath, conf);
            var files = new List<DATFile>();

            using (var datData = new WadReader(conf, File.OpenRead(datPath)))
            {
                if (dirPath is not null)
                {
                    using var dirData = new WadReader(conf, File.OpenRead(dirPath));
                    var fileCount = dirData.ReadInt32();
                    for (int i = 0; i < fileCount; i++)
                    {
                        conf.ReadVersion.DirFormat.Unpack(dirData, out var name, out var size, out var start);
                        datData.Seek(start, SeekOrigin.Begin);
                        files.Add(ParseDatFile(name.Trim('\0'), datData.ReadBytes(size)));
                    }
                }
                else// Croc 2 Demo DUMMY
                {
                    while (datData.Position < datData.Length)
                    {
                        var name = datData.Position.ToString("X8");
                        var startingPos = datData.Position;
                        var size = datData.ReadInt32();
                        if (size == 0)
                        {
                            break;
                        }

                        //TODO: WADs can also start with CWAD which indicates chunk compression

                        // WADs start with TPSX
                        var codename = datData.ReadInt32();
                        string suffix;
                        if(codename == TPSXSectionInfo.Instance.codename_raw)
                        {
                            suffix = ".WAD";
                        }
                        //Demos seem to start with 0
                        else if(codename == 0)
                        {
                            suffix = ".DEM";
                        }
                        else
                        {
                            throw new Exception();
                        }

                        datData.Position = startingPos;
                        var data = datData.ReadBytes(size);

                        Utils.PadIn2048Bytes(datData);
                        files.Add(ParseDatFile(name + suffix, data));
                    }
                }
            }
            return new DIR_DAT(files);
        }

        public static DIR_DAT FromFiles(params string[] paths)
        {
            var filePaths = new List<string>();
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    filePaths.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories).Where(File.Exists));
                }
                else if (File.Exists(path))
                {
                    filePaths.Add(path);
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            var files = new DATFile[filePaths.Count];
            for (int i = 0; i < filePaths.Count; i++)
            {
                var filePath = filePaths[i];
                files[i] = ParseDatFile(Path.GetFileName(filePath), File.ReadAllBytes(filePath));
            }
            return new DIR_DAT(files);
        }
        public void Serialize(string outputFolder, Configuration conf)
        {
            if (File.Exists(outputFolder))
            {
                throw new Exception($"Not a directory: {outputFolder}");
            }
            else if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            using var dirOutput = new Serializer(conf, File.OpenWrite(Path.Join(outputFolder, conf.WriteVersion.FilenameDIR)));
            using var datOutput = new Serializer(conf, File.OpenWrite(Path.Join(outputFolder, conf.WriteVersion.FilenameDAT)));

            if (dirOutput.WriteVersion != CROC_1_PS1.Instance)
            {
                dirOutput.WriteInt32(Files.Count);
            }
            foreach (var file in Files)
            {
                var start = datOutput.Position;
                file.Serialize(datOutput);
                var size = datOutput.Position - start;
                Utils.PadOut2048Bytes(datOutput);
                conf.WriteVersion.DirFormat.Pack(dirOutput, file.Name, size, start);
            }
        }
    }
}