using System.Text;
using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse
{
	public interface BaseDataClass
	{
		//Add in Pack/Repack/Write
	}

	public abstract class BaseWADSectionInfo
	{
		public abstract VersionInfo[] supported_games{get;}
		public abstract string section_content_description{get;}
		//Big endian
		public abstract string codename_str{get;}
		//Little Endian
		public byte[] codename_bytes{get;protected set;}
		public uint codename_raw{get;protected set;}

		public unsafe BaseWADSectionInfo()
		{
			if(codename_str != null)
			{
				if(codename_str.Length!=4){throw new Exception();}
				codename_bytes = new byte[4];
				for(int i=0; i<4; i++)
				{
					//String is in big endian for display so we need to swap it
					codename_bytes[i] = (byte)codename_str[4-(i+1)];
				}
				codename_raw = BitConverter.ToUInt32(codename_bytes);
			}
		}

		public unsafe void check_codename(WadReader data_in)
		{
			var found_codename = data_in.ReadUInt32();
			if(found_codename != codename_raw)
			{
				throw new SectionNameError(data_in.Position, codename_str, Encoding.Latin1.GetString((byte*)&found_codename, 4));
			}
		}

		public void check_size(int expected_size, int section_start, int current_position)
		{
			var calculated_size = current_position - section_start;
			if(expected_size != calculated_size)
			{
				throw new SectionSizeMismatch(current_position, codename_str, expected_size, calculated_size);
			}
		}

		protected (int,int) parseInner(WadReader data_in)
		{
			if(!supported_games.Contains(data_in.Version))
			{
				throw new UnsupportedParsing(section_content_description);
			}
			check_codename(data_in);
			return (data_in.ReadInt32(), data_in.Position);
		}
		public abstract BaseWADSection Parse(WadReader data_in);

		protected static byte[] fallback_parse_data(WadReader data_in)
		{
			var start = data_in.Position;
			var codename = data_in.ReadUInt32();
			var size = data_in.ReadInt32();

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
		public VersionInfo[] supported_games => Info.supported_games;
		public string section_content_description => Info.section_content_description;
		public string codename_str => Info.codename_str;
		public byte[] codename_bytes => Info.codename_bytes;
		public uint codename_raw => Info.codename_raw;

		public byte[] _data;

		public BaseWADSection(BaseWADSectionInfo info, byte[] data = null)
		{
			Info = info;
			if(data is not null)
			{
				this._data = data;
			}
		}

	
		public virtual void serialize(Serializer data_out, Configuration conf)
		{
			fallback_serialize(data_out);
		}
		protected int serializeInner(Serializer data_out, Configuration conf)
		{
			//TODO: InputVersion used in serialize
			if(!this.supported_games.Contains(conf.InputVersion))
			{
				throw new UnsupportedSerialization(this.section_content_description);
			}
			data_out.WriteBytes(codename_bytes);
			data_out.WriteInt32(0);// Section's size
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
		public override VersionInfo[] supported_games => Configuration.SUPPORTED_GAMES;

		public override string section_content_description => $"{codename_str} chunk";
		public override string codename_str{get;}

		public unsafe UnknownSectionInfo(uint codename_raw)
		{
			Span<char> str = stackalloc char[4];
			byte* codename_raw0 = (byte*)&codename_raw;
			for(int i = 0; i < 4; i++)
			{
				str[3-i] = (char)codename_raw0[i];
			}
			this.codename_str = new string(str);
			this.codename_raw = codename_raw;
			this.codename_bytes = BitConverter.GetBytes(codename_raw);
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