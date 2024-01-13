namespace ArgonautReverse.Engine.Versions
{
	//public sealed class CROC_2_DEMO_PS1_DUMMY:WadVersion
	//{
	//	//Croc 2 PS1 US Demo's DUMMY.DAT
	//	//The files in here were almost certainly built prior to the rest of the demo
	//	public static CROC_2_DEMO_PS1_DUMMY Instance{get;} = new CROC_2_DEMO_PS1_DUMMY();

	//	public override string Title => "Croc 2 Demo PS1 (Dummy)";
	//	//Actual date is unknown but it was prior to the demo
	//	public override DateTime BuildDate => new DateTime(1999, 3, 3);
	//	public override string FilenameDAT => "DUMMY.DAT";
	//	public override string FilenameDIR => null;
	//	public override DirFormat DirFormat => null;

	//	public override bool NEW_COLLISION => false;

	//	private CROC_2_DEMO_PS1_DUMMY(){}
	//}

	//Croc 2 PS1 US Demo's DUMMY.DAT
	//The files in here were almost certainly built prior to the rest of the demo
	public static class CROC_2_DEMO_PS1_DUMMY
	{
		public static DatVersion DatVersion => CROC_2_DEMO_PS1_DUMMY_Dat.Instance;
		public static DirFormat DirFormat => CROC_2_PS1.DirFormat;

		public static WadVersion WadVersion_00BD7800 => CROC_2_DEMO_PS1_DUMMY_Wad.Instance_00BD7800;
		public static WadVersion WadVersion_Latest => CROC_2_DEMO_PS1_DUMMY_Wad.Instance_Latest;

		private sealed class CROC_2_DEMO_PS1_DUMMY_Dat:DatVersion
		{
			public static readonly DatVersion Instance = new CROC_2_DEMO_PS1_DUMMY_Dat();

			public override string Title => "Croc 2 Demo PS1 (Dummy)";
			public override string FilenameDAT => "DUMMY.DAT";
			public override string FilenameDIR => null;
			public override DirFormat DirFormat => null;

			public override WadVersion GetWadVersion(string wadName) => CROC_2_DEMO_PS1_DUMMY_Wad.wadVersions.GetValueOrDefault(wadName, WadVersion_Latest);

			public override IReadOnlyCollection<WadVersion> WadVersions => CROC_2_DEMO_PS1_DUMMY_Wad.wadVersions.Values;
		}

		private sealed class CROC_2_DEMO_PS1_DUMMY_Wad:WadVersion
		{
			public static readonly Dictionary<string,WadVersion> wadVersions = new Dictionary<string, WadVersion>();

			public override DateTime BuildDate{get;}
			
			public override bool NEW_COLLISION => false;

			private CROC_2_DEMO_PS1_DUMMY_Wad(int buildVersionOrder, params string[] wadNames)
			{
				if(buildVersionOrder<0 || 1000<=buildVersionOrder)
				{
					throw new Exception();
				}
				//Actual dates are unknown but it was prior to the demo which was 1999, 3, 4
				BuildDate = new DateTime(1999, 3, 3, 0, 0, 0, buildVersionOrder);

				foreach(var wadName in wadNames)
				{
					wadVersions.Add(wadName, this);
				}
			}

			//Version used in: 00BD7800, 01864000
			public static readonly WadVersion Instance_00BD7800 = new CROC_2_DEMO_PS1_DUMMY_Wad(100,
				"00BD7800",//Dino Fight
				"01864000",//Early snow hub
				"01BEF000",//Early Sledding/Snowman roll
				"01CBF000"//Widgies
			);
			//Version used by most
			public static readonly WadVersion Instance_Latest = new CROC_2_DEMO_PS1_DUMMY_Wad(999, "__Latest__");
		}
	}
}