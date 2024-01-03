using System.Text;
using ArgonautReverse.WadSections;
using ArgonautReverse.WadSections.DPSX;
using ArgonautReverse.WadSections.TPSX;

namespace ArgonautReverse.Files
{
	public class WADFile:DATFile//, IReadOnlyDictionary<uint, BaseWADSectionInfo>
	{
		private readonly Dictionary<uint,BaseWADSection> dict;
		//suffix = "WAD"

		private static readonly Dictionary<uint,BaseWADSectionInfo> sections_conf = new Dictionary<uint, BaseWADSectionInfo>()
		{
			[TPSXSectionInfo.Instance.codename_raw] = TPSXSectionInfo.Instance,
			//TODO: Sound
			//[SPSXSectionInfo.Instance.codename_raw] = SPSXSectionInfo.Instance,
			[DPSXSectionInfo.Instance.codename_raw] = DPSXSectionInfo.Instance,
			[PORTSectionInfo.Instance.codename_raw] = PORTSectionInfo.Instance,
			//TODO: End
			//[ENDSectionInfo.Instance.codename_raw] = ENDSectionInfo.Instance,
		};

		public WADFile(string stem, string suffix = null, /*Dictionary<uint,BaseWADSection> sections = null, */byte[] data = null):base(stem, suffix, data:data)
		{
			//if(sections != null)
			//{
			//	dict = new Dictionary<uint,BaseWADSection>(sections);
			//}
			//else
			//{
				dict = new Dictionary<uint,BaseWADSection>();
			//}
		}
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
					res += $" {this.n_textures:>4} texture(s)";
				}
				//TODO: Sound
				//if(this.spsx is SPSXSection)
				//{
				//	res += $" {this.n_sounds:>4} audio file(s)";
				//}
				if(this.dpsx != null)
				{
					res += $" {this.n_models:>4} model(s) {this.n_animations:>4} animation(s) {this.n_filled_chunks:>4} chunk(s)";
				}
			}
			return res;
		}
		// WAD sections

		public TPSXSection tpsx => (TPSXSection)this.dict.GetValueOrDefault(TPSXSectionInfo.Instance.codename_raw);

		//TODO: Sound
		//public SPSXSection spsx => (SPSXSection)this.dict.GetValueOrDefault(SPSXSectionInfo.Instance.codename_raw);

		public DPSXSection dpsx => (DPSXSection)this.dict.GetValueOrDefault(DPSXSectionInfo.Instance.codename_raw);

		public PORTSection port => (PORTSection)this.dict.GetValueOrDefault(PORTSectionInfo.Instance.codename_raw);

		//public ENDSection end => (ENDSection)this.dict.GetValueOrDefault(ENDSectionInfo.Instance.codename_raw);

		// TPSX

		public IReadOnlyList<string> titles => (IReadOnlyList<string>)this.tpsx?.Titles ?? Array.Empty<string>();

		public IReadOnlyList<TextureData> textures => this.tpsx?.TextureFile?.Textures;

		public int n_textures => this.tpsx?.TextureFile?.Textures?.Count ?? 0;

		// SPSX

		//TODO: Sound
		public object common_sound_effects => throw new NotImplementedException();//this.spsx?.common_sfx;

		//TODO: Sound
		public object ambient_tracks => throw new NotImplementedException();//this.spsx?.ambient_tracks;

		public object flattened_level_sfx
		{
			get
			{
				//TODO: Sound
				throw new NotImplementedException();
				//if(this.end == null) {return null;}
				//return this.spsx.level_sfx_groups.sounds;
			}
		}

		public object level_sfx
		{
			get
			{
				//TODO: Sound
				throw new NotImplementedException();
				//if(this.end == null) {return null;}
				//return this.spsx.level_sfx_groups;
			}
		}

		public object dialogues_bgms
		{
			get
			{
				//TODO: Sound
				throw new NotImplementedException();
				//if(this.end == null) {return null;}
				//return this.spsx.dialogues_bgms;
			}
		}

		public int n_sounds
		{
			get
			{
				//TODO: Sound
				throw new NotImplementedException();
				//if(this.spsx == null || this.spsx is not SPSXSection)
				//{
				//	return 0;
				//}
				//return this.spsx.n_sounds;
			}
		}
		// DPSX
		public IReadOnlyList<Object3DData> models_3d => this.dpsx?.models_3d;

		public int n_models => this.dpsx?.models_3d.Count ?? 0;

		public IReadOnlyList<AnimationData> animations => this.dpsx?.animations;

		public int n_animations => this.dpsx?.animations?.Count ?? 0;

		public IReadOnlyList<ActorData> scripts => this.dpsx?.scripts;

		public int n_scripts => this.dpsx?.scripts?.Count ?? 0;

		public ChunksMatrix chunks_matrix => this.dpsx?.level_file?.chunks_matrix;

		public int n_filled_chunks => this.dpsx?.level_file?.chunks_matrix?.n_filled_chunks ?? 0;

		/// <summary>Exports the material (MTL) and texture (PNG) files that are needed by the OBJ Wavefront file.</summary>
		public void _prepare_obj_export(string folder_path, string wad_filename)
		{
			using(var mtl_file = new StreamWriter(Path.Join(folder_path, wad_filename+".MTL"), false, Encoding.ASCII))
			{
				mtl_file.WriteLine(Configuration.wavefront_header + $"newmtl mtl1\nmap_Kd {wad_filename}.PNG");
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
				var obj = new StringWriter();//StringIO
				this.models_3d[model_id].Data.ToSingleObj(obj, filename, this.textures, filename);
				obj_file.Write(obj.ToString());
			}
		}

		public void export_audio(string folder_path, string wad_filename, string fmt)
		{
			//TODO: Sound
			throw new NotImplementedException();
			//if(fmt!="VAG" && fmt!="WAV")
			//{
			//	throw new Exception("Only VAG and WAV export is supported at the moment");
			//}
		
			//if(this.spsx != null)
			//{
			//	throw new NotImplementedException();
			//	//var mono_sounds = new Dictionary<string, object>()
			//	//{
			//	//	["effect"] = this.spsx.common_sfx,
			//	//	["ambient"] = this.spsx.ambient_tracks,
			//	//	["level_effect"] = this.spsx.level_sfx_groups,
			//	//};
			//	//foreach(var(prefix, sounds) in mono_sounds)
			//	//{
			//	//	for i, vag in enumerate(sounds.vags):
			//	//		filename = f"{wad_filename}_{prefix}_{i}"
			//	//		audio_bytes = (
			//	//			vag.to_vag(filename)[0]
			//	//			if fmt == "VAG"
			//	//			else vag.to_wav(filename)
			//	//		)
			//	//		(folder_path / f"{filename}.{fmt}").write_bytes(audio_bytes)
			//	//}
			//	//dialogue_index = 0
			//	//bgm_index = 0
			//	//for sound in this.spsx.dialogues_bgms:
			//	//	if DialoguesBGMsSoundFlags.IS_BACKGROUND_MUSIC in sound.flags:
			//	//		filename = f"{wad_filename}_background_music_{bgm_index}"
			//	//		bgm_index += 1
			//	//	else:
			//	//		filename = f"{wad_filename}_dialogue_{dialogue_index}"
			//	//		dialogue_index += 1
			//	//	audio_bytes = (
			//	//		sound.vag.to_vag(filename)
			//	//		if fmt == "VAG"
			//	//		else sound.vag.to_wav(filename)
			//	//	)
			//	//	if fmt == "VAG" and len(audio_bytes) == VAGSoundData.STEREO:
			//	//		(folder_path / f"{filename}_L.VAG").write_bytes(audio_bytes[0])
			//	//		(folder_path / f"{filename}_R.VAG").write_bytes(audio_bytes[1])
			//	//	else:
			//	//		(folder_path / f"{filename}.{fmt}").write_bytes(
			//	//			audio_bytes[0] if fmt == "VAG" else audio_bytes
			//	//		)
			//}
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
				var obj = new StringWriter();//StringIO
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

		public void ExportStrats(string folder_path, string wad_filename)
		{
			for(int i=0; i<this.n_scripts; i++)
			{
				File.WriteAllBytes(Path.Join(folder_path, $"{wad_filename}_{i}.STRAT_ASM"), this.scripts[i].data);
			}
		}

		public override unsafe void parse(Configuration conf)
		{
			using var data_in = new Parser(new MemoryStream(this._data));//BytesIO
			var sections_offsets = new Dictionary<uint,int>();
			void parse_sections()
			{
				data_in.Seek(4);
				while(true)
				{
					var codename = data_in.ReadUInt32();

					// Detects incorrect WADs like FESOUND or FETHUND
					if (sections_offsets.Count == 0 & codename != TPSXSectionInfo.Instance.codename_raw)
					{
						throw new SectionNameError(data_in.Position, TPSXSectionInfo.Instance.codename_str, Encoding.Latin1.GetString((byte*)&codename, 4));
					}
					sections_offsets[codename] = data_in.Position - 4;
				
					//0x454E4420
					if(codename == 0x454E4420 /*ENDSectionInfo.Info.codename_raw*/)// ' DNE' (END)
					{
						break;
					}

					var offset = data_in.ReadInt32();
					data_in.Position += offset;
				}
			}
		
			this.dict.Clear();
			parse_sections();

			foreach(var(codename_bytes, offset) in sections_offsets)
			{
				data_in.Seek(offset);
				if(WADFile.sections_conf.ContainsKey(codename_bytes))
				{
					var section = WADFile.sections_conf[codename_bytes];
					if(section.supported_games.Contains(conf.game))
					{
						if(codename_bytes != 0x454E4420/*ENDSection.codename_bytes*/)
						{
							this.dict[codename_bytes] = section.Parse(data_in, conf);
						}
						else
						{
							this.dict[codename_bytes] = section.Parse(data_in, conf/*, spsx_section=this.dict[SPSXSection.codename_bytes]*/);
						}
					}
					else
					{
						throw new Exception("No idea what to do here, the original code shouldn't work here");
						//this.dict[codename_bytes] = BaseWADSection.fallback_parse(data_in);
					}
				}
				else
				{
					//TODO: Throw expection or add general type

					//throw new Exception("No idea what to do here, the original code shouldn't work here");
					Console.WriteLine($"Skipping Chunk: {Encoding.ASCII.GetString((byte*)&codename_bytes, 4)}");
					//this.dict[codename_bytes] = BaseWADSection.fallback_parse(data_in);
				}
			}
			this.end_parse();
		}
		public override void serialize(object file_path_or_data_out, Configuration conf)
		{
			var data_out = (file_path_or_data_out is BinaryWriter writer) ? writer : new BinaryWriter(new MemoryStream());

			var wad_size_offset = (int)data_out.BaseStream.Position;
			data_out.Write((int)0);//b"\x00\x00\x00\x00"
			foreach(var section in this.dict.Values)
			{
				section.serialize(data_out, conf);
				//if(section.serialize == BaseWADSection.serialize)// FIXME Dirty
				//{
				//	section.fallback_serialize(data_out);
				//}
				//else
				//{
				//	section.serialize(data_out, conf);
				//}
			}
			var end_offset = (int)data_out.BaseStream.Position;
			var wad_size = end_offset - wad_size_offset;
			if(conf.game==G.CROC_2_PS1 || conf.game==G.HARRY_POTTER_1_PS1 || conf.game==G.HARRY_POTTER_2_PS1)
			{
				wad_size += 2048;
			}
			data_out.BaseStream.Position += wad_size_offset;
			data_out.Write((int)wad_size);
			data_out.BaseStream.Position += end_offset;

			if(file_path_or_data_out is string)
			{
				data_out.BaseStream.Position = 0;
				using var output_file = File.OpenWrite((string)file_path_or_data_out);
				data_out.BaseStream.CopyTo(output_file);
				data_out.Close();
			}
		}
	}
}