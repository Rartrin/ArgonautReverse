using System.Collections.Immutable;
using ArgonautReverse.PSX.StratLang;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public sealed class AsmParser
	{
		public Instruction[] Instructions;
		public readonly Dictionary<AsmInstruction,Instruction> PsxInstructions = new();

		private readonly Dictionary<string,Instruction> Subroutines = new();
		private readonly Dictionary<AsmInstruction,Instruction> PsxSubroutines = new();

		private readonly Dictionary<string,Instruction> Labels = new();

		private readonly Queue<Instruction> NeedsSetup = new();

		public Instruction CreateInstruction(AsmInstruction psxLabel, AsmInstruction psxOperation)
		{
			var instr = InstructionLookup.CreateInstruction(psxOperation.OpCode);
			instr.Create(psxLabel, psxOperation);
			PsxInstructions.Add(psxLabel, instr);

			if(instr.SubroutineType != SubroutineType.None)
			{
				Subroutines.Add(instr.AsmSubroutineName, instr);
				PsxSubroutines.Add(psxLabel, instr);
			}
			if(!string.IsNullOrEmpty(instr.AsmLabelName))
			{
				Labels.Add(instr.AsmLabelName, instr);
			}

			return instr;
		}

		public Instruction GetInstruction(int lineNumber, Instruction? prev, Instruction? jumpFrom)
		{
			var ret = Instructions[lineNumber];

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
				var lineNumber = instruction.Index;

				instruction.AsmNext = GetInstruction(lineNumber+1, instruction, null);
			}

			instruction.SetupDone = true;
		}

		public Instruction ParseAndSetupInstructions(IReadOnlyList<(AsmInstruction label,AsmInstruction operation)> lines)
		{
			Instructions = CreateInstructions(lines);

			foreach(var subroutine in Subroutines.Values)
			{
				GetStrat(subroutine.RawLabel, null);//Trigger setup
				while(NeedsSetup.TryDequeue(out var instr))
				{
					SetupInstruction(instr);
				}
			}
			return Instructions.First(i => i.Start);
		}

		private Instruction[] CreateInstructions(IReadOnlyList<(AsmInstruction label,AsmInstruction operation)> lines)
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
			return instructions.ToArray();
		}

		public IReadOnlyList<Instruction> GetSubroutines()
		{
			return Subroutines.Values.OrderBy(i => i.Index).ToImmutableList();
		}
	}
}