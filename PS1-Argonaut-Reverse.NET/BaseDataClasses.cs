using System.Text;
using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.WadSections;

namespace ArgonautReverse
{
	public interface BaseDataClass
	{
		//Add in Pack/Repack/Write
	}

	public abstract class BaseWADSectionInfo
	{
		//TODO: Separate into Read and Write
		public abstract WadVersion[] supported_games{get;}


		public abstract string section_content_description{get;}
		
		public abstract ChunkType ChunkType{get;}

		public BaseWADSectionInfo(){}

		public unsafe void check_codename(WadReader data_in)
		{
			var found_codename = (ChunkType)data_in.Read<uint>();
			if(found_codename != ChunkType)
			{
				throw new SectionNameError(data_in.Position, ChunkType.ToString(), Encoding.Latin1.GetString((byte*)&found_codename, 4));
			}
		}

		public void check_size(int expected_size, int section_start, int current_position)
		{
			var calculated_size = current_position - section_start;
			if(expected_size != calculated_size)
			{
				throw new SectionSizeMismatch(current_position, ChunkType.ToString(), expected_size, calculated_size);
			}
		}

		protected void parseInner(WadReader data_in, out int size, out int start)
		{
			if(!supported_games.Contains(data_in.ReadVersion))
			{
				throw new UnsupportedParsing(section_content_description);
			}
			check_codename(data_in);
			size = data_in.ReadInt32();
			start = data_in.Position;
		}
		public abstract BaseWADSection Parse(WadReader data_in);

		protected static byte[] fallback_parse_data(WadReader data_in)
		{
			var start = data_in.Position;
			var chunkType = data_in.Read<uint>();
			var size = data_in.Read<int>();

			//var data = codename + size + data_in.read(int.from_bytes(size, "little"));
			data_in.Position = start;
			var data = data_in.ReadBytes(8 + size);
			data_in.Position = start;
			return data;
		}

		public abstract BaseWADSection fallback_parse(WadReader data_in);
	}
	public abstract class BaseWADSectionInfo<T>:BaseWADSectionInfo where T:BaseWADSection
	{
		public sealed override BaseWADSection fallback_parse(WadReader data_in)
		{
			return (T)Activator.CreateInstance(typeof(T), this, fallback_parse_data(data_in));
		}
	}

	public abstract class BaseWADSection:BaseDataClass
	{
		public readonly BaseWADSectionInfo Info;
		public WadVersion[] supported_games => Info.supported_games;
		public string section_content_description => Info.section_content_description;

		public byte[] _data;

		public BaseWADSection(BaseWADSectionInfo info, byte[] data = null)
		{
			Info = info;
			if(data is not null)
			{
				this._data = data;
			}
		}

	
		public virtual void serialize(Serializer data_out)
		{
			fallback_serialize(data_out);
		}
		protected int serializeInner(Serializer data_out)
		{
			if(!this.supported_games.Contains(data_out.WriteVersion))
			{
				throw new UnsupportedSerialization(this.section_content_description);
			}
			data_out.Write((uint)Info.ChunkType);
			data_out.Write<uint>(0);// Section's size
			return data_out.Position;
		}

		protected static void SerializeSectionSize(Serializer data_out, int start)
		{
			var end = data_out.Position;
			var size = end - start;
			data_out.Position = start - 4;
			data_out.WriteInt32(size);
			data_out.Position = end;
		}

		public void fallback_serialize(Serializer data_out)
		{
			data_out.WriteBytes(this._data);
		}
	}

	public sealed class UnknownSectionInfo:BaseWADSectionInfo<UnknownSection>
	{
		public override WadVersion[] supported_games => Configuration.ALL_WADS;

		public override string section_content_description => $"{ChunkType.GetRawName()} chunk";

		public override ChunkType ChunkType{get;}

		public unsafe UnknownSectionInfo(ChunkType chunkType)
		{
			ChunkType = chunkType;
		}

		public override UnknownSection Parse(WadReader data_in)
		{
			return (UnknownSection)fallback_parse(data_in);
		}
	}

	public sealed class UnknownSection:BaseWADSection
	{
		public UnknownSection(BaseWADSectionInfo info, byte[] data = null) : base(info, data){}
	}
}