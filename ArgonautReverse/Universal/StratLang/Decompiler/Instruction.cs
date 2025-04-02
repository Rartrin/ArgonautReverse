namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public enum SubroutineType
	{
		None,
		Strat,
		Proc,
		Trigger
	}

	public abstract class Instruction
	{
		//During parsing, this is the line number on the input file.
		public int Index;

		public string AsmSubroutineName;
		public string AsmLabelName;

		public Instruction AsmPrev;
		public Instruction AsmNext;

		public Instruction RawAsmPrev;
		public Instruction RawAsmNext;

		//Indicated that this instructions terminates the current flow.
		//It can still redirect to a new flow though, like with jump/goto.
		public virtual bool Terminal => false;

		public bool Start = false;

		public bool SetupDone = false;

		public string[] RawArgs;

		public readonly List<Instruction> JumpsFrom = new List<Instruction>();

		public readonly List<Instruction> CallsFrom = new List<Instruction>();//Proc

		public readonly List<Instruction> ReferencedFrom = new List<Instruction>();//Strat, Trigger
		public SubroutineType SubroutineType = SubroutineType.None;

		public virtual void Setup(Parser parser){}

		public abstract bool TryGetSubroutineName(out string subroutineName);

		public abstract bool TryGetLabel(out string label);

		public override string ToString()
		{
			return base.ToString();
		}
	}
}