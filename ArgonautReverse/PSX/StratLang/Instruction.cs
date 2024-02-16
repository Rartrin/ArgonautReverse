using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.PSX.StratLang
{
	public abstract class Instruction
	{
		public InstructionOpcode OpCode;

		//Operands that are part of the instruction
		public int OperandCount;

		public int PopCount;
		public int PushCount;

		//Whether or not this could potentially continue to the next instruction.
		//Errors, returns, and unconditional jumps do not;
		public bool Terminal = false;

		public Instruction Prev;
		public Instruction Next;

		//This points to the value AFTER the OpCode in the original instruction stream. In other words, this is only the operands.
		public readonly InstructionAddress InstrAddr;

		public bool Done = false;
		public bool Start = false;

		public readonly List<Instruction> JumpsFrom = new List<Instruction>();

		public readonly List<Instruction> CallsFrom = new List<Instruction>();//Proc

		public readonly List<Instruction> ReferencedFrom = new List<Instruction>();//Strat, Trigger
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
					return $"ExternalStrat_{(uint)InstrAddr:X8}";
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

		public Instruction(InstructionAddress address, InstructionOpcode opcode, int operandCount,int popCount,int pushCount)
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
	}
}
