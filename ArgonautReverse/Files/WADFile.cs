using System.Text;
using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PSX;
using ArgonautReverse.WadChunks;
using ArgonautReverse.WadChunks.PC;
using ArgonautReverse.WadChunks.PSX;

namespace ArgonautReverse.Files
{
    public enum WadFileType:int
	{
		WAD_TYPE_INVALID = -1,
		WAD_TYPE_LEVEL = 0,
		WAD_TYPE_BOSS = 1,
		WAD_TYPE_SECRET = 2,
		WAD_TYPE_INTERFACE = 3,
	}
	public abstract class WADFile:DATFile
	{
		private readonly List<BaseWadChunk> chunks = new List<BaseWadChunk>();

		public override string Suffix => "WAD";

		public WadVersion Version{get;}

		public static WADFile Create(DatVersion datVerion, WadVersion wadVersion, string stem, byte[] data) => datVerion.Platform switch
		{
			Platform.PC => new WadFilePC(wadVersion, stem, data),
			Platform.PSX => new WadFilePSX(wadVersion, stem, data),
			_ => throw new NotImplementedException($"Platform not implemented: {datVerion.Platform}"),
		};

		public WADFile(WadVersion version, string stem, byte[] data):base(stem, data)
		{
			Version = version;
		}
		
		public abstract bool TryGetChunkInfo(ChunkType chunkType, out BaseWADChunkInfo info);
		
		public abstract void AddChunk(BaseWadChunk chunk);

		public abstract T GetChunk<T>(BaseWADChunkInfo<T> info) where T:BaseWadChunk;

		private sealed class ChunkLocation
		{
			public BaseWADChunkInfo Info;
			public int DataStart;
			public int DataLength;

			public ChunkLocation(BaseWADChunkInfo info, int dataStart, int dataLength)
			{
				Info = info;
				DataStart = dataStart;
				DataLength = dataLength;
			}
		}

		private unsafe IEnumerable<ChunkLocation> LocateChunks(WadReader reader)
		{
			var chunkLocations = new List<ChunkLocation>();
			
			var wadDataLength = reader.Read<int>();

			ChunkType prevChunk = ChunkType.Unknown;
			ChunkType chunkType;
			do
			{
				chunkType = (ChunkType)reader.Read<uint>();

				if(!Enum.IsDefined(chunkType))
				{
					//The size on the MAP chunk is sometimes off by 4 bytes.
					//If we don't recognize the current chunk type and the last chunk was a map, reduce its length and then try again
					//TODO: Any better way to handle MAP size programatically?
					if(prevChunk == ChunkType.ID_PC_MAP)
					{
						reader.Position -= 8;
						chunkType = (ChunkType)reader.Read<uint>();
						if(!Enum.IsDefined(chunkType))
						{
							throw new Exception("Map chunk's size is offset more than normal");
						}
						chunkLocations[^1].DataLength -= 4;
					}
					else
					{
						throw new Exception("Unknown chunk type");
					}
				}

				var chunkDataLength = reader.Read<int>();

				// Detects incorrect WADs like FESOUND or FETHUND
				if (chunkLocations.Count == 0)
				{
					switch(chunkType)
					{
						case ChunkType.ID_PC_INFO://Start of PC wads
						case ChunkType.ID_PSX_CWAD://Start of compressed PSX Wads
						case ChunkType.ID_PSX_TEXT://Start of uncompressed PSX Wads
							break;
						default:
							throw new Exception($"Wad starts with unsupported chunk type {chunkType} at file position 0x{reader.Position:X8}");
					}
				}

				if(!TryGetChunkInfo(chunkType, out var chunkInfo))
				{
					Console.WriteLine($"Unknown Chunk: {chunkType.GetRawName()}");
					chunkInfo = new UnknownChunkInfo(chunkType);
				}
				else if(!chunkInfo.SupportedWadVersions.Contains(reader.ReadVersion))
				{
					Console.WriteLine($"Unsupported Chunk: {chunkType}");
					chunkInfo = new UnsupportedChunkInfo(chunkInfo);
				}

				chunkLocations.Add(new ChunkLocation(chunkInfo, reader.Position, chunkDataLength));
				
				reader.Position += chunkDataLength;
				prevChunk = chunkType;
			}
			while(chunkType!=ChunkType.ID_END);

			if(reader.Remaining != 0)
			{
				Console.WriteLine($"Wad file has {reader.Remaining} unread bytes following END chunk.");
			}
			return chunkLocations;
		}

		public override unsafe void Parse(Configuration conf)
		{
			var data_in = new WadReader(this, conf, conf.ReadVersion.GetWadVersion(Stem), this._data);
			
			var chunkLocations = LocateChunks(data_in);

			var chunkTypesRead = new HashSet<ChunkType>();
			this.chunks.Clear();
			foreach(var chunkLocation in chunkLocations)
			{
				data_in.Position = chunkLocation.DataStart;
				var chunkReader = data_in.ReadChunk(chunkLocation.DataLength);
				{
					if(!chunkTypesRead.Add(chunkLocation.Info.ChunkType))
					{
						throw new Exception($"Chunk {chunkLocation.Info.ChunkType} already read");
					}
					var chunk = chunkLocation.Info.Parse(chunkReader);
					this.AddChunk(chunk);
					this.chunks.Add(chunk);
					if(chunkReader.Remaining != 0)
					{
						Console.WriteLine($"WARNING: There were {chunkReader.Remaining} bytes of unparsed data in {chunkLocation.Info.ChunkType}!");
					}
				}
			}
		}
		public override void Serialize(WadWriter data_out)
		{
			var wad_size_offset = data_out.Position;

			//TODO: Understand data
			data_out.Write<uint>(0);//Placeholder for total data size
			foreach(var chunk in this.chunks)
			{
				chunk.Serialize(data_out);
			}
			var end_offset = data_out.Position;
			var wad_size = end_offset - wad_size_offset;
			if(data_out.WriteVersion==CROC_2_PS1.WadVersion || data_out.WriteVersion==HARRY_POTTER_1_PS1.WadVersion || data_out.WriteVersion==HARRY_POTTER_2_PS1.WadVersion)
			{
				wad_size += 2048;
			}
			data_out.Position += wad_size_offset;
			data_out.WriteInt32(wad_size);
			data_out.Position += end_offset;
		}
	}

	public sealed class WadFilePC:WADFile
	{
		public INFOChunk InfoChunk{get;private set;}
		public VERSChunk VersionChunk{get;private set;}
		public MAPChunk MapChunk{get;private set;}
		public TRAKChunk TrackChunk{get;private set;}
		public TEXTChunk TextChunk{get;private set;}
		//public LGHTChunk LightChunk{get;private set;}
		public STPCChunk StratChunk{get;private set;}
		public WFPCChunk WadflagsChunk{get;private set;}
		//public SMPCChunk SampleChunk{get;private set;}
		//public LANGChunk LanguageChunk{get;private set;}
		//public AMPCChunk AMPCChunk{get;private set;}
		public FONTChunk FontChunk{get;private set;}
		//public SPRTChunk SpriteChunk{get;private set;}
		//public RIMGChunk RIMGChunk{get;private set;}
		public ENDChunkPC EndChunk{get;private set;}

		public WadFilePC(WadVersion version, string stem, byte[] data) : base(version, stem, data){}

		public override bool TryGetChunkInfo(ChunkType chunkType, out BaseWADChunkInfo info)
		{
			info = chunkType switch
			{
				ChunkType.ID_PC_INFO => INFOChunkInfo.Instance,
				ChunkType.ID_PC_VERSION => VERSChunkInfo.Instance,
				ChunkType.ID_PC_MAP => MAPChunkInfo.Instance,
				ChunkType.ID_PC_TRACK => TRAKChunkInfo.Instance,
				ChunkType.ID_PC_TEXT => TEXTChunkInfo.Instance,
				//ChunkType.ID_PC_LIGHT => LGHTChunkInfo.Instance,
				ChunkType.ID_PC_STRAT => STPCChunkInfo.Instance,
				ChunkType.ID_PC_WADFLAGS => WFPCChunkInfo.Instance,

				//ChunkType.ID_PC_SAMPLE => SMPCChunkInfo.Instance,
				//ChunkType.ID_PC_LANG => LANGChunkInfo.Instance,

				//ChunkType.ID_PC_AMPC => AMPCChunkInfo.Instance,
				ChunkType.ID_PC_FONT => FONTChunkInfo.Instance,
				//ChunkType.ID_PC_SPRITE => SPRTChunkInfo.Instance,
				//ChunkType.ID_PC_RIMG => RIMGChunkInfo.Instance,

				ChunkType.ID_END => ENDChunkPCInfo.Instance,
				_ => null
			};
			return info != null;
		}
		public override T GetChunk<T>(BaseWADChunkInfo<T> info)
		{
			return (T)(BaseWadChunk)(info.ChunkType switch
			{
				ChunkType.ID_PC_INFO => InfoChunk,
				ChunkType.ID_PC_VERSION => VersionChunk,
				ChunkType.ID_PC_MAP => MapChunk,
				ChunkType.ID_PC_TRACK => TrackChunk,
				ChunkType.ID_PC_TEXT => TextChunk,
				//ChunkType.ID_PC_LIGHT => LightChunk,
				ChunkType.ID_PC_STRAT => StratChunk,
				ChunkType.ID_PC_WADFLAGS => WadflagsChunk,

				//ChunkType.ID_PC_SAMPLE => SampleChunk,
				//ChunkType.ID_PC_LANG => LanguageChunk,

				//ChunkType.ID_PC_AMPC => AMPCChunk,
				ChunkType.ID_PC_FONT => FontChunk,
				//ChunkType.ID_PC_SPRITE => SpriteChunk,
				//ChunkType.ID_PC_RIMG => RIMGChunk,

				ChunkType.ID_END => EndChunk,
				_ => throw new Exception($"Unknown type: {info.ChunkType}")
			});
		}

		public override void AddChunk(BaseWadChunk chunk)
		{
			switch(chunk.Info.ChunkType)
			{
				case ChunkType.ID_PC_INFO:InfoChunk = (INFOChunk)chunk;break;
				case ChunkType.ID_PC_VERSION:VersionChunk = (VERSChunk)chunk;break;
				case ChunkType.ID_PC_MAP:MapChunk = (MAPChunk)chunk;break;
				case ChunkType.ID_PC_TRACK:TrackChunk = (TRAKChunk)chunk;break;
				case ChunkType.ID_PC_TEXT:TextChunk = (TEXTChunk)chunk;break;
				//case ChunkType.ID_PC_LIGHT:LightChunk = (LGHTChunk)chunk;break;
				case ChunkType.ID_PC_STRAT:StratChunk = (STPCChunk)chunk;break;
				case ChunkType.ID_PC_WADFLAGS:WadflagsChunk = (WFPCChunk)chunk;break;

				//case ChunkType.ID_PC_SAMPLE:SampleChunk = (SMPCChunk)chunk;break;
				//case ChunkType.ID_PC_LANG:LanguageChunk = (LANGChunk)chunk;break;

				//case ChunkType.ID_PC_AMPC:AMPCChunk = (AMPCChunk)chunk;break;
				case ChunkType.ID_PC_FONT:FontChunk = (FONTChunk)chunk;break;
				//case ChunkType.ID_PC_SPRITE:SpriteChunk = (SPRTChunk)chunk;break;
				//case ChunkType.ID_PC_RIMG:RIMGChunk = (RIMGChunk)chunk;break;

				case ChunkType.ID_END:EndChunk = (ENDChunkPC)chunk;break;
				//default:throw new Exception("Unsupported chunk for platform");
			}
		}
	}

	public sealed class WadFilePSX:WADFile
	{
		public TPSXChunk TPSX{get;private set;}
		public SPSXChunk SPSX{get;private set;}
		public DPSXChunk DPSX{get;private set;}
		public PORTChunk PORT{get;private set;}
		public ENDChunkPSX END{get;private set;}

		public WadFilePSX(WadVersion version, string stem, byte[] data) : base(version, stem, data){}

		public override bool TryGetChunkInfo(ChunkType chunkType, out BaseWADChunkInfo info)
		{
			info = chunkType switch
			{
				ChunkType.ID_PSX_TEXT => TPSXChunkInfo.Instance,
				ChunkType.ID_PSX_SAMPLE => SPSXChunkInfo.Instance,
				ChunkType.ID_PSX_DATA => DPSXChunkInfo.Instance,
				ChunkType.ID_PSX_PORT => PORTChunkInfo.Instance,

				ChunkType.ID_END => ENDChunkInfoPSX.Instance,
				_ => null
			};
			return info != null;
		}
		public override T GetChunk<T>(BaseWADChunkInfo<T> info)
		{
			return (T)(BaseWadChunk)(info.ChunkType switch
			{
				ChunkType.ID_PSX_TEXT => TPSX,
				ChunkType.ID_PSX_SAMPLE => SPSX,
				ChunkType.ID_PSX_DATA => DPSX,
				ChunkType.ID_PSX_PORT => PORT,

				ChunkType.ID_END => END,
				_ => throw new Exception($"Unknown type: {info.ChunkType}")
			});
		}
		public override void AddChunk(BaseWadChunk chunk)
		{
			switch(chunk.Info.ChunkType)
			{
				case ChunkType.ID_PSX_DATA:DPSX = (DPSXChunk)chunk;break;
				case ChunkType.ID_PSX_SAMPLE:SPSX = (SPSXChunk)chunk;break;
				case ChunkType.ID_PSX_TEXT:TPSX = (TPSXChunk)chunk;break;
				case ChunkType.ID_END:END = (ENDChunkPSX)chunk;break;
				default:throw new Exception("Unsupported chunk for platform");
			}
		}

		public override void PrintInfo(TextWriter output)
		{
			output.Write("Game level");

			if(this.titles != null)
			{
				output.Write($" ({string.Join(", ", this.titles.Select(titles => titles.Trim(' ')))})");
			}
			
			output.WriteLine();
			if(this.TPSX != null)
			{
				output.Write($" {this.n_textures} texture(s)");
			}
			if(this.SPSX != null)
			{
				output.Write($" {this.n_sounds} audio file(s)");
			}
			if(this.DPSX != null)
			{
				output.Write($" {this.n_models} model(s) {this.n_animations} animation(s) {this.n_filled_chunks} chunk(s)");
			}
			output.WriteLine();
		}

		// TPSX

		public IReadOnlyList<string> titles => this.TPSX?.Titles ?? Array.Empty<string>();

		public IReadOnlyList<TextureDataPSX> textures => this.TPSX?.TextureFile?.Textures;

		public int n_textures => this.TPSX?.TextureFile?.Textures?.Count ?? 0;

		// SPSX

		public int n_sounds => this.SPSX?.n_sounds ?? 0;

		// DPSX
		public IReadOnlyList<Object3DDataPSX> models_3d => this.DPSX?.models_3d;

		public int n_models => this.DPSX?.models_3d.Count ?? 0;

		public IReadOnlyList<AnimationDataPSX> animations => this.DPSX?.animations;

		public int n_animations => this.DPSX?.animations?.Count ?? 0;

		public IReadOnlyList<ActorDataPSX> actors => this.DPSX?.actors;

		public int n_scripts => this.DPSX?.actors?.Count ?? 0;

		public ChunksMatrixPSX chunks_matrix => this.DPSX?.level_file?.chunks_matrix;

		public int n_filled_chunks => this.DPSX?.level_file?.chunks_matrix?.n_filled_chunks ?? 0;

		/// <summary>Exports the material (MTL) and texture (PNG) files that are needed by the OBJ Wavefront file.</summary>
		public void _prepare_obj_export(string folder_path, string wad_filename)
		{
			using(var mtl_file = new System.IO.StreamWriter(Path.Join(folder_path, wad_filename+".MTL"), false, Encoding.ASCII))
			{
				mtl_file.WriteLine($"newmtl mtl1\nmap_Kd {wad_filename}.PNG");
			}
			this.TPSX.TextureFile.to_colorized_texture().Save(Path.Join(folder_path, (wad_filename + ".PNG")), System.Drawing.Imaging.ImageFormat.Png);
		}
		/// <summary>
		/// Tries to find one compatible animation for each model in the WAD, animates it to make it clean
		/// (see doc about 3D models) and exports them into Wavefront OBJ files at the given location.
		/// </summary>
		public void export_experimental_models(string folder_path, string wad_filename)
		{
			var n_models = this.n_models;
			var n_animations = this.n_animations;

			int? guess_compatible_animation(int position, int n_vertices_groups)
			{
				//EXPERIMENTAL: Band-aid, will be removed when animations' model id is found & reversed.
				position = n_animations * position / n_models;
				int a = (int)position;
				int b = (int)Math.Ceiling((double)position);
				while(a > -1 || b < n_animations)
				{
					if(a > -1)
					{
						if(this.animations[a].n_vertices_groups == n_vertices_groups)
						{
							return a;
						}
						a -= 1;
					}
					if(b < n_animations)
					{
						if(this.animations[b].n_vertices_groups == n_vertices_groups)
						{
							return b;
						}
						b += 1;
					}
				}
				return null;
			}
			if(!Directory.Exists(folder_path))
			{
				Directory.CreateDirectory(folder_path);
			}
			else if(File.Exists(folder_path))
			{
				throw new Exception("Path should be to a directory, not a file");
			}

			this._prepare_obj_export(folder_path, wad_filename);
			for(int i=0; i<this.DPSX.models_3d.Count; i++)
			{
				var model_3d = this.DPSX.models_3d[i];
				var obj_filename = $"{wad_filename}_{i}";
				using(var obj_file = new System.IO.StreamWriter(Path.Join(folder_path, (obj_filename + ".OBJ")), false, Encoding.ASCII))
				{
					if(model_3d.Data.n_vertices_groups == 1)
					{
						model_3d.Data.ToSingleObj(obj_file, obj_filename, this.textures, wad_filename);
					}
					else
					{
						var animation_id = guess_compatible_animation(i, this.models_3d[i].Data.n_vertices_groups);
						if(animation_id == null)
						{
							model_3d.Data.ToSingleObj(obj_file, obj_filename, this.textures, wad_filename);
						}
						else
						{
							model_3d.Animate(this.animations[animation_id.Value]).Data.ToSingleObj(obj_file, obj_filename, this.textures, wad_filename);
						}
					}
				}
			}
		}
		/// <summary>
		/// Exports a 3D model into a Wavefront OBJ file along with a MTL file and a texture file.
		/// Avoid calling this function on a lot of 3D models at once, WAD batch export functions are made for that.
		/// If you do it anyway, the export will take a long time as a new texture file will be generated for each model.
		/// </summary>
		public void export_model_3d(int model_id, string folder_path, string filename)
		{
			this._prepare_obj_export(folder_path, filename);
			using(var obj_file = new System.IO.StreamWriter(Path.Join(folder_path, (filename + ".OBJ")), false, Encoding.ASCII))
			{
				var obj = new StringWriter();
				this.models_3d[model_id].Data.ToSingleObj(obj, filename, this.textures, filename);
				obj_file.Write(obj.ToString());
			}
		}

		public void export_audio(string folder_path, string wad_filename, string fmt)
		{
			if(fmt!="VAG" && fmt!="WAV")
			{
				throw new Exception("Only VAG and WAV export is supported at the moment");
			}

			if(this.SPSX != null)
			{
				var mono_sounds = new Dictionary<string, IEnumerable<VAGSoundDataPSX>>()
				{
					["effect"] = this.SPSX.common_sfx.vags,
					["ambient"] = this.SPSX.ambient_tracks.vags,
					["level_effect"] = this.SPSX.level_sfx_groups.vags,
				};
				foreach(var(prefix, vags) in mono_sounds)
				{
					int i=0;
					foreach(var vag in vags)
					{
						var filename = $"{wad_filename}_{prefix}_{i}";
						var audio_bytes = fmt == "VAG" ? vag.to_vag()[0] : vag.to_wav(filename);
						File.WriteAllBytes(Path.Join(folder_path, $"{filename}.{fmt}"), audio_bytes);
						i++;
					}
				}
				int dialogue_index = 0;
				int bgm_index = 0;
				foreach(var sound in this.SPSX.dialogues_bgms.Sounds)
				{
					string filename;
					if((((DialogueBGMSoundPSX)sound).flagsAndLoop&DialoguesBGMsSoundFlagsPSX.IS_BACKGROUND_MUSIC)!=0)
					{
						filename = $"{wad_filename}_background_music_{bgm_index}";
						bgm_index += 1;
					}
					else
					{
						filename = $"{wad_filename}_dialogue_{dialogue_index}";
						dialogue_index += 1;
					}
					var audio_bytes = (fmt == "VAG") ? sound.vag.to_vag() : new[]{sound.vag.to_wav(filename)};
					if(fmt == "VAG" && audio_bytes.Length == VAGSoundDataPSX.STEREO)
					{
						File.WriteAllBytes(Path.Join(folder_path, $"{filename}_L.VAG"), audio_bytes[0]);
						File.WriteAllBytes(Path.Join(folder_path, $"{filename}_R.VAG"), audio_bytes[1]);
					}
					else
					{
						File.WriteAllBytes(Path.Join(folder_path, $"{filename}.{fmt}"), audio_bytes[0]);
					}
				}
			}
		}

		public void export_audio_to_wav(string folder_path, string wad_filename) => this.export_audio(folder_path, wad_filename, "WAV");

		public void export_audio_to_vag(string  folder_path, string wad_filename) => this.export_audio(folder_path, wad_filename, "VAG");

		public void export_level(string folder_path, string wad_filename)
		{
			if(!Directory.Exists(folder_path))
			{
				Directory.CreateDirectory(folder_path);//mkdir(parents=True, exist_ok=True)
			}
			else if(File.Exists(folder_path))
			{
				throw new Exception("Should be a directory, not a file");
			}

			this._prepare_obj_export(folder_path, wad_filename);
			using(var obj_file = new System.IO.StreamWriter(Path.Join(folder_path, (wad_filename + ".OBJ")), false, Encoding.ASCII))
			{
				var obj = new StringWriter();
				obj.WriteLine(string.Format(Model3DDataPSX.mtl_header, wad_filename));
				int vio = 0;
				int sub_chunk_id = 0;
				foreach(var texture in this.textures)
				{
					foreach(var coord in texture.output_coords)
					{
						obj.WriteLine($"vt {coord.X / 1024.0} {(1024 - coord.Y) / 1024.0}");
					}
				}
				for(int i=0; i<this.DPSX.level_file.chunks_matrix.ChunkHolders.Count; i++)
				{
					var chunk_holder = this.DPSX.level_file.chunks_matrix.ChunkHolders[i];
					if(chunk_holder!=null)
					{
						var (x, z) = this.DPSX.level_file.chunks_matrix.x_z_coords(i);
						foreach(var chunk in chunk_holder.Subchunks)
						{
							var cm = chunk.model_3d_data.Data;
							cm.ToBatchObj(
								obj,
								$"{wad_filename}_{sub_chunk_id}",
								x,
								chunk.height,
								z,
								chunk.rotation,
								vio
							);
							vio += cm.n_vertices;
							sub_chunk_id += 1;
						}
					}
				}
				obj_file.Write(obj.ToString());
			}
		}

		public void ExportActors(string folder_path, string wad_filename)
		{
			for(int i=0; i<this.n_scripts; i++)
			{
				File.WriteAllBytes(Path.Join(folder_path, $"{wad_filename}_{i}.raw_actor"), this.actors[i].data);
			}
		}
	}
}