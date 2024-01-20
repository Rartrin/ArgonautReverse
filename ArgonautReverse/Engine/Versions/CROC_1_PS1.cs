using System.Text;
using ArgonautReverse.IO;

namespace ArgonautReverse.Engine.Versions
{
	//Croc 1 PS1 NA Release
	public static class CROC_1_PS1
	{
		public static DatVersion DatVersion => CROC_1_PS1_Dat.Instance;
		public static WadVersion WadVersion => CROC_1_PS1_Wad.Instance;
		public static DirFormat DirFormat => CROC_1_PS1_DirFormat.Instance;

		private sealed class CROC_1_PS1_Dat:DatVersion
		{
			public static readonly DatVersion Instance = new CROC_1_PS1_Dat();

			public override string Title => "Croc 1 PS1";

			public override string FilenameDAT => "CROCFILE.1";
			public override string FilenameDIR => "CROCFILE.DIR";
			public override DirFormat DirFormat => CROC_1_PS1.DirFormat;

			public override WadVersion GetWadVersion(string wadName) => WadVersion;

			public override IReadOnlyList<WadVersion> WadVersions{get;} = new[]{WadVersion};
		}

		private sealed class CROC_1_PS1_Wad:WadVersion
		{
			public static readonly WadVersion Instance = new CROC_1_PS1_Wad();

			public override DateTime BuildDate => new DateTime(1997, 9, 29);
			
			public override bool NEW_COLLISION => throw new NotSupportedException();
		}

		private sealed class CROC_1_PS1_DirFormat:DirFormat
		{
			public static readonly DirFormat Instance = new CROC_1_PS1_DirFormat();

			//Little Endian, 12 byte string, followed by two 32-bit integers, and 4 bytes of padding
			//This is the same as Croc 2 but with padding at the end
			//Struct("<12sII4x")
			public override void Unpack(BaseReader reader, out string name, out int size, out int start)
			{
				name = reader.ReadString(12);
				size = reader.Read<int>();
				start = reader.Read<int>();
				reader.AssertRead<int>(0);//padding
			}

			public override void Pack(Serializer writer, string name, int size, int start)
			{
				writer.WriteBytes(Encoding.ASCII.GetBytes(name.PadRight(12, '\0')));
				writer.WriteInt32(size);
				writer.WriteInt32(start);
				writer.WriteInt32(0);//Padding
			}
		}
	}
}
