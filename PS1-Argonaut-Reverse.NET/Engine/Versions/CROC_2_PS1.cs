using System.Text;
using ArgonautReverse.IO;

namespace ArgonautReverse.Engine.Versions
{
	public sealed class CROC_2_PS1:VersionInfo
	{
		//Croc 2 PS1 NA Release
		public static CROC_2_PS1 Instance{get;} = new CROC_2_PS1();

		public override string Title => "Croc 2 PS1";
		public override DateTime BuildDate => new DateTime(1999, 7, 1);
		public override string FilenameDAT => "CROCII.DAT";
		public override string FilenameDIR => "CROCII.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		public override bool NEW_COLLISION => false;

		private CROC_2_PS1(){}
	}

	public class DirFormat_Croc2:DirFormat
	{
		public static readonly DirFormat_Croc2 Instance = new DirFormat_Croc2();

		protected DirFormat_Croc2(){}

		public override void Unpack(WadReader reader, out string name, out int size, out int start)
		{
			//Little Endian, 12 byte string, followed by two 32-bit integers
			//Struct("<12sII")
			name = Encoding.ASCII.GetString(reader.ReadBytes(12));
			size = reader.ReadInt32();
			start = reader.ReadInt32();
		}
		public override void Pack(Serializer writer, string name, int size, int start)
		{
			//Little Endian, 12 byte string, followed by two 32-bit integers
			//Struct("<12sII")
			writer.WriteBytes(Encoding.ASCII.GetBytes(name.PadRight(12, '\0')));
			writer.WriteInt32(size);
			writer.WriteInt32(start);
		}
	}
}