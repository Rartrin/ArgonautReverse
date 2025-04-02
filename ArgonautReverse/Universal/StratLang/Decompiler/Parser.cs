using System.Collections.Immutable;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public sealed class Parser
	{
		public Instruction[] Instructions;

		private readonly Dictionary<string,Instruction> Subroutines = new Dictionary<string,Instruction>();

		private readonly Dictionary<string,Instruction> Labels = new Dictionary<string,Instruction>();

		private readonly Queue<Instruction> NeedsSetup = new Queue<Instruction>();

		public Instruction CreateInstruction(string rawInstr)
		{
			var labels = rawInstr.Split(':',3);

			int endOfInst = labels[2].IndexOf(' ');
			var instrName = endOfInst>=0 ? labels[2].Substring(0, endOfInst) : labels[2];
			var instr = InstructionLookup.CreateInstruction(instrName);

			//Space should use uses exclusively as arg separators
			//EXCEPT for DebugName and PrintInstructions where that can be located in strings.
			//Should be easy for DebugName to just join the args back together for it with spaces.
			if(endOfInst >= 0)
			{
				instr.RawArgs = labels[2].Substring(endOfInst + 1).Split(' ');
			}
			else
			{
				instr.RawArgs = Array.Empty<string>();
			}

			if(labels[0].Length != 0)//Subroutine
			{
				instr.AsmSubroutineName = labels[0];
				if(instr.AsmSubroutineName.StartsWith("Trigger")){instr.SubroutineType = SubroutineType.Trigger;}
				else if(instr.AsmSubroutineName.StartsWith("Proc")){instr.SubroutineType = SubroutineType.Proc;}
				else if(instr.AsmSubroutineName.StartsWith("Strat"))
				{
					if(instr.AsmSubroutineName.StartsWith("StratMain") || instr.AsmSubroutineName.StartsWith("StratExternal"))
					{
						instr.Start = true;
					}
					instr.SubroutineType = SubroutineType.Strat;
				}
				else
				{
					throw new Exception("Unknown SR type");
				}
				Subroutines.Add(instr.AsmSubroutineName, instr);
			}
			if(labels[1].Length != 0)
			{
				instr.AsmLabelName = labels[1];
				Labels.Add(instr.AsmLabelName, instr);

				//TODO: Remove this once I'm sure
				if(instr is IStackConsumer)
				{
					throw new Exception("Conjecture failed: Label on a Consumer");
				}
			}

			return instr;
		}

		public Instruction GetLabeledInstruction(string labelName, Instruction prev, Instruction jumpFrom)
		{
			return GetInstruction(Labels[labelName].Index, prev, jumpFrom);
		}

		public Instruction GetInstruction(int lineNumber, Instruction prev, Instruction jumpFrom)
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

		public Instruction GetStrat(string subroutineName, Instruction referenced)
		{
			return GetStrat(Subroutines[subroutineName].Index, referenced);
		}
		public Instruction GetStrat(int lineNumber, Instruction referenced)
		{
			var retInstruction = GetInstruction(lineNumber, null, null);
			if(referenced != null)
			{
				retInstruction.ReferencedFrom.Add(referenced);
			}
			return retInstruction;
		}
		public Instruction GetProc(string subroutineName, Instruction callFrom)
		{
			return GetProc(Subroutines[subroutineName].Index, callFrom);
		}
		public Instruction GetProc(int lineNumber, Instruction callFrom)
		{
			var retInstruction = GetInstruction(lineNumber, null, null);
			retInstruction.CallsFrom.Add(callFrom);
			return retInstruction;
		}
		public Instruction GetTrigger(string subroutineName, Instruction referenced)
		{
			return GetTrigger(Subroutines[subroutineName].Index, referenced);
		}
		public Instruction GetTrigger(int lineNumber, Instruction referenced)
		{
			var retInstruction = GetInstruction(lineNumber, null, null);
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

		public Instruction ParseAndSetupInstructions(string[] lines)
		{
			Instructions = CreateInstructions(lines);

			foreach(var subroutine in Subroutines.Values)
			{
				GetStrat(subroutine.Index, null);//Trigger setup
				while(NeedsSetup.TryDequeue(out var instr))
				{
					SetupInstruction(instr);
				}
			}
			return Instructions.First(i => i.Start);
		}


		private Instruction[] CreateInstructions(string[] lines)
		{
			int lineNumber = 0;

			var instructions = new List<Instruction>(lines.Length);
			for(int i=0; i<lines.Length; i++)
			{
				if(string.IsNullOrWhiteSpace(lines[i]))
				{
					continue;
				}

				var instr = CreateInstruction(lines[i]);
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