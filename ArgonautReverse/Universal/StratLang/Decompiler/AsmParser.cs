using System.Collections.Immutable;
using ArgonautReverse.PSX.StratLang;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public sealed class AsmParser
	{
		public readonly Dictionary<AsmInstruction,Instruction> PsxInstructions = new();
		private readonly Dictionary<Script,Dictionary<AsmInstruction,Instruction>> PsxSubroutines = new();

		private readonly Queue<Instruction> NeedsSetup = new();

		private Instruction CreateInstruction(AsmInstruction psxLabel, AsmInstruction psxOperation)
		{
			var instr = InstructionLookup.CreateInstruction(psxOperation.OpCode);
			instr.Create(psxLabel, psxOperation);
			PsxInstructions.Add(psxLabel, instr);

			if(instr.SubroutineType != SubroutineType.None)
			{
				PsxSubroutines[psxLabel.Script].Add(psxLabel, instr);
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

		public void ParseAndSetupInstructions(Script script)
		{
			foreach(var subroutine in PsxSubroutines[script].Values)
			{
				GetStrat(subroutine.AsmLabel, null);//Trigger setup
				while(NeedsSetup.TryDequeue(out var instr))
				{
					SetupInstruction(instr);
				}
			}
		}

		public void AddScript(Script script)
		{
			PsxSubroutines.Add(script, new());

			var lines = script.Parser.GetInstructions();

			int lineNumber = 0;

			var instructions = new Instruction[lines.Count];
			for(int i=0; i<lines.Count; i++)
			{
				(var label, var operation) = lines[i];

				var instr = CreateInstruction(label, operation);
				instr.Index = lineNumber;
				instructions[i] = instr;
				lineNumber++;
			}

			for(int i=1; i<instructions.Length; i++)
			{
				var instruction = instructions[i];
				var prev = instructions[i-1];
				instruction.RawAsmPrev = prev;
				prev.RawAsmNext = instruction;
			}
		}

		public IReadOnlyList<Instruction> GetSubroutines(Script script)
		{
			return PsxSubroutines[script].Values.OrderBy(i => i.Index).ToImmutableList();
		}
	}
}