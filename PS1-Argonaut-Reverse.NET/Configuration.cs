using System.Text;

namespace ArgonautReverse
{
	public abstract class DirFormat
	{
		public abstract void Pack(Serializer writer, string name, int size, int start);
		public abstract void Unpack(Parser reader, out string name, out int size, out int start);
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
		
		public override void Unpack(Parser reader, out string name, out int size, out int start)
		{
			//Same as Croc 2 but with 4 bytes of padding at the end
			//Struct("<12sII4x")
			base.Unpack(reader, out name, out size, out start);
			_ = reader.ReadInt32();//Padding
		}
	}
	public class DirFormat_Croc2:DirFormat
	{
		public static readonly DirFormat_Croc2 Instance = new DirFormat_Croc2();

		protected DirFormat_Croc2(){}

		public override void Unpack(Parser reader, out string name, out int size, out int start)
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
	public abstract class Game
	{
		public abstract string Title{get;}
		public abstract DateTime BuildDate{get;}
		public abstract string FilenameDAT{get;}
		public abstract string FilenameDIR{get;}
		public abstract DirFormat DirFormat{get;}
	}

	public sealed class CROC_1_PS1:Game
	{
		//Croc 1 PS1 NA Release
		public static CROC_1_PS1 Instance{get;} = new CROC_1_PS1();
		public override string Title => "Croc 1 PS1";
		public override DateTime BuildDate => new DateTime(1997, 9, 29);
		public override string FilenameDAT => "CROCFILE.1";
		public override string FilenameDIR => "CROCFILE.DIR";
		public override DirFormat DirFormat => DirFormat_Croc1.Instance;

		private CROC_1_PS1(){}
	}
	public sealed class CROC_2_PS1:Game
	{
		//Croc 2 PS1 NA Release
		public static CROC_2_PS1 Instance{get;} = new CROC_2_PS1();

		public override string Title => "Croc 2 PS1";
		public override DateTime BuildDate => new DateTime(1999, 7, 1);
		public override string FilenameDAT => "CROCII.DAT";
		public override string FilenameDIR => "CROCII.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		private CROC_2_PS1(){}
	}
	public sealed class CROC_2_DEMO_PS1:Game
	{
		//Croc 2 PS1 US Demo
		public static CROC_2_DEMO_PS1 Instance{get;} = new CROC_2_DEMO_PS1();

		public override string Title => "Croc 2 Demo PS1";
		public override DateTime BuildDate => new DateTime(1999, 3, 4);
		public override string FilenameDAT =>"CROCII.DAT";
		public override string FilenameDIR => "CROCII.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		private CROC_2_DEMO_PS1(){}
	}
	public sealed class CROC_2_DEMO_PS1_DUMMY:Game
	{
		//Croc 2 PS1 US Demo's DUMMY.DAT
		public static CROC_2_DEMO_PS1_DUMMY Instance{get;} = new CROC_2_DEMO_PS1_DUMMY();

		public override string Title => "Croc 2 Demo PS1 (Dummy)";
		public override DateTime BuildDate => new DateTime(1999, 3, 4);
		public override string FilenameDAT =>"DUMMY.DAT";
		public override string FilenameDIR => null;
		public override DirFormat DirFormat => null;

		private CROC_2_DEMO_PS1_DUMMY(){}
	}
	public sealed class HARRY_POTTER_1_PS1:Game
	{
		public static HARRY_POTTER_1_PS1 Instance{get;} = new HARRY_POTTER_1_PS1();

		public override string Title => "Harry Potter 1 PS1";
		public override DateTime BuildDate => new DateTime(2001, 12, 1);
		public override string FilenameDAT =>"POTTER.DAT";
		public override string FilenameDIR => "POTTER.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		private HARRY_POTTER_1_PS1(){}
	}
	public sealed class HARRY_POTTER_2_PS1:Game
	{
		public static HARRY_POTTER_2_PS1 Instance{get;} = new HARRY_POTTER_2_PS1();

		public override string Title => "Harry Potter 2 PS1";
		public override DateTime BuildDate => new DateTime(2002, 11, 5);
		public override string FilenameDAT =>"POTTER.DAT";
		public override string FilenameDIR => "POTTER.DIR";
		public override DirFormat DirFormat => DirFormat_Croc2.Instance;

		private HARRY_POTTER_2_PS1(){}
	}

	public sealed class Configuration
	{
		public readonly Game game;
		public readonly bool ignore_warnings;
		public readonly bool debug;
		public Configuration(Game game, bool ignore_warnings = false, bool debug = false)
		{
			this.game = game;
			this.ignore_warnings = ignore_warnings;
			//logging.basicConfig(format="%(message)s", level=logging.DEBUG if debug else logging.WARNING)
			this.debug = debug;
		}

		public static readonly Game[] SUPPORTED_GAMES = new Game[]
		{
			CROC_1_PS1.Instance,
			CROC_2_PS1.Instance,
			CROC_2_DEMO_PS1.Instance,
			CROC_2_DEMO_PS1_DUMMY.Instance,
			HARRY_POTTER_1_PS1.Instance,
			HARRY_POTTER_2_PS1.Instance,
		};
		//Croc 1 parsing is not supported, but it can be sliced
		public static readonly Game[] PARSABLE_GAMES = new Game[]
		{
			CROC_2_PS1.Instance,
			CROC_2_DEMO_PS1.Instance,
			CROC_2_DEMO_PS1_DUMMY.Instance,
			HARRY_POTTER_1_PS1.Instance,
			HARRY_POTTER_2_PS1.Instance,
		};
		public static readonly Game[] SLICEABLE_GAMES = SUPPORTED_GAMES;
	}
}