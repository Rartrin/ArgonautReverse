using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.PSX.StratLang
{
	public abstract class AsmInstruction
	{
		public InstructionOpcode OpCode;

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

		//private:
		//std::string label;
		//public:
		public string GetLabel()
		{
			if(/*label.empty() && */JumpsFrom.Count>0)
			{
				/*label = */return $"Label_{(uint)InstrAddr:X8}";
			}
			return "";//label;
		}

		public string SubroutineName()
		{
			if(SubroutineType == SubroutineType.Proc)
			{
				return $"Proc_{(uint)InstrAddr:X8}";
			}
			else if(SubroutineType == SubroutineType.Strat)
			{
				if(Start)
				{
					return $"StratExternal_{(uint)InstrAddr:X8}";
				}
				return $"Strat_{(uint)InstrAddr:X8}";
			}
			else if(SubroutineType == SubroutineType.Trigger)
			{
				return $"Trigger_{(uint)InstrAddr:X8}";
			}
			else
			{
				return "";
			}
		}

		public AsmInstruction(InstructionAddress address, InstructionOpcode opcode, int operandCount,int popCount,int pushCount)
		{
			this.OpCode = opcode;
			this.OperandCount = operandCount;
			this.PopCount = popCount;
			this.PushCount = pushCount;

			InstrAddr = address;

			Next = null;
		}
		
		public virtual void Parse(StratReader reader){}
		public virtual void Setup(StratParser parser){}

		public abstract string ToAsmString(bool exportForParsing);

		public sealed override string ToString()
		{
			return $"{(uint)InstrAddr:X8} {base.ToString()}";
		}
	}
}