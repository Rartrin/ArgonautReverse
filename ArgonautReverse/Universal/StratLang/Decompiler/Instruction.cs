using ArgonautReverse.Universal.StratLang.Disassembler;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public abstract class Instruction
	{
		public AsmInstruction AsmLabel
		{
			get;
			init
			{
				field = value;
				if(value.IsSubroutineEntry)
				{
					SubroutineType = value.SubroutineType;
					Start = value.Start;
				}
				if(value.HasLabel)
				{
					//TODO: Remove this once I'm sure
					if(this is IStackConsumer)
					{
						throw new Exception("Conjecture failed: Label on a Consumer");
					}
				}
			}
		}
		public AsmInstruction AsmOperation{get;init;}

		//During parsing, this is the line number on the input file.
		public int Index;

		public Instruction AsmPrev;
		public Instruction AsmNext;

		public Instruction? RawAsmPrev;
		public Instruction RawAsmNext;

		//Indicates that this instruction terminates the current flow.
		//It can still redirect to a new flow though, like with jump/goto.
		public virtual bool Terminal => false;

		public readonly bool Start = false;

		public bool SetupDone = false;

		//TODO: Make these sets?
		public readonly List<Instruction> JumpsFrom = new List<Instruction>();
		public readonly List<Instruction> CallsFrom = new List<Instruction>();//Proc
		public readonly List<Instruction> ReferencedFrom = new List<Instruction>();//Strat, Trigger

		public readonly SubroutineType SubroutineType = SubroutineType.None;

		public abstract IStackOperation StackOperation{get;}

		public virtual void Setup(AsmParser parser){}

		public TAsmInstruction GetAsmInstruction<TAsmInstruction>() where TAsmInstruction:AsmInstruction => (TAsmInstruction)AsmOperation;
	}
}