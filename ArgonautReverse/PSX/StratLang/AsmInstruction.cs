using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.PSX.StratLang
{
	public abstract class AsmInstruction
	{
		public readonly Script Script;

		public readonly InstructionOpcode OpCode;

		//Operands that are part of the instruction
		public int OperandCount;

		public int PopCount;
		public int PushCount;

		//False if this could potentially continue to the next instruction.
		//Errors, returns, and unconditional jumps are terminal.
		public bool Terminal = false;

		public AsmInstruction? Prev;
		public AsmInstruction? Next;

		//This points to the value AFTER the OpCode in the original instruction stream. In other words, this is only the operands.
		public readonly InstructionAddress InstrAddr;

		public bool Done = false;
		public bool Start = false;

		public readonly List<AsmInstruction> JumpsFrom = new List<AsmInstruction>();

		public readonly List<AsmInstruction> CallsFrom = new List<AsmInstruction>();//Proc

		public readonly List<AsmInstruction> ReferencedFrom = new List<AsmInstruction>();//Strat, Trigger
		public SubroutineType SubroutineType = SubroutineType.None;

		public virtual bool Export => true;

		public bool HasLabel => JumpsFrom.Count != 0;
		public string GetLabel()
		{
			if(!HasLabel){throw new Exception("Instruction does not have a label");}
			return $"Label_{(uint)InstrAddr:X8}";
		}

		public bool IsSubroutineEntry => SubroutineType != SubroutineType.None;
		public string SubroutineName() => SubroutineType switch
		{
			SubroutineType.Proc => $"Proc_{(uint)InstrAddr:X8}",
			SubroutineType.Strat => $"{(Start ? "StratExternal_" : "Strat_")}{(uint)InstrAddr:X8}",
			SubroutineType.Trigger => $"Trigger_{(uint)InstrAddr:X8}",
			SubroutineType.None => throw new Exception("Instruction is not a subroutine"),
			_ => throw new NotImplementedException($"Unimplemented subroutine type: {SubroutineType}")
		};

		public AsmInstruction(Script script, InstructionAddress address, InstructionOpcode opcode, int operandCount,int popCount,int pushCount)
		{
			Script = script;
			OpCode = opcode;
			OperandCount = operandCount;
			PopCount = popCount;
			PushCount = pushCount;

			InstrAddr = address;

			Next = null;
		}
		
		public virtual void Parse(StratReader reader){}
		public virtual void Setup(StratParser parser){}

		public abstract string ToAsmString(bool exportForParsing);

		public sealed override string ToString()
		{
			if(IsSubroutineEntry)
			{
				return SubroutineName();
			}
			if(HasLabel)
			{
				return GetLabel();
			}
			return $"{(uint)InstrAddr:X8} {OpCode}";
		}
	}
}