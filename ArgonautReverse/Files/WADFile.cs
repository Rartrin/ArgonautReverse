using System.Diagnostics.CodeAnalysis;
using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.OpenStratEngine;
using ArgonautReverse.PC;
using ArgonautReverse.PSX;
using ArgonautReverse.WadChunks;

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
	public abstract class WADFile(WadVersion version, string stem, byte[] data):DATFile(stem, data),IConvertibleToOSE<WadFileOSE>
	{
		private readonly List<BaseWadChunk> chunks = new List<BaseWadChunk>();

		public override string Suffix => "WAD";

		public WadVersion Version => version;

		public static WADFile Create(DatVersion datVerion, WadVersion wadVersion, string stem, byte[] data) => datVerion.Platform switch
		{
			Platform.PC => new WadFilePC(wadVersion, stem, data),
			Platform.PSX => new WadFilePSX(wadVersion, stem, data),
			_ => throw new NotImplementedException($"Platform not implemented: {datVerion.Platform}"),
		};

		public abstract bool TryGetChunkInfo(ChunkType chunkType, [MaybeNullWhen(false)]out BaseWADChunkInfo info);
		
		public virtual void AddChunk(BaseWadChunk chunk)
		{
			chunks.Add(chunk);
		}

		public abstract T GetChunk<T>(BaseWADChunkInfo<T> info) where T:BaseWadChunk;

		private sealed class ChunkLocation(BaseWADChunkInfo info, int dataStart, int dataLength)
		{
			public BaseWADChunkInfo Info = info;
			public int DataStart = dataStart;
			public int DataLength = dataLength;
		}

		private unsafe List<ChunkLocation> LocateChunks(WadReader reader)
		{
			var chunkLocations = new List<ChunkLocation>();
			
			//This should be the length of the WAD's data but it is wrong in some cases so we can't rely on it.
			_ = reader.Read<int>();

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
						reader.Position -= 4;//Unread the chunkType
						reader.Position -= 4;//Move back 4 bytes.
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

		public override void Parse(ProgramArgs args, Configuration conf)
		{
			var data_in = new WadReader(this, conf, conf.ReadVersion.GetWadVersion(Stem), this._data);
			
			var chunkLocations = LocateChunks(data_in);

			var chunkTypesRead = new HashSet<ChunkType>();
			this.chunks.Clear();
			foreach(var chunkLocation in chunkLocations)
			{
				data_in.Position = chunkLocation.DataStart;
				var chunkReader = data_in.ReadChunk(chunkLocation.DataLength);
				
				if(!chunkTypesRead.Add(chunkLocation.Info.ChunkType))
				{
					throw new Exception($"Chunk {chunkLocation.Info.ChunkType} already read");
				}
				var chunk = chunkLocation.Info.Parse(chunkReader);
				this.AddChunk(chunk);
				if(chunkReader.Remaining != 0)
				{
					Console.WriteLine($"WARNING: There were {chunkReader.Remaining} bytes of unparsed data in {chunkLocation.Info.ChunkType}!");
				}
				chunk.PostParseSetup(this);
			}
		}

		public WadFileOSE ToOSE()
		{
			var ose = new WadFileOSE(Stem);
			foreach(var chunk in chunks)
			{
				foreach(var oseChunk in chunk.ToOSE())
				{
					ose.AddChunk(oseChunk);
				}
			}
			return ose;
		}

		public void Write(ProgramArgs args, Configuration conf)
		{
			var writer = new WadWriter(this, conf, conf.WriteVersion!.GetWadVersion(null), new());
			var wadStart = writer.Position;
			var wadSize = writer.WriteHold<int>();

			foreach(var chunk in chunks)
			{
				chunk.Write(writer.GetChunkWriter());
			}

			var wadEnd = writer.Position;
			wadSize.Set(wadEnd - wadStart);
			throw new Exception("Remember to write writer stream to file.");
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
			data_out.Position = wad_size_offset;
			data_out.WriteInt32(wad_size);
			data_out.Position = end_offset;
		}
	}
}