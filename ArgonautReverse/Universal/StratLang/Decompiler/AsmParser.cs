using System.Collections.Immutable;
using ArgonautReverse.PSX.StratLang;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public sealed class AsmParser
	{
		public readonly Dictionary<AsmInstruction,Instruction> PsxInstructions = new();
		private readonly Dictionary<AsmInstruction,Instruction> PsxSubroutines = new();

		private readonly Queue<Instruction> NeedsSetup = new();

		public Instruction CreateInstruction(AsmInstruction psxLabel, AsmInstruction psxOperation)
		{
			var instr = InstructionLookup.CreateInstruction(psxOperation.OpCode);
			instr.Create(psxLabel, psxOperation);
			PsxInstructions.Add(psxLabel, instr);

			if(instr.SubroutineType != SubroutineType.None)
			{
				PsxSubroutines.Add(psxLabel, instr);
			}

			return instr;
		}

		public Instruction GetInstruction(AsmInstruction psxInstr, Instruction? prev, Instruction? jumpFrom)
		{
			var ret = PsxInstructions[psxInstr];

			if(ret.AsmPrev == null && prev != null)
			{
				ret.AsmPrev = prev;
			}

			if(jumpFrom != null)
			{
				ret.JumpsFrom.Add(jumpFrom);
			}
			if(!ret.SetupDone)
			{
				NeedsSetup.Enqueue(ret);
			}
			return ret;
		}
		public Instruction GetStrat(AsmInstruction psxInstr, Instruction? referenced)
		{
			var retInstruction = GetInstruction(psxInstr, null, null);
			if(referenced != null)
			{
				retInstruction.ReferencedFrom.Add(referenced);
			}
			return retInstruction;
		}
		public Instruction GetProc(AsmInstruction psxInstr, Instruction callFrom)
		{
			var retInstruction = GetInstruction(psxInstr, null, null);
			retInstruction.CallsFrom.Add(callFrom);
			return retInstruction;
		}
		public Instruction GetTrigger(AsmInstruction psxInstr, Instruction referenced)
		{
			var retInstruction = GetInstruction(psxInstr, null, null);
			retInstruction.ReferencedFrom.Add(referenced);
			return retInstruction;
		}

		public void SetupInstruction(Instruction instruction)
		{
			if(instruction.SetupDone){return;}

			instruction.Setup(this);

			if(!instruction.Terminal)
			{
				instruction.AsmNext = GetInstruction(instruction.AsmOperation.Next!, instruction, null);
				//instruction.AsmNext = GetInstruction(instruction.Index + 1, instruction, null);
			}

			instruction.SetupDone = true;
		}

		public Instruction ParseAndSetupInstructions(IReadOnlyList<(AsmInstruction label,AsmInstruction operation)> lines)
		{
			var instructions = CreateInstructions(lines);

			foreach(var subroutine in PsxSubroutines.Values)
			{
				GetStrat(subroutine.AsmLabel, null);//Trigger setup
				while(NeedsSetup.TryDequeue(out var instr))
				{
					SetupInstruction(instr);
				}
			}
			return instructions.First(i => i.Start);
		}

		private List<Instruction> CreateInstructions(IReadOnlyList<(AsmInstruction label,AsmInstruction operation)> lines)
		{
			int lineNumber = 0;

			var instructions = new List<Instruction>(lines.Count);
			for(int i=0; i<lines.Count; i++)
			{
				(var label, var operation) = lines[i];

				var instr = CreateInstruction(label, operation);
				instr.Index = lineNumber;
				instructions.Add(instr);
				lineNumber++;
			}

			for(int i=1; i<instructions.Count; i++)
			{
				var instruction = instructions[i];
				var prev = instructions[i-1];
				instruction.RawAsmPrev = prev;
				prev.RawAsmNext = instruction;
			}
			return instructions;
		}

		public IReadOnlyList<Instruction> GetSubroutines()
		{
			return PsxSubroutines.Values.OrderBy(i => i.Index).ToImmutableList();
		}
	}
}