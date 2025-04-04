using ArgonautReverse.PSX.StratLang;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public abstract class Instruction
	{
		public AsmInstruction RawLabel;
		public AsmInstruction RawOperation;

		//During parsing, this is the line number on the input file.
		public int Index;

		public string? AsmSubroutineName;
		public string? AsmLabelName;

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

		//TODO: Make these sets?
		public readonly List<Instruction> JumpsFrom = new List<Instruction>();
		public readonly List<Instruction> CallsFrom = new List<Instruction>();//Proc
		public readonly List<Instruction> ReferencedFrom = new List<Instruction>();//Strat, Trigger

		public SubroutineType SubroutineType = SubroutineType.None;

		public virtual void Create(string rawInstruction, AsmInstruction label, AsmInstruction operation)
		{
			RawLabel = label;
			RawOperation = operation;

			int endOfInst = rawInstruction.IndexOf(' ');

			//Space should use uses exclusively as arg separators
			//EXCEPT for DebugName and PrintInstructions where that can be located in strings.
			//Should be easy for DebugName to just join the args back together for it with spaces.
			if(endOfInst >= 0)
			{
				RawArgs = rawInstruction.Substring(endOfInst + 1).Split(' ');
			}
			else
			{
				RawArgs = [];
			}

			if(label.SubroutineType != SubroutineType.None)
			{
				SubroutineType = label.SubroutineType;
				Start = label.Start;
				AsmSubroutineName = label.SubroutineName();
			}
			if(label.JumpsFrom.Count > 0)
			{
				AsmLabelName = label.GetLabel();
				//TODO: Remove this once I'm sure
				if(this is IStackConsumer)
				{
					throw new Exception("Conjecture failed: Label on a Consumer");
				}
			}
		}

		public virtual void Setup(AsmParser parser){}

		public abstract bool TryGetSubroutineName(out string subroutineName);

		public abstract bool TryGetLabel(out string label);

		public TAsmInstruction GetAsmInstruction<TAsmInstruction>() where TAsmInstruction:AsmInstruction => (TAsmInstruction)RawOperation;
	}
}