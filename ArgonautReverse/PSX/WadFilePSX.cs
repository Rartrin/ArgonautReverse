using System.Text;
using ArgonautReverse.Engine;
using ArgonautReverse.Files;
using ArgonautReverse.WadChunks.PSX;
using ArgonautReverse.WadChunks;
using System.Diagnostics.CodeAnalysis;
using ArgonautReverse.Universal.StratLang;
using ArgonautReverse.Universal.StratLang.Decompiler;

namespace ArgonautReverse.PSX
{
	public sealed class WadFilePSX(WadVersion version, string stem, byte[] data):WADFile(version, stem, data)
	{
		public TPSXChunk TPSX{get;private set;}
		public SPSXChunk SPSX{get;private set;}
		public DPSXChunk DPSX{get;private set;}

		public LPSXChunk LPSX{get;private set;}
		public PORTChunk PORT{get;private set;}
		public UNIFChunk UNIF{get;private set;}

		public ENDChunkPSX END{get;private set;}

		public override bool TryGetChunkInfo(ChunkType chunkType, [MaybeNullWhen(false)]out BaseWADChunkInfo info)
		{
			info = chunkType switch
			{
				ChunkType.ID_PSX_TEXT => TPSXChunkInfo.Instance,
				ChunkType.ID_PSX_SAMPLE => SPSXChunkInfo.Instance,
				ChunkType.ID_PSX_DATA => DPSXChunkInfo.Instance,

				ChunkType.ID_PSX_LANG => LPSXChunkInfo.Instance,
				ChunkType.ID_PSX_PORT => PORTChunkInfo.Instance,
				ChunkType.ID_PSX_UNIF => UNIFChunkInfo.Instance,

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

				ChunkType.ID_PSX_LANG => LPSX,
				ChunkType.ID_PSX_PORT => PORT,
				ChunkType.ID_PSX_UNIF => UNIF,

				ChunkType.ID_END => END,
				_ => throw new Exception($"Unknown type: {info.ChunkType}")
			});
		}
		public override void AddChunk(BaseWadChunk chunk)
		{
			base.AddChunk(chunk);
			switch(chunk.Info.ChunkType)
			{
				case ChunkType.ID_PSX_DATA:DPSX = (DPSXChunk)chunk;break;
				case ChunkType.ID_PSX_SAMPLE:SPSX = (SPSXChunk)chunk;break;
				case ChunkType.ID_PSX_TEXT:TPSX = (TPSXChunk)chunk;break;

				case ChunkType.ID_PSX_LANG:LPSX = (LPSXChunk)chunk;break;
				case ChunkType.ID_PSX_PORT:PORT = (PORTChunk)chunk;break;
				case ChunkType.ID_PSX_UNIF:UNIF = (UNIFChunk)chunk;break;

				case ChunkType.ID_END:END = (ENDChunkPSX)chunk;break;
				default:throw new Exception("Unsupported chunk for platform");
			}
		}

		public override void PrintInfo(TextWriter output)
		{
			output.Write("Game level");

			if(TPSX != null)
			{
				output.Write($" ({string.Join(", ", TPSX.Titles.Select(title => title.Trim(' ')))})");
			}
			
			output.WriteLine();
			if(TPSX != null)
			{
				output.Write($" {TPSX.TextureFile.Textures.Count} texture(s)");
			}
			if(SPSX != null)
			{
				output.Write($" {SPSX.n_sounds} audio file(s)");
			}
			if(DPSX != null)
			{
				output.Write($" {DPSX.Models3D.Count} model(s) {DPSX.Animations.Count} animation(s) {DPSX.LevelFile.chunks_matrix.n_filled_chunks} chunk(s)");
			}
			output.WriteLine();
		}

		/// <summary>Exports the material (MTL) and texture (PNG) files that are needed by the OBJ Wavefront file.</summary>
		private void _prepare_obj_export(string folder_path, string wad_filename)
		{
			var mtl_file = new StringWriter();
			mtl_file.WriteLine("newmtl mtl1");
			mtl_file.WriteLine($"map_Kd {wad_filename}.png");
			File.WriteAllText(Path.Join(folder_path, wad_filename+".mtl"), mtl_file.ToString());
			this.TPSX.TextureFile.to_colorized_texture().Save(Path.Join(folder_path, wad_filename + ".png"), System.Drawing.Imaging.ImageFormat.Png);
		}
		/// <summary>
		/// Tries to find one compatible animation for each model in the WAD, animates it to make it clean
		/// (see doc about 3D models) and exports them into Wavefront OBJ files at the given location.
		/// </summary>
		public void ExportModels(string folder_path, string wad_filename)
		{
			var n_models = DPSX.Models3D.Count;
			var n_animations = DPSX.Animations.Count;

			int? guess_compatible_animation(int position, int n_vertices_groups)
			{
				//EXPERIMENTAL: Band-aid, will be removed when animations' model id is found & reversed.
				float position2 = n_animations * position / (float)n_models;
				int a = (int)position2;
				int b = (int)MathF.Ceiling(position2);
				while(a > -1 || b < n_animations)
				{
					if(a > -1)
					{
						if(DPSX.Animations[a].n_vertices_groups == n_vertices_groups)
						{
							return a;
						}
						a -= 1;
					}
					if(b < n_animations)
					{
						if(DPSX.Animations[b].n_vertices_groups == n_vertices_groups)
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
			for(int i=0; i<DPSX.Models3D.Count; i++)
			{
				var model_3d = this.DPSX.Models3D[i];
				var obj_filename = $"{wad_filename}_{i}";
				var obj_file = new StringWriter();
				var textures = TPSX.TextureFile.Textures;
				if(model_3d.Data.VerticesGroups == 1)
				{
					model_3d.Data.ToSingleObj(obj_file, obj_filename, textures, wad_filename);
				}
				else
				{
					var animation_id = guess_compatible_animation(i, DPSX.Models3D[i].Data.VerticesGroups);
					if(animation_id == null)
					{
						model_3d.Data.ToSingleObj(obj_file, obj_filename, textures, wad_filename);
					}
					else
					{
						model_3d.Animate(DPSX.Animations[animation_id.Value]).Data.ToSingleObj(obj_file, obj_filename, textures, wad_filename);
					}
				}
				File.WriteAllText(Path.Join(folder_path, obj_filename + ".obj"), obj_file.ToString());
			}
		}

		private enum AudioFormat
		{
			WAV,
			VAG,
		}
		private void ExportAudio(string folder_path, string wad_filename, AudioFormat fmt)
		{
			if(fmt!=AudioFormat.VAG && fmt!=AudioFormat.WAV)
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
						(var audio_bytes, var extension) = fmt switch
						{
							AudioFormat.VAG => (vag.to_vag()[0], "vag"),
							AudioFormat.WAV => (vag.to_wav(), "wav"),
							_ => throw new NotImplementedException(),
						};
						File.WriteAllBytes(Path.Join(folder_path, $"{filename}.{extension}"), audio_bytes);
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
					switch(fmt)
					{
						case AudioFormat.VAG:
						{
							var audio_bytes = sound.vag.to_vag();
							if(fmt == AudioFormat.VAG && audio_bytes.Length == VAGSoundDataPSX.STEREO)
							{
								File.WriteAllBytes(Path.Join(folder_path, $"{filename}_L.vag"), audio_bytes[0]);
								File.WriteAllBytes(Path.Join(folder_path, $"{filename}_R.vag"), audio_bytes[1]);
							}
							else
							{
								File.WriteAllBytes(Path.Join(folder_path, $"{filename}.vag"), audio_bytes[0]);
							}
							break;
						}
						case AudioFormat.WAV:
						{
							var audio_bytes = sound.vag.to_wav();
							File.WriteAllBytes(Path.Join(folder_path, $"{filename}.wav"), audio_bytes);
							break;
						}
						default:throw new NotImplementedException();
					}
				}
			}
		}

		public void ExportAudioWAV(string folder_path, string wad_filename) => this.ExportAudio(folder_path, wad_filename, AudioFormat.WAV);
		public void ExportAudioVAG(string  folder_path, string wad_filename) => this.ExportAudio(folder_path, wad_filename, AudioFormat.VAG);

		public void ExportTrack(string folder_path, string wad_filename)
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
			obj.WriteLine(string.Format(TrackModelDataPSX.mtl_header, wad_filename));
			int vio = 0;
			int sub_chunk_id = 0;
			foreach(var texture in TPSX.TextureFile.Textures)
			{
				foreach(var coord in texture.output_coords)
				{
					obj.WriteLine($"vt {coord.X / 1024f} {(1024 - coord.Y) / 1024f}");
				}
			}
			for(int i=0; i<this.DPSX.LevelFile.chunks_matrix.ChunkHolders.Count; i++)
			{
				var chunk_holder = this.DPSX.LevelFile.chunks_matrix.ChunkHolders[i];
				if(chunk_holder!=null)
				{
					var (x, z) = this.DPSX.LevelFile.chunks_matrix.x_z_coords(i);
					foreach(var chunk in chunk_holder.Subchunks)
					{
						var cm = chunk.model_3d_data.Data;
						cm.ToBatchObj(obj, $"{wad_filename}_{sub_chunk_id}", x, chunk.height, z, chunk.rotation, vio);
						vio += cm.n_vertices;
						sub_chunk_id += 1;
					}
				}
			}
			File.WriteAllText(Path.Join(folder_path, wad_filename + ".OBJ"), obj.ToString(), Encoding.ASCII);
		}

		public void ExportActors(ProgramArgs args, Configuration conf)
		{
			if(!args.ExtractScripts){return;}

			var scriptsDirectory = args.GetExtractDirectory(Stem, "Scripts");
			var parser = new AsmParser(DPSX.Actors);
			parser.Process(scriptsDirectory);
		}

		private void ExportDPSX(ProgramArgs args, Configuration conf)
		{
			if(!DPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any()){return;}

			ExportActors(args, conf);
			if(args.ExtractModels)
			{
				var modelDirectory = args.GetExtractDirectory(Stem, "Models");
				ExportModels(modelDirectory, Stem);
			}
			if(args.ExtractLevels)
			{
				var trackDirectory = args.GetExtractDirectory(Stem, "Track");
				ExportTrack(trackDirectory, Stem);
			}
		}

		private void ExportSPSX(ProgramArgs args, Configuration conf)
		{
			if(!SPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any()){return;}

			if(args.ExtractAudio)
			{
				var audioDirectory = args.GetExtractDirectory(Stem, "Audio");
				ExportAudioWAV(audioDirectory, Stem);
			}
			if(args.UnpackAudio)
			{
				var unpackedAudioDirectory = args.GetExtractDirectory(Stem, "UnpackedAudio");
				ExportAudioVAG(unpackedAudioDirectory, Stem);
			}
		}

		private void ExportTPSX(ProgramArgs args, Configuration conf)
		{
			if(!TPSXChunkInfo.Instance.SupportedWadVersions.Intersect(conf.ReadVersion.WadVersions).Any()){return;}

			if(args.ExtractTextures)
			{
				TPSX.TextureFile.to_colorized_texture().Save(Path.Join(args.ExtractPath, Stem, "Textures.png"), System.Drawing.Imaging.ImageFormat.Png);
			}
		}

		public override void ExtractAssets(ProgramArgs args, Configuration conf)
		{
			ExportAssets.CreateExportDirectory(Path.Join(args.ExtractPath, Stem));

			ExportTPSX(args, conf);
			ExportSPSX(args, conf);
			ExportDPSX(args, conf);
		}

		public override (Script script, InstructionAddress address) GetStratProcAddr(int dataOffset)
		{
			//On PSX, this is a DATA chunk offset
			var script = DPSX.GetScript(dataOffset);
			return (script, (InstructionAddress)(dataOffset - script.DataChunkAddress));
		}

		public override void ProcessScripts()
		{
			DPSX.ProcessScipts(this);
		}
	}
}