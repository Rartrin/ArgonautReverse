using System.Text;
using ArgonautReverse.Engine.Mappings;
using ArgonautReverse.IO;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine.Versions
{
	//Croc 2 PS1 NA Release
	public static class CROC_2_PS1
	{
		public static DatVersion DatVersion => CROC_2_PS1_Dat.Instance;
		public static WadVersion WadVersion => CROC_2_PS1_Wad.Instance;
		public static DirFormat DirFormat => CROC_2_PS1_DirFormat.Instance;

		private sealed class CROC_2_PS1_Dat:DatVersionPSX
		{
			public static readonly DatVersion Instance = new CROC_2_PS1_Dat();

			public override string Title => "Croc 2 PS1";
			public override string FilenameDAT => "CROCII.DAT";
			public override string FilenameDIR => "CROCII.DIR";
			public override DirFormat DirFormat => CROC_2_PS1.DirFormat;

			public override WadVersion GetWadVersion(string? wadName) => WadVersion;

			public override IReadOnlyList<WadVersion> WadVersions{get;} = [WadVersion];
		}

		private sealed class CROC_2_PS1_Wad:WadVersion
		{
			public static readonly WadVersion Instance = new CROC_2_PS1_Wad();

			public override DateTime BuildDate => new DateTime(1999, 7, 1);
			
			public override bool NEW_COLLISION => false;
			public override bool KEYFRAME_STUFF => false;
            public override bool HAS_SPLINE_POINTS => false;
            public override bool HAS_STRAT_ARRAY_POOL => false;

			public override InstructionOpcode MapOpcode(int value) => MapperCroc2.OpcodeMapper(value);
			public override TriggerType MapTriggerType(int value) => MapperCroc2.TriggerTypeMapper(value);
		}

		public sealed class CROC_2_PS1_DirFormat:DirFormat
		{
			public static readonly DirFormat Instance = new CROC_2_PS1_DirFormat();

			//Little Endian, 12 byte string, followed by two 32-bit integers
			//Struct("<12sII")
			public override void Unpack(BaseReader reader, out string name, out int size, out int start)
			{
				name = reader.ReadString(12);
				size = reader.Read<int>();
				start = reader.Read<int>();
			}
			public override void Pack(WadWriter writer, string name, int size, int start)
			{
				writer.WriteBytes(Encoding.ASCII.GetBytes(name.PadRight(12, '\0')));
				writer.WriteInt32(size);
				writer.WriteInt32(start);
			}

			private CROC_2_PS1_DirFormat(){}
		}
	}
}