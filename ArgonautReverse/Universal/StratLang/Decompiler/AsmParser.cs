using System.Collections.Immutable;
using ArgonautReverse.PC;
using ArgonautReverse.Universal.StratLang.Disassembler;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public sealed class AsmParser(IReadOnlyList<Script> scripts)
	{
		public readonly Dictionary<AsmInstruction,Instruction> PsxInstructions = new();
		private readonly Dictionary<Script,Dictionary<AsmInstruction,Instruction>> PsxSubroutines = new();

		private readonly Queue<Instruction> NeedsSetup = new();

		private readonly IReadOnlyList<Script> scripts = scripts;

		private Instruction CreateInstruction(AsmInstruction psxLabel, AsmInstruction psxOperation)
		{
			var instr = InstructionLookup.CreateInstruction(label:psxLabel, operation:psxOperation);
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

		private void AddScript(Script script)
		{
			//Already added
			if(PsxSubroutines.ContainsKey(script)){return;}

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

		public void Process(string scriptsDirectory)
		{
			var scriptData = new (string? baseFilePath,bool parsingSucceeded,StackAnalyzer? stackAnalyzer,FlowAnalyzer? flowAnalyzer)[scripts.Count];
			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				var baseFilePath = Path.Join(scriptsDirectory, $"STRAT_{i}");

				//TODO: SKIP EVERYTHING EXCEPT for this WalkingCroc for testing
				//if((Stem, i) != ("0015F800",4)){continue;}

				var script = scripts[i];
				
				
				//TODO: Make exporting ASM an argument
				script.ExportAsm(baseFilePath);

				if(script.Failed)
				{
					continue;
				}
				data.baseFilePath = baseFilePath;
			}
			for(int i=0; i<scriptData.Length; i++)
			{
				if(scriptData[i].baseFilePath == null){continue;}

				unsafe
				{
					var data = scripts[i].Data;
					fixed(int* data0 = data)
					{
						File.WriteAllBytes($"{scriptData[i].baseFilePath}.strat.bytes", new Span<byte>(data0, data.Length*sizeof(int)));
					}
				}
			}

			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(data.baseFilePath == null || !scripts[i].Processed){continue;}
				AddScript(scripts[i]);
			}

			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(data.baseFilePath == null || !scripts[i].Processed){continue;}
				try
				{
					ParseAndSetupInstructions(scripts[i]);
					data.parsingSucceeded = true;
				}
				catch(Exception e)
				{
					//TODO: This could possibly break parsing.
					Console.WriteLine($"Failed to setup asm parser {data.baseFilePath}:");
					Console.WriteLine(e.Message);
					continue;
				}
			}

			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(!data.parsingSucceeded){continue;}
				try
				{
					//TODO: StackAanalyzer likely should be consistant across all scripts.
					//Failing during analysis could invalidate the values in the analyzer though.
					var stackAnalyzer = new StackAnalyzer();
					stackAnalyzer.Analyze(GetSubroutines(scripts[i]));

					data.stackAnalyzer = stackAnalyzer;
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to analyze stack {data.baseFilePath}:");
					Console.WriteLine(e.Message);
					continue;
				}
				var stackWriter = new Writer();
				data.stackAnalyzer.Write(stackWriter);

				File.WriteAllText($"{data.baseFilePath}.stack.strat", stackWriter.GetString());
			}
			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(data.stackAnalyzer == null){continue;}
				try
				{
					var flowAnalyzer = new FlowAnalyzer();
					flowAnalyzer.Analyze(data.stackAnalyzer.Subroutines);
					data.flowAnalyzer = flowAnalyzer;
				}
				catch(Exception e)
				{
					Console.WriteLine($"Failed to analyze flow {data.baseFilePath}:");
					Console.WriteLine(e.Message);
					continue;
				}
			}
			for(int i=0; i<scriptData.Length; i++)
			{
				ref var data = ref scriptData[i];
				if(data.flowAnalyzer == null){continue;}

				var flowWriter = new Writer();
				data.flowAnalyzer.Write(flowWriter);

				File.WriteAllText($"{data.baseFilePath}.flow.strat", flowWriter.GetString());
			}
		}
	}
}