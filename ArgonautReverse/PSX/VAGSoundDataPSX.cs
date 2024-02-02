using System.Text;
using ArgonautReverse.IO;

namespace ArgonautReverse.PSX
{
	public sealed class VAGSoundDataPSX
	{
		public const int MONO = 1;
		public const int STEREO = 2;
		public static readonly (double _0, double _1)[] constants = new[]
		{
			(0.0, 0.0),
			(60.0 / 64.0, 0.0),
			(115.0 / 64.0, -52.0 / 64.0),
			(98.0 / 64.0, -55.0 / 64.0),
			(122.0 / 64.0, -60.0 / 64.0),
		};

		private static readonly byte[] vagpBytes = Encoding.ASCII.GetBytes("VAGp");
		private static readonly byte[] riffBytes = Encoding.ASCII.GetBytes("RIFF");
		private static readonly byte[] waveBytes = Encoding.ASCII.GetBytes("WAVE");
		private static readonly byte[] fmtBytes = Encoding.ASCII.GetBytes("fmt ");
		private static readonly byte[] dataBytes = Encoding.ASCII.GetBytes("data");


		public readonly byte[] data;
		public readonly int n_channels;
		public readonly uint sampling_rate;

		private static unsafe void WriteInt32BE(BaseWriter writer, int value)
		{
			var bytes = (byte*)&value;
			for(int i = 3; i >= 0; i--)
			{
				writer.Write(bytes[i]);
			}
		}

		public VAGSoundDataPSX(byte[] data, int n_channels, uint sampling_rate)
		{
			if(n_channels != MONO && n_channels != STEREO)
			{
				throw new Exception("Unsuppported number of channels");
			}
			this.data = data;
			this.n_channels = n_channels;
			this.sampling_rate = sampling_rate;
		}

		public int size => data.Length;

		public static VAGSoundDataPSX parse(WadReader data_in, int size, int n_channels, uint sampling_rate)
		{
			var data = data_in.ReadArray<byte>(size);
			return new VAGSoundDataPSX
			(
				data,
				n_channels,
				sampling_rate
			);
		}

		public void serialize(WadWriter data_out)
		{
			data_out.WriteBytes(data);
		}

		public byte[][] to_vag(bool with_headers = true)
		{
			byte[] header;
			if(with_headers)
			{
				header = new byte[48];
				using var headerStream = new IO.StreamWriter(new MemoryStream(header), 0, true);
				headerStream.WriteBytes(vagpBytes);
				headerStream.SkipBytes(8);//TODO: What is this?
				WriteInt32BE(headerStream, size / n_channels);
				WriteInt32BE(headerStream, (int)sampling_rate);

				//TODO: What is this data here for? This is 28 bytes in total.
				headerStream.SkipBytes(10);
				headerStream.Write<byte>(1);
				headerStream.Write<byte>(0);
				headerStream.WriteBytes(Encoding.ASCII.GetBytes("OverSurgeReverse"));
			}
			else
			{
				header = Array.Empty<byte>();
			}
			int header_size = 48;

			if(n_channels == 1)
			{
				byte[] ret;
				if(!with_headers)
				{
					ret = data;
				}
				else
				{
					ret = new byte[header.Length + data.Length];
					header.CopyTo(ret, 0);
					data.CopyTo(ret, header.Length);
				}

				return new byte[1][] { ret };
			}
			else
			{
				var trackDataSize = header_size + size / 2;
				var tracks = new byte[2][]
				{
					new byte[trackDataSize],
					new byte[trackDataSize]
				};
				if(with_headers)
				{
					header.CopyTo(tracks[0], 0);
					header.CopyTo(tracks[1], 0);
				}

				//Swaps tracks every 1024 bytes
				for(int dataIndex = 0; dataIndex < size; dataIndex += 2048)
				{
					var trackIndex = header_size + dataIndex / 2;
					data.AsSpan(dataIndex + 0, 1024).CopyTo(tracks[0].AsSpan(trackIndex, 1024));
					data.AsSpan(dataIndex + 1024, 1024).CopyTo(tracks[1].AsSpan(trackIndex, 1024));
					//tracks[0][trackIndex .. (trackIndex + 1024)] = this.data[dataIndex .. (dataIndex + 1024)];
					//tracks[1][trackIndex .. (trackIndex + 1024)] = this.data[(dataIndex + 1024) .. (dataIndex + 2048)];
				}
				return tracks;
			}
		}

		/// <summary>Supports stereo export in a single file, unlike to_vag().</summary>
		public byte[] to_wav(string filename)// TODO Poor performance
		{
			var vag = to_vag(false);

			var byte_rate = sampling_rate * n_channels * 2;
			var block_align = n_channels * 2;
			var audio_data_size = size * 7 / 2;// VAG -> WAV has a 3.5 size ratio

			var total_wav_size = 44 + audio_data_size;
			var ret = new byte[total_wav_size];
			using(var headerStream = new IO.StreamWriter(new MemoryStream(ret), 0, true))
			{
				//RIFF header
				headerStream.WriteBytes(riffBytes);
				headerStream.WriteInt32(total_wav_size - 8);
				headerStream.WriteBytes(waveBytes);

				//fmt chunk
				headerStream.WriteBytes(fmtBytes);
				headerStream.WriteInt32(16);//Chunk size
				headerStream.WriteUInt16(1);//Format code: 1 = WAVE_FORMAT_PCM
				headerStream.WriteUInt16((ushort)n_channels);
				headerStream.WriteInt32((int)sampling_rate);
				headerStream.WriteInt32((int)byte_rate);
				headerStream.WriteUInt16((ushort)block_align);
				headerStream.WriteUInt16(0x0010);//Bits per sample

				//data chunk
				headerStream.WriteBytes(dataBytes);
				headerStream.WriteInt32(audio_data_size);

				if(headerStream.Position != 44)
				{
					throw new Exception("Header size invalid");
				}
			}

			byte[][] channels;
			if(n_channels == 1)
			{
				channels = null;
			}
			else
			{
				channels = new byte[2][]
				{
					new byte[audio_data_size / 2],
					new byte[audio_data_size / 2]
				};
			}
			// Based on VAG-Depack 0.1 by bITmASTER
			for(int c = 0; c < n_channels; c++)
			{
				var s_1 = 0.0;
				var s_2 = 0.0;
				var samples = new double[28];
				for(int i = 16; i < size / n_channels; i += 16)
				{
					var predict_nr = vag[c][i];
					var shift_factor = predict_nr & 0xF;
					predict_nr >>= 4;
					var flags = vag[c][i + 1];
					if(flags == 7)
					{
						break;
					}
					for(int j = 0; j < 28; j += 2)
					{
						var d = vag[c][i + 2 + j / 2];
						short s0 = (short)((d & 0xF) << 12);
						samples[j + 0] = s0 >> shift_factor;
						short s1 = (short)((d & 0xF0) << 8);
						samples[j + 1] = s1 >> shift_factor;
					}
					for(int j = 0; j < 28; j++)
					{
						samples[j] +=

							s_1 * constants[predict_nr]._0
							+ s_2 * constants[predict_nr]._1
						;
						s_2 = s_1;
						s_1 = samples[j];
						var d = (int)(samples[j] + 0.5);
						var wav_pos = i * 7 / 2 + j * 2;
						if(n_channels == 1)
						{
							ret[44 + wav_pos + 0] = (byte)d;
							ret[44 + wav_pos + 1] = (byte)(d >> 8);
						}
						else
						{
							channels[c][wav_pos + 0] = (byte)d;
							channels[c][wav_pos + 1] = (byte)(d >> 8);
						}
					}
					if(flags == 1)
					{
						break;
					}
				}
			}
			if(n_channels == 2)
			{
				for(int i = 0; i < audio_data_size / 2; i += 2)
				{
					int retIndex = 44 + 2 * i;
					ret[retIndex + 0] = channels[0][i + 0];
					ret[retIndex + 1] = channels[0][i + 1];
					ret[retIndex + 2] = channels[1][i + 0];
					ret[retIndex + 3] = channels[1][i + 1];
				}
			}
			return ret;
		}
	}
}