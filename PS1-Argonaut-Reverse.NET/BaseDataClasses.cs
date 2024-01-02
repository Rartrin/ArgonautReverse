//from io import BufferedIOBase


//from ps1_argonaut.errors_warnings import (
//    SectionNameError,
//    SectionSizeMismatch,
//    UnsupportedParsing,
//    UnsupportedSerialization,
//)


using System.Text;

namespace ArgonautReverse
{
	public interface BaseDataClass
	{
		//public /*static*/ abstract void parse(BinaryReader data_in, Configuration conf/*, *args, **kwargs*/);//BufferedIOBase

		//public abstract void serialize(BinaryWriter data_out, Configuration conf/*, *args, **kwargs*/);//BufferedIOBase
	}

	public abstract class BaseWADSectionInfo
	{
		public abstract G[] supported_games{get;}//: tuple[G, ...]
		public abstract string section_content_description{get;}
		//Big endian
		public abstract string codename_str{get;}
		//Little Endian
		public readonly byte[] codename_bytes;
		public readonly uint codename_raw;

		public unsafe BaseWADSectionInfo()
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

		//@classmethod
		public unsafe void check_codename(BinaryReader data_in)//BufferedIOBase
		{
			var found_codename = data_in.ReadUInt32();
			if(found_codename != codename_raw)
			{
				throw new SectionNameError((int)data_in.BaseStream.Position, codename_str, Encoding.Latin1.GetString((byte*)&found_codename, 4));
			}
		}

		//@classmethod
		public void check_size(int expected_size, int section_start, int current_position)
		{
			var calculated_size = current_position - section_start;
			if(expected_size != calculated_size)
			{
				throw new SectionSizeMismatch(current_position, codename_str, expected_size, calculated_size);
			}
		}
		//@classmethod
		protected (int,int) parseInner(BinaryReader data_in, Configuration conf/*, *args, **kwargs*/)//BufferedIOBase
		{
			if(!supported_games.Contains(conf.game))
			{
				throw new UnsupportedParsing(section_content_description);
			}
			check_codename(data_in);
			return (data_in.ReadInt32(), (int)data_in.BaseStream.Position);
		}
		public abstract BaseWADSection parse(Parser data_in, Configuration conf/*, *args, **kwargs*/);//BufferedIOBase

		//@staticmethod
		public static void serialize_section_size(BinaryWriter data_out, int start)//BufferedIOBase
		{
			var end = (int)data_out.BaseStream.Position;
			var size = end - start;
			data_out.BaseStream.Position = start - 4;
			data_out.Write((int)size);
			data_out.BaseStream.Position = end;
		}

		//@classmethod
		public static byte[] fallback_parse_data(Parser data_in)//BufferedIOBase
		{
			var start = (int)data_in.Position;
			var codename = data_in.ReadUInt32();
			var size = data_in.ReadInt32();

			//var data = codename + size + data_in.read(int.from_bytes(size, "little"));
			data_in.Position = start;
			var data = data_in.ReadBytes(8 + size);
			data_in.Position = start;
			return data;
		}

		//@classmethod
		public abstract BaseWADSection fallback_parse(Parser data_in);//BufferedIOBase
		//{
		//	return Activator.CreateInstance(, fallback_parse_data(data_in));
		//}
	}
	public abstract class BaseWADSectionInfo<T>:BaseWADSectionInfo where T:BaseWADSection
	{
		public sealed override BaseWADSection fallback_parse(Parser data_in)
		{
			return (T)Activator.CreateInstance(typeof(T), fallback_parse_data(data_in));
		}
	}

	public abstract class BaseWADSection:BaseDataClass
	{
		public readonly BaseWADSectionInfo Info;
		public G[] supported_games => Info.supported_games;
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

	
		public virtual void serialize(BinaryWriter data_out, Configuration conf/*, *args, **kwargs*/)//BufferedIOBase
		{
			fallback_serialize(data_out);
		}
		protected int serializeInner(BinaryWriter data_out, Configuration conf/*, *args, **kwargs*/)//BufferedIOBase
		{
			if(!this.supported_games.Contains(conf.game))
			{
				throw new UnsupportedSerialization(this.section_content_description);
			}
			data_out.Write(codename_bytes);
			data_out.Write((int)0);// Section's size
			return (int)data_out.BaseStream.Position;
		}

		public void fallback_serialize(BinaryWriter data_out)//BufferedIOBase
		{
			data_out.Write(this._data);
		}
	}
}