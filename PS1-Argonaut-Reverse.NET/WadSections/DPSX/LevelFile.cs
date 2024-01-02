//from io import BufferedIOBase, SEEK_CUR

//from ps1_argonaut.BaseDataClasses import BaseDataClass
//from ps1_argonaut.configuration import Configuration, G
//from ps1_argonaut.wad_sections.DPSX.ChunkClasses import (
//    ChunkHolder,
//    ChunkRotation,
//    ChunksMatrix,
//    SubChunk,
//)
//from ps1_argonaut.wad_sections.DPSX.Model3DData import LevelGeom3DData
//from ps1_argonaut.wad_sections.DPSX.Model3DHeader import Model3DHeader


using System.Text;
using Compat;
using static Compat.Compat;

namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class LevelFile:BaseDataClass
	{
		public readonly ChunksMatrix chunks_matrix;

		public LevelFile(ChunksMatrix chunks_matrix)
		{
			this.chunks_matrix = chunks_matrix;
		}

		//@classmethod
		public static LevelFile parse(Parser data_in, Configuration conf/*, *args, **kwargs*/)//BufferedIOBase
		{
			//base.parse(data_in, conf);
			var n_chunk_models = data_in.ReadInt32();
			var _chunk_model_headers = new Model3DHeader[n_chunk_models];
			for(int i=0; i<n_chunk_models; i++)
			{
				_chunk_model_headers[i] = Model3DHeader.parse(data_in, conf);
			}
			var chunk_models = new LevelGeom3DData[n_chunk_models];
			for(int i=0; i<n_chunk_models; i++)
			{
				chunk_models[i] = LevelGeom3DData.parse(data_in, conf, header:_chunk_model_headers[i]);
			}
			if(conf.game != G.CROC_2_DEMO_PS1_DUMMY)
			{
				data_in.seek(8, SEEK_CUR);
			}
			var n_sub_chunks = data_in.ReadInt32();

			var n_idk1 = data_in.ReadInt32();
			data_in.seek(4 * n_idk1, SEEK_CUR);
			Utils.Assert(n_sub_chunks == data_in.ReadInt32());
			var n_actors_instances = data_in.ReadUInt16();
			data_in.seek((conf.game != G.CROC_2_DEMO_PS1_DUMMY) ? 6 : 2, SEEK_CUR);
			var n_total_chunks = data_in.ReadInt32();
			var n_chunk_columns = data_in.ReadInt32();
			var n_chunk_rows = data_in.ReadInt32();
		
			ushort? n_lighting_headers;
			ushort? n_add_sub_chunks_lighting;
			if(conf.game != G.CROC_2_DEMO_PS1_DUMMY)
			{
				n_lighting_headers = data_in.ReadUInt16();
				n_add_sub_chunks_lighting = data_in.ReadUInt16();
				var idk3 = data_in.ReadInt32();
			}
			else
			{
				n_lighting_headers = null;
				n_add_sub_chunks_lighting = null;
			}
			var n_idk4 = data_in.ReadInt32();
			data_in.seek((conf.game != G.CROC_2_DEMO_PS1_DUMMY) ? 116 : 80, SEEK_CUR);

			var _chunks_matrix = new object[n_total_chunks];//list[list[int] | int | None]
			for(int i=0; i<n_total_chunks; i++)
			{
				_chunks_matrix[i] = data_in.ReadUInt32();
			}
		
			var _sub_chunks_height_0 = new Dictionary<uint,(int _0, int _1)>();
			var _sub_chunks_height = new Dictionary<uint,int>();

			var chunks_info_start_offset = data_in.tell();

			IReadOnlyList<int> parse_chunks_info(int offset, List<int> chunks_ids_list)
			{
				data_in.seek(chunks_info_start_offset + offset);
				chunks_ids_list.Add(data_in.ReadInt32());
				var linked_chunk_offset = data_in.ReadUInt32();
				if(linked_chunk_offset != 0xFFFFFFFF)
				{
					return parse_chunks_info((int)linked_chunk_offset, chunks_ids_list);
				}
				else
				{
					return chunks_ids_list;
				}
			}
			for(int i=0; i<n_chunk_rows; i++)
			{
				for(int j=0; j<n_chunk_columns; j++)
				{
					var index = i * n_chunk_columns + j;
					var chunk_info_offset = (uint)_chunks_matrix[index];
					if(chunk_info_offset != 0xFFFFFFFF)
					{
						var sub_chunk_ids = parse_chunks_info((int)chunk_info_offset, new List<int>());
						_chunks_matrix[index] = sub_chunk_ids;
						foreach(var sub_chunk_id in sub_chunk_ids)
						{
							_sub_chunks_height_0[(uint)sub_chunk_id] = (i, j);
						}
					}
					else
					{
						_chunks_matrix[index] = null;
					}
				}
			}
			data_in.seek(chunks_info_start_offset + 8 * n_sub_chunks);
			int[] zone_ids;
			byte[][] fvw_data;
			if(conf.game != G.CROC_2_DEMO_PS1_DUMMY)
			{
				var header256bytes = data_in.ReadBytes(256);
				var n_zone_ids = data_in.ReadInt32();
				zone_ids = new int[n_zone_ids];
				for(int i=0; i<n_zone_ids; i++)
				{
					zone_ids[i] = data_in.ReadInt32();
				}
				Utils.Assert(n_zone_ids == n_total_chunks);

				if(Encoding.ASCII.GetString(data_in.ReadBytes(4)) == "fvw\x00")
				{
					fvw_data = new byte[n_total_chunks][];
					for(int i=0; i<n_total_chunks;i++)
					{
						fvw_data[i] = data_in.ReadBytes(2);
					}
				}
				else
				{
					fvw_data = null;
					data_in.seek(-4, SEEK_CUR);
				}
			}
			else
			{
				zone_ids = null;
				fvw_data = null;
			}
			var _sub_chunks_rotation = new Dictionary<int,ChunkRotation>();
			for(int i=0; i<n_sub_chunks; i++)
			{
				var rotation = data_in.ReadUInt32BE();
				Utils.Assert(rotation==0 || rotation==4 || rotation==8 || rotation==12);
				_sub_chunks_rotation[i] = (ChunkRotation)rotation;
				Utils.Assert(data_in.ReadInt32() == 0);//b"\x00\x00\x00\x00"
				var x = data_in.ReadInt32();
				var y = data_in.ReadInt32();
				var z = data_in.ReadInt32();
				Utils.Assert(data_in.ReadInt32() == 0);//b"\x00\x00\x00\x00"
				Utils.Assert(x == 2048 + 4096 * _sub_chunks_height_0[(uint)i]._1);  // Chunks are 4096-large, so +2048 for the chunk's center
				Utils.Assert(z == 2048 + 4096 * _sub_chunks_height_0[(uint)i]._0);
				_sub_chunks_height[(uint)i] = y;
			}
			var chunks_models_mapping = data_in.ReadUInt32Array(n_sub_chunks);

			byte[][] lighting_headers = null;
			if(conf.game != G.CROC_2_DEMO_PS1_DUMMY)
			{
				lighting_headers = data_in.ReadArrayOfByteArrays(84, n_lighting_headers.Value);
			}

			var idk_4 = data_in.ReadArrayOfByteArrays(36, n_idk4);

			for(int i=0; i<n_actors_instances; i++)
			{
				data_in.seek(24, SEEK_CUR);
				var actor_offset = data_in.ReadInt32();
				data_in.seek(32, SEEK_CUR);
				var actor_sound_level = data_in.ReadInt32();
			}
			int[] add_models_mapping;
			if(conf.game!=G.CROC_2_DEMO_PS1 && conf.game!=G.CROC_2_DEMO_PS1_DUMMY)
			{
				add_models_mapping = new int[n_add_sub_chunks_lighting.Value];
				for(int i=0; i<n_add_sub_chunks_lighting; i++)
				{
					data_in.seek(16, SEEK_CUR);
					add_models_mapping[i] = data_in.ReadInt32();
					data_in.seek(4, SEEK_CUR);
				}
				var n_idk2 = data_in.ReadInt32();
				data_in.seek(32 * n_idk2, SEEK_CUR);  // TODO Reverse this
			}
			else
			{
				add_models_mapping = null;
				data_in.seek(32 * n_sub_chunks, SEEK_CUR);  // Two different 32-bytes long structures
				data_in.seek(32 * n_sub_chunks, SEEK_CUR);
				data_in.seek((conf.game == G.CROC_2_DEMO_PS1) ? 32 : 92, SEEK_CUR);
			}
			if(conf.game == G.CROC_2_PS1)
			{
				data_in.seek(30732, SEEK_CUR);
			}
			else if(conf.game != G.CROC_2_DEMO_PS1_DUMMY && n_sub_chunks != 0)
			{
				var sub_chunks_n_lighting = data_in.ReadUInt32Array(n_sub_chunks);
				var sub_chunks_n_add_lighting = data_in.ReadUInt32Array(n_add_sub_chunks_lighting.Value);
				for(int model_id=0; model_id<n_sub_chunks; model_id++)
				{
					for(int i=0; i<sub_chunks_n_lighting[model_id]; i++)
					{
						var size = 4 * chunk_models[chunks_models_mapping[model_id]].n_vertices;
						data_in.seek(size, SEEK_CUR);
					}
				}
				for(int model_id=0; model_id<n_add_sub_chunks_lighting; model_id++)
				{
					for(int i=0; i<sub_chunks_n_add_lighting[model_id]; i++)
					{
						var size = 4 * chunk_models[add_models_mapping[model_id]].n_vertices;
						data_in.seek(size, SEEK_CUR);
					}
				}
				if(conf.game != G.CROC_2_DEMO_PS1)// Not present in Croc 2 Demo Dummy
				{
					var idk_size = data_in.ReadInt32();
					if(idk_size != 0)
					{
						data_in.seek(4 + idk_size, SEEK_CUR);
					}
					else
					{
						data_in.seek(-4, SEEK_CUR);
					}
					var n_idk3 = data_in.ReadInt32();
					if(n_idk3 == 0)
					{
						data_in.seek(-4, SEEK_CUR);
					}
					var idk3 = data_in.ReadArrayOfByteArrays(40, n_idk3);//var idk3 = [int.from_bytes(data_in.read(40), "little") for _ in range(n_idk3)]
				}
				data_in.seek(12, SEEK_CUR);
			}
			var chunks_holders = new ChunkHolder[n_total_chunks];
			for(int i=0; i<n_total_chunks; i++)
			{
				if(_chunks_matrix[i] is not null)
				{
					var sub_chunks = ((IReadOnlyList<int>)_chunks_matrix[i]).Select(sub_chunk_id => new SubChunk(
						chunk_models[chunks_models_mapping[sub_chunk_id]],
						_sub_chunks_height[(uint)sub_chunk_id],
						_sub_chunks_rotation[sub_chunk_id]
					)).ToList();

					if(conf.game != G.CROC_2_DEMO_PS1_DUMMY)
					{
						chunks_holders[i] = new ChunkHolder(sub_chunks, zone_ids[i], fvw_data!=null ? fvw_data[i] : null);
					}
					else
					{
						chunks_holders[i] = new ChunkHolder(sub_chunks);
					}
				}
				else
				{
					if(conf.game != G.CROC_2_DEMO_PS1_DUMMY)
					{
						chunks_holders[i] = new ChunkHolder(zone_id:zone_ids[i], fvw_data: fvw_data!=null ? fvw_data[i] : null);
					}
					else
					{
						chunks_holders[i] = new ChunkHolder();
					}
				}
			}
			return new LevelFile
			(
				new ChunksMatrix
				(
					chunks_holders,
					chunk_models,
					n_chunk_rows,
					n_chunk_columns,
					zone_ids is not null
				)
			);
		}
	}
}