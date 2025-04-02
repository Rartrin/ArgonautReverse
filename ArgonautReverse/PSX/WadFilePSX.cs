using System.Text;
using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.WadChunks.PSX;
using ArgonautReverse.WadChunks;

namespace ArgonautReverse.PSX
{
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
		public IReadOnlyList<ObjectDataPSX> models_3d => this.DPSX?.models_3d;

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
			this.TPSX.TextureFile.to_colorized_texture().Save(Path.Join(folder_path, wad_filename + ".PNG"), System.Drawing.Imaging.ImageFormat.Png);
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
				using(var obj_file = new StreamWriter(Path.Join(folder_path, obj_filename + ".OBJ"), false, Encoding.ASCII))
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
			using(var obj_file = new StreamWriter(Path.Join(folder_path, filename + ".OBJ"), false, Encoding.ASCII))
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
			File.WriteAllText(Path.Join(folder_path, wad_filename + ".OBJ"), obj.ToString(), Encoding.ASCII);
		}

		public void ExportActors(string folder_path, string wad_filename)
		{
			for(int i=0; i<this.n_scripts; i++)
			{
				var script = this.actors[i];
				var baseFilePath = Path.Join(folder_path, $"{wad_filename}_{i}");
				try
				{
					using var output = new StreamWriter($"{baseFilePath}.strat_asm", false);
					script.Write(output, true);
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to export ASM {baseFilePath}:");
					Console.WriteLine(e.ToString());
					continue;
				}

				Universal.StratLang.Decompiler.Parser parser;
				try
				{
					var lines = File.ReadAllLines($"{baseFilePath}.strat_asm");

					parser = new();
					var start = parser.ParseAndSetupInstructions(lines);
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to analyze stack {baseFilePath}:");
					Console.WriteLine(e.ToString());
					continue;
				}

				Universal.StratLang.Decompiler.StackAnalyzer stackAnalyzer;
				try
				{
					stackAnalyzer = new();
					stackAnalyzer.Analyze(parser.GetSubroutines());
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to analyze stack {baseFilePath}:");
					Console.WriteLine(e.ToString());
					continue;
				}
				var stackOutputLines = new List<string>();
				stackAnalyzer.Write(stackOutputLines);

				File.WriteAllLines($"{baseFilePath}.stack.strat", stackOutputLines);


				Universal.StratLang.Decompiler.FlowAnalyzer flowAnalyzer;
				try
				{
					flowAnalyzer = new();
					flowAnalyzer.Analyze(stackAnalyzer.Subroutines);
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to analyze flow {baseFilePath}:");
					Console.WriteLine(e.ToString());
					continue;
				}
				var flowWriter = new Universal.StratLang.Decompiler.Writer();
				flowAnalyzer.Write(flowWriter);

				File.WriteAllLines($"{baseFilePath}.flow.strat", flowWriter.GetLines());
			}
		}

		private void ExportDPSX(ProgramArgs args, Configuration conf)
		{
			if(!DPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any()){return;}

			if(args.ExportActors is string exportActors)
			{
				var wad_actors_folder_path = Path.Join(exportActors, Stem);
				ExportAssets.CreateExportDirectory(wad_actors_folder_path);
				ExportActors(wad_actors_folder_path, Stem);
			}
			if(args.ExportModels is string exportModels)
			{
				var wad_models_3d_folder_path = Path.Join(exportModels, Stem);
				ExportAssets.CreateExportDirectory(wad_models_3d_folder_path);
				export_experimental_models(wad_models_3d_folder_path, Stem);
			}
			if(args.ExportLevels is string exportLevels)
			{
				var wad_level_folder_path = Path.Join(exportLevels, Stem);
				ExportAssets.CreateExportDirectory(wad_level_folder_path);
				export_level(wad_level_folder_path, Stem);
			}
		}

		private void ExportSPSX(ProgramArgs args, Configuration conf)
		{
			if(!SPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any()){return;}

			if(args.ExportAudio is string exportAudio)
			{
				var wad_audio_export_folder_path = Path.Join(exportAudio, Stem);
				ExportAssets.CreateExportDirectory(wad_audio_export_folder_path);
				export_audio_to_wav(wad_audio_export_folder_path, Stem);
			}
			if(args.UnpackAudio is string unpackAudio)
			{
				var wad_audio_unpack_folder_path = Path.Join(unpackAudio, Stem);
				ExportAssets.CreateExportDirectory(wad_audio_unpack_folder_path);
				export_audio_to_vag(wad_audio_unpack_folder_path, Stem);
			}
		}

		private void ExportTPSX(ProgramArgs args, Configuration conf)
		{
			if(!TPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any()){return;}

			if(args.ExportTextures is string exportTextures)
			{
				TPSX.TextureFile.to_colorized_texture().Save(Path.Join(exportTextures, $"{Stem}.PNG"), System.Drawing.Imaging.ImageFormat.Png);
			}
		}

		public override void ExportWadAssets(ProgramArgs args, Configuration conf)
		{
			ExportTPSX(args, conf);
			ExportSPSX(args, conf);
			ExportDPSX(args, conf);
		}
	}
}