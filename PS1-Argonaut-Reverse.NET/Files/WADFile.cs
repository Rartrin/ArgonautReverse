using System.Text;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.WadSections;
using ArgonautReverse.WadSections.DPSX;
using ArgonautReverse.WadSections.SPSX;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse.Files
{
	public class WADFile:DATFile
	{
		private readonly Dictionary<ChunkType,BaseWADSection> dict = new Dictionary<ChunkType,BaseWADSection>();

		private static readonly Dictionary<ChunkType,BaseWADSectionInfo> sections_conf = new Dictionary<ChunkType, BaseWADSectionInfo>()
		{
			[ChunkType.ID_TEXTPSX] = TPSXSectionInfo.Instance,
			[ChunkType.ID_SAMPLEPSX] = SPSXSectionInfo.Instance,
			[ChunkType.ID_DATAPSX] = DPSXSectionInfo.Instance,
			[ChunkType.ID_PORT] = PORTSectionInfo.Instance,
			[ChunkType.ID_END] = ENDSectionInfo.Instance,
		};

		public override string Suffix => "WAD";

		public WADFile(string stem, byte[] data):base(stem, data){}

		public override string ToString()
		{
			string titles = "";
			if(this.titles != null)
			{
				titles = $"({string.Join(", ", this.titles.Select(titles => titles.Trim(' ')))})";
			}
			var res = $"Game level{titles}";
			//if(this)
			{
				res += "\n";
				if(this.tpsx != null)
				{
					res += $" {this.n_textures} texture(s)";
				}
				if(this.spsx != null)
				{
					res += $" {this.n_sounds} audio file(s)";
				}
				if(this.dpsx != null)
				{
					res += $" {this.n_models} model(s) {this.n_animations} animation(s) {this.n_filled_chunks} chunk(s)";
				}
			}
			return res;
		}
		// WAD sections

		public TPSXSection tpsx => this.dict.GetValueOrDefault(ChunkType.ID_TEXTPSX) as TPSXSection;

		public SPSXSection spsx => this.dict.GetValueOrDefault(ChunkType.ID_SAMPLEPSX) as SPSXSection;

		public DPSXSection dpsx => this.dict.GetValueOrDefault(ChunkType.ID_DATAPSX) as DPSXSection;

		public PORTSection port => this.dict.GetValueOrDefault(ChunkType.ID_PORT) as PORTSection;

		public ENDSection end => this.dict.GetValueOrDefault(ChunkType.ID_END) as ENDSection;

		// TPSX

		public IReadOnlyList<string> titles => this.tpsx?.Titles ?? Array.Empty<string>();

		public IReadOnlyList<TextureData> textures => this.tpsx?.TextureFile?.Textures;

		public int n_textures => this.tpsx?.TextureFile?.Textures?.Count ?? 0;

		// SPSX

		public int n_sounds => this.spsx?.n_sounds ?? 0;

		// DPSX
		public IReadOnlyList<Object3DData> models_3d => this.dpsx?.models_3d;

		public int n_models => this.dpsx?.models_3d.Count ?? 0;

		public IReadOnlyList<AnimationData> animations => this.dpsx?.animations;

		public int n_animations => this.dpsx?.animations?.Count ?? 0;

		public IReadOnlyList<ActorData> actors => this.dpsx?.actors;

		public int n_scripts => this.dpsx?.actors?.Count ?? 0;

		public ChunksMatrix chunks_matrix => this.dpsx?.level_file?.chunks_matrix;

		public int n_filled_chunks => this.dpsx?.level_file?.chunks_matrix?.n_filled_chunks ?? 0;

		/// <summary>Exports the material (MTL) and texture (PNG) files that are needed by the OBJ Wavefront file.</summary>
		public void _prepare_obj_export(string folder_path, string wad_filename)
		{
			using(var mtl_file = new StreamWriter(Path.Join(folder_path, wad_filename+".MTL"), false, Encoding.ASCII))
			{
				mtl_file.WriteLine($"newmtl mtl1\nmap_Kd {wad_filename}.PNG");
			}
			this.tpsx.TextureFile.to_colorized_texture().Save(Path.Join(folder_path, (wad_filename + ".PNG")), System.Drawing.Imaging.ImageFormat.Png);
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
			for(int i=0; i<this.dpsx.models_3d.Count; i++)
			{
				var model_3d = this.dpsx.models_3d[i];
				var obj_filename = $"{wad_filename}_{i}";
				using(var obj_file = new StreamWriter(Path.Join(folder_path, (obj_filename + ".OBJ")), false, Encoding.ASCII))
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
			using(var obj_file = new StreamWriter(Path.Join(folder_path, (filename + ".OBJ")), false, Encoding.ASCII))
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

			if(this.spsx != null)
			{
				var mono_sounds = new Dictionary<string, IEnumerable<VAGSoundData>>()
				{
					["effect"] = this.spsx.common_sfx.vags,
					["ambient"] = this.spsx.ambient_tracks.vags,
					["level_effect"] = this.spsx.level_sfx_groups.vags,
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
				foreach(var sound in this.spsx.dialogues_bgms.Sounds)
				{
					string filename;
					if((((DialogueBGMSound)sound).flagsAndLoop&DialoguesBGMsSoundFlags.IS_BACKGROUND_MUSIC)!=0)
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
					if(fmt == "VAG" && audio_bytes.Length == VAGSoundData.STEREO)
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

		public void export_audio_to_wav(string folder_path, string wad_filename) =>
			this.export_audio(folder_path, wad_filename, "WAV");

		public void export_audio_to_vag(string  folder_path, string wad_filename) =>
			this.export_audio(folder_path, wad_filename, "VAG");

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
			using(var obj_file = new StreamWriter(Path.Join(folder_path, (wad_filename + ".OBJ")), false, Encoding.ASCII))
			{
				var obj = new StringWriter();
				obj.Write(string.Format(Model3DData.mtl_header, wad_filename));
				int vio = 0;
				int sub_chunk_id = 0;
				foreach(var texture in this.textures)
				{
					foreach(var coord in texture.output_coords)
					{
						obj.WriteLine($"vt {coord.X / 1024.0} {(1024 - coord.Y) / 1024.0}");
					}
				}
				for(int i=0; i<this.dpsx.level_file.chunks_matrix.Count; i++)
				{
					var chunk_holder = this.dpsx.level_file.chunks_matrix[i];
					if(chunk_holder!=null)
					{
						var (x, z) = this.dpsx.level_file.chunks_matrix.x_z_coords(i);
						foreach(var chunk in chunk_holder)
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

		private static unsafe IEnumerable<(ChunkType type, int start, int length)> GetChunkLocations(WadReader reader)
		{
			var chunkLocations = new List<(ChunkType type, int start,int size)>();
			
			//Locate Chunks
			var wadDataLength = reader.Read<int>();

			
			ChunkType chunkType;
			do
			{
				var start = reader.Position;
				chunkType = (ChunkType)reader.Read<uint>();
				var chunkDataLength = reader.Read<int>();

				// Detects incorrect WADs like FESOUND or FETHUND
				if (chunkLocations.Count == 0 & chunkType != ChunkType.ID_TEXTPSX)
				{
					throw new SectionNameError(reader.Position, ChunkType.ID_TEXTPSX.ToString(), chunkType.ToString());
				}
				chunkLocations.Add((chunkType, start, chunkDataLength + 2*sizeof(int)));//Adding for codename and chunkDataLength fields
				
				
				reader.Position += chunkDataLength;
			}
			while(chunkType!=ChunkType.ID_END);

			return chunkLocations;
		}

		public override unsafe void Parse(Configuration conf)
		{
			using var data_in = new WadReader(this, conf, conf.ReadVersion.GetWadVersion(Stem), new MemoryStream(this._data));
			
			var chunkLocations = GetChunkLocations(data_in);

			this.dict.Clear();
			foreach(var(type, start, length) in chunkLocations)
			{
				var chunkReader = data_in.ReadChunk(start, length);
				if(WADFile.sections_conf.ContainsKey(type))
				{
					var section = WADFile.sections_conf[type];
					if(section.supported_games.Contains(chunkReader.ReadVersion))
					{
						this.dict.Add(type, section.Parse(chunkReader));
					}
					else
					{
						Console.WriteLine($"Unsupported Chunk: {type}");
						this.dict.Add(type, new UnknownSectionInfo(type).fallback_parse(chunkReader));//throw new Exception("No idea what to do here, the original code shouldn't work here");
					}
				}
				else
				{
					Console.WriteLine($"Unknown Chunk: {type.GetRawName()}");
					this.dict.Add(type, new UnknownSectionInfo(type).fallback_parse(chunkReader));
				}
			}
		}
		public override void Serialize(Serializer data_out)
		{
			var wad_size_offset = data_out.Position;

			//TODO: Understand data
			data_out.Write<uint>(0);//Placeholder for total data size
			foreach(var section in this.dict.Values)
			{
				section.serialize(data_out);
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
}