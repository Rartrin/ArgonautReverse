using System.Diagnostics.CodeAnalysis;
using ArgonautReverse.PSX.StratLang;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public abstract class Instruction
	{
		public AsmInstruction AsmLabel;
		public AsmInstruction AsmOperation;

		//During parsing, this is the line number on the input file.
		public int Index;

		public Instruction AsmPrev;
		public Instruction AsmNext;

		public Instruction RawAsmPrev;
		public Instruction RawAsmNext;

		//Indicated that this instructions terminates the current flow.
		//It can still redirect to a new flow though, like with jump/goto.
		public virtual bool Terminal => false;

		public bool Start = false;

		public bool SetupDone = false;

		//TODO: Make these sets?
		public readonly List<Instruction> JumpsFrom = new List<Instruction>();
		public readonly List<Instruction> CallsFrom = new List<Instruction>();//Proc
		public readonly List<Instruction> ReferencedFrom = new List<Instruction>();//Strat, Trigger

		public SubroutineType SubroutineType = SubroutineType.None;

		public virtual void Create(AsmInstruction label, AsmInstruction operation)
		{
			AsmLabel = label;
			AsmOperation = operation;

			if(label.IsSubroutineEntry)
			{
				SubroutineType = label.SubroutineType;
				Start = label.Start;
			}
			if(label.HasLabel)
			{
				//TODO: Remove this once I'm sure
				if(this is IStackConsumer)
				{
					throw new Exception("Conjecture failed: Label on a Consumer");
				}
			}
		}

		public virtual void Setup(AsmParser parser){}

		public abstract bool TryGetSubroutine([MaybeNullWhen(false)]out AsmInstruction subroutine);

		public abstract bool TryGetLabel([MaybeNullWhen(false)]out AsmInstruction label);

		public TAsmInstruction GetAsmInstruction<TAsmInstruction>() where TAsmInstruction:AsmInstruction => (TAsmInstruction)AsmOperation;
	}
}