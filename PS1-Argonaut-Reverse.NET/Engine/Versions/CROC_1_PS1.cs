using ArgonautReverse.IO;

namespace ArgonautReverse.Engine.Versions
{
	public sealed class CROC_1_PS1:VersionInfo
	{
		//Croc 1 PS1 NA Release
		public static CROC_1_PS1 Instance{get;} = new CROC_1_PS1();
		public override string Title => "Croc 1 PS1";
		public override DateTime BuildDate => new DateTime(1997, 9, 29);
		public override string FilenameDAT => "CROCFILE.1";
		public override string FilenameDIR => "CROCFILE.DIR";
		public override DirFormat DirFormat => DirFormat_Croc1.Instance;

		public override bool NEW_COLLISION => throw new NotSupportedException();

		private CROC_1_PS1(){}
	}

	public sealed class DirFormat_Croc1:DirFormat_Croc2
	{
		new public static readonly DirFormat_Croc1 Instance = new DirFormat_Croc1();

		private DirFormat_Croc1(){}

		public override void Pack(Serializer writer, string name, int size, int start)
		{
			//Same as Croc 2 but with 4 bytes of padding at the end
			//Struct("<12sII4x")
			base.Pack(writer, name, size, start);
			writer.WriteInt32(0);//Padding
		}

		public override void Unpack(WadReader reader, out string name, out int size, out int start)
		{
			//Same as Croc 2 but with 4 bytes of padding at the end
			//Struct("<12sII4x")
			base.Unpack(reader, out name, out size, out start);
			_ = reader.ReadInt32();//Padding
		}
	}
}
