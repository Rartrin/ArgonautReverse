using ArgonautReverse.Files;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Universal.StratLang.Disassembler
{
	public sealed class StratParser(WADFile wadFile, Script script)
	{
		private readonly Queue<AsmInstruction> needsSetup = new Queue<AsmInstruction>();

		private readonly Dictionary<InstructionAddress,AsmInstruction> instructions = new Dictionary<InstructionAddress,AsmInstruction>();

		private readonly Dictionary<InstructionAddress,AsmInstruction> subroutines = new Dictionary<InstructionAddress,AsmInstruction>();

		private readonly Dictionary<int,AsmInstruction> triggers = new Dictionary<int,AsmInstruction>();

		public readonly WADFile WadFile = wadFile;
		public readonly Script Script = script;

		public AsmInstruction ParseInstruction(InstructionAddress instrAddr, AsmInstruction? prev, AsmInstruction? jumpFrom)
		{
			//Check if instruction was already processed
			if(instructions.TryGetValue(instrAddr, out var ret))
			{
				if(ret.Prev == null && prev != null)
				{
					ret.Prev = prev;
				}
			}
			else
			{
				var reader = new StratReader(WadFile, Script, instrAddr);
				ret = CreateInstruction(reader, prev);
				instructions.Add(instrAddr, ret);

				needsSetup.Enqueue(ret);
			}

			if(jumpFrom != null)
			{
				ret.JumpsFrom.Add(jumpFrom);
			}
			return ret;
		}

		public AsmInstruction ParseStrat(InstructionAddress instrAddr, AsmInstruction? referenced, bool start)
		{
			var retInstruction = ParseInstruction(instrAddr, null, null);
			retInstruction.SubroutineType = SubroutineType.Strat;
			retInstruction.Start = start;
			if(referenced != null)
			{
				retInstruction.ReferencedFrom.Add(referenced);
			}
			subroutines[instrAddr] = retInstruction;
			return retInstruction;
		}
		public AsmInstruction ParseProc(InstructionAddress instrAddr, AsmInstruction callFrom)
		{
			var retInstruction = ParseInstruction(instrAddr, null, null);
			retInstruction.SubroutineType = SubroutineType.Proc;
			retInstruction.CallsFrom.Add(callFrom);
			subroutines[instrAddr] = retInstruction;
			return retInstruction;
		}
		public AsmInstruction ParseTrigger(InstructionAddress? instrAddr, int index, AsmInstruction referencing)
		{
			AsmInstruction retInstruction;
			if(instrAddr == null)
			{
				if(triggers[index] == null)
				{
					throw new Exception("Trigger doesn't exist");
				}
				retInstruction = triggers[index];
			}
			else
			{
				retInstruction = ParseInstruction(instrAddr.Value, null, null);
				retInstruction.SubroutineType = SubroutineType.Trigger;
				subroutines[instrAddr.Value] = retInstruction;
				triggers[index] = retInstruction;
			}
			retInstruction.ReferencedFrom.Add(referencing);
			return retInstruction;
		}

		private void SetupInstruction(AsmInstruction instruction)
		{
			if(instruction.Done){return;}

			instruction.Setup(this);
			if(!instruction.Terminal)
			{
				var rawInstr = instruction.InstrAddr;

				//Skip Operands
				rawInstr += sizeof(int) * instruction.OperandCount;

				instruction.Next = ParseInstruction(rawInstr, instruction, null);
			}
			else
			{
				//Anything needed here?
				//instruction->Next;
			}

			instruction.Done = true;
		}

		public AsmInstruction ParseAndSetup(InstructionAddress rawInstr)
		{
			var retInstructions = ParseStrat(rawInstr, null, true);
			while(needsSetup.TryDequeue(out var cur))
			{
				SetupInstruction(cur);
			}
			return retInstructions;
		}

		public void Write(TextWriter output, bool exportForParsing)
		{
			bool lastInstrMissing = false;

			foreach(AsmInstruction instr in this.instructions.OrderBy(e => e.Key).Select(e => e.Value))
			{
				var instrStr = instr.ToAsmString(exportForParsing);

				//If the last instruction is empty, it means that this one was merged with it. 
				//In order for this to work, the current instruction should not have any calls/jump to it,
				//then we use the jumps and calls from the previous instructions.
				//In other words, we use the labels from the first instruction but the operation from the second.

				if(exportForParsing && lastInstrMissing)
				{
					if(instr.IsSubroutineEntry || instr.HasLabel)
					{
						throw new Exception("Invalid merged instruction");
					}
				}
				else
				{
					if(instr.IsSubroutineEntry)
					{
						if(!exportForParsing)
						{
							output.WriteLine();
						}
						output.Write(instr.SubroutineName());
						output.Write(':');
						if(!exportForParsing)
						{
							output.WriteLine();
						}
					}
					else if(exportForParsing)
					{
						output.Write(':');
					}
					if(instr.HasLabel)
					{
						output.Write(instr.GetLabel());
						output.Write(':');
						if(!exportForParsing)
						{
							output.WriteLine();
						}
					}
					else if(exportForParsing)
					{
						output.Write(':');
					}
					if(!exportForParsing)
					{
						output.Write('\t');
					}
				}
				if(!exportForParsing || instrStr.Length!=0)
				{
					output.WriteLine(instrStr);
				}

				lastInstrMissing = instrStr.Length == 0;
			}
		}

		public IReadOnlyList<(AsmInstruction label,AsmInstruction operation)> GetInstructions()
		{
			var ret = new List<(AsmInstruction label,AsmInstruction operation)>();

			bool lastInstrMissing = false;

			AsmInstruction? label = null;
			foreach(AsmInstruction instr in instructions.OrderBy(e => e.Key).Select(e => e.Value))
			{
				//If the last instruction is empty, it means that this one was merged with it. 
				//In order for this to work, the current instruction should not have any calls/jump to it,
				//then we use the jumps and calls from the previous instructions.
				//In other words, we use the labels from the first instruction but the operation from the second.

				if(lastInstrMissing)
				{
					if(instr.IsSubroutineEntry || instr.HasLabel)
					{
						throw new Exception("Invalid merged instruction");
					}
				}
				else
				{
					label = instr;
				}

				if(instr.Export)
				{
					ret.Add((label!, instr));
					lastInstrMissing = false;
				}
				else
				{
					lastInstrMissing = true;
				}
			}
			return ret;
		}

		private static AsmInstruction CreateInstruction(StratReader reader, AsmInstruction? prev)
		{
			var opcodeValue = reader.ReadInt();
			var opcode = reader.WadFile.Version.MapOpcode(opcodeValue);
			AsmInstruction newInstr = opcode switch
			{
				//0 - 99
				InstructionOpcode.CommandError => new CommandErrorInstruction(reader.Script, reader.Position, InstructionOpcode.CommandError),
				InstructionOpcode.Local => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Local),
				InstructionOpcode.Global => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Global),
				InstructionOpcode.WorldGlobal => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.WorldGlobal),
				InstructionOpcode.AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.AlienVar),
				InstructionOpcode.LocalAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.LocalAddress),
				InstructionOpcode.GlobalAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.GlobalAddress),
				InstructionOpcode.WorldGlobalAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.WorldGlobalAddress),
				InstructionOpcode.AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.AlienVarAddress),
				InstructionOpcode.Print => new PrintInstruction(reader.Script, reader.Position, InstructionOpcode.Print),
				InstructionOpcode.Number => new NumberInstruction(reader.Script, reader.Position, InstructionOpcode.Number),
				InstructionOpcode.UMinus => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.UMinus),
				InstructionOpcode.Increase => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Increase),
				InstructionOpcode.Decrease => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Decrease),
				InstructionOpcode.Add => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Add),
				InstructionOpcode.Sub => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Sub),
				InstructionOpcode.Mul => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Mul),
				InstructionOpcode.Div => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Div),
				InstructionOpcode.Equals => new BasicInstruction(reader.Script, 2, 0, reader.Position, InstructionOpcode.Equals),
				InstructionOpcode.Compare => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Compare),
				InstructionOpcode.LessThan => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.LessThan),
				InstructionOpcode.GreaterThan => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.GreaterThan),
				InstructionOpcode.SetModel => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SetModel),
				InstructionOpcode.Scale => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Scale),
				InstructionOpcode.ScaleX => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.ScaleX),
				InstructionOpcode.ScaleY => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.ScaleY),
				InstructionOpcode.ScaleZ => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.ScaleZ),
				InstructionOpcode.Shadow => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.Shadow),
				InstructionOpcode.ShadowSize => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.ShadowSize),
				InstructionOpcode.ShadowType => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.ShadowType),
				InstructionOpcode.Hide => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.Hide),
				InstructionOpcode.Flash => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Flash),
				InstructionOpcode.Trans => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Trans),
				InstructionOpcode.MoveUp => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveUp),
				InstructionOpcode.MoveDown => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveDown),
				InstructionOpcode.MoveForward => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveForward),
				InstructionOpcode.MoveBackward => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveBackward),
				InstructionOpcode.MoveLeft => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveLeft),
				InstructionOpcode.MoveRight => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveRight),
				InstructionOpcode.TurnRight => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TurnRight),
				InstructionOpcode.TurnLeft => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TurnLeft),
				InstructionOpcode.TiltLeft => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TiltLeft),
				InstructionOpcode.TiltRight => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TiltRight),
				InstructionOpcode.TiltForward => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TiltForward),
				InstructionOpcode.TiltBackward => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TiltBackward),
				InstructionOpcode.TurnToPlayerX => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TurnToPlayerX),
				InstructionOpcode.TurnToPlayerY => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TurnToPlayerY),
				InstructionOpcode.TurnToPlayerXY => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TurnToPlayerXY),
				InstructionOpcode.TurnToX => new BasicInstruction(reader.Script, 4, 0, reader.Position, InstructionOpcode.TurnToX),
				InstructionOpcode.TurnToY => new BasicInstruction(reader.Script, 4, 0, reader.Position, InstructionOpcode.TurnToY),
				InstructionOpcode.TurnToXY => new BasicInstruction(reader.Script, 4, 0, reader.Position, InstructionOpcode.TurnToXY),
				InstructionOpcode.Wobble => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Wobble),
				InstructionOpcode.ReSetPos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ReSetPos),
				InstructionOpcode.SetPos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.SetPos),
				InstructionOpcode.Jump => new JumpInstruction(reader.Script, reader.Position, InstructionOpcode.Jump),
				InstructionOpcode.ObjectFall => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ObjectFall),
				InstructionOpcode.Hang => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.Hang),
				InstructionOpcode.WPFirst => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.WPFirst),
				InstructionOpcode.WPLast => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.WPLast),
				InstructionOpcode.WPNext => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.WPNext),
				InstructionOpcode.WPPrev => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.WPPrev),
				InstructionOpcode.WPDel => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.WPDel),
				InstructionOpcode.WPNew => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.WPNew),
				InstructionOpcode.WPNearest => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.WPNearest),
				InstructionOpcode.WPFurthest => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.WPFurthest),
				InstructionOpcode.WPTurnToX => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.WPTurnToX),
				InstructionOpcode.WPTurnToY => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.WPTurnToY),
				InstructionOpcode.WPTurnToXY => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.WPTurnToXY),
				InstructionOpcode.AnimPlay => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.AnimPlay),
				InstructionOpcode.AnimStop => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.AnimStop),
				InstructionOpcode.AnimClear => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.AnimClear),
				InstructionOpcode.AnimSetSpeed => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.AnimSetSpeed),
				InstructionOpcode.CollisionType => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.CollisionType),
				InstructionOpcode.CollRadius => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.CollRadius),
				InstructionOpcode.CollHeight => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.CollHeight),
				InstructionOpcode.CollExtent => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.CollExtent),
				InstructionOpcode.CollView => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.CollView),
				InstructionOpcode.CollPoints => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.CollPoints),
				InstructionOpcode.CollSetPoint => new BasicInstruction(reader.Script, 4, 0, reader.Position, InstructionOpcode.CollSetPoint),
				InstructionOpcode.CreateTrigger => new TriggerCreateInstruction(reader.Script, reader.Position, InstructionOpcode.CreateTrigger),
				InstructionOpcode.KillTrigger => new TriggerUpdateInstruction(reader.Script, reader.Position, InstructionOpcode.KillTrigger),
				InstructionOpcode.HoldTriggers => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.HoldTriggers),
				InstructionOpcode.ReleaseTriggers => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ReleaseTriggers),
				InstructionOpcode.HoldTrigger => new TriggerUpdateInstruction(reader.Script, reader.Position, InstructionOpcode.HoldTrigger),
				InstructionOpcode.ReleaseTrigger => new TriggerUpdateInstruction(reader.Script, reader.Position, InstructionOpcode.ReleaseTrigger),
				InstructionOpcode.Wait => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Wait),
				InstructionOpcode.Hold => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.Hold),
				InstructionOpcode.Release => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Release),
				InstructionOpcode.Remove => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.Remove),
				InstructionOpcode.MapRemove => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.MapRemove),
				InstructionOpcode.MapAdd => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.MapAdd),
				InstructionOpcode.MapReplace => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.MapReplace),
				InstructionOpcode.Activated => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.Activated),
				InstructionOpcode.Collected => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.Collected),
				InstructionOpcode.Spawn => new SpawnInstruction(reader.Script, reader.Position, InstructionOpcode.Spawn),
				InstructionOpcode.SpawnFrom => new SpawnFromInstruction(reader.Script, reader.Position, InstructionOpcode.SpawnFrom),
				InstructionOpcode.Link => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Link),
				InstructionOpcode.Unlink => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Unlink),
				InstructionOpcode.SoundShift => new BasicInstruction(reader.Script, 2, 0, reader.Position, InstructionOpcode.SoundShift),
				InstructionOpcode.SoundStop => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SoundStop),
				//100 - 199
				InstructionOpcode.CdPlay => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.CdPlay),
				InstructionOpcode.MidiLoop => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.MidiLoop),
				InstructionOpcode.MidiVolume => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.MidiVolume),
				InstructionOpcode.CdFade => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.CdFade),
				InstructionOpcode.MidiStop => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.MidiStop),
				InstructionOpcode.MidiQueue => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.MidiQueue),
				InstructionOpcode.IsLight => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.IsLight),
				InstructionOpcode.LightCol => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.LightCol),
				InstructionOpcode.LightFade => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.LightFade),
				InstructionOpcode.LightAtten => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.LightAtten),
				InstructionOpcode.LightType => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.LightType),
				InstructionOpcode.CollisionOn => new CollisionFlagInstruction(reader.Script, reader.Position, InstructionOpcode.CollisionOn),
				InstructionOpcode.CollisionOff => new CollisionFlagInstruction(reader.Script, reader.Position, InstructionOpcode.CollisionOff),
				InstructionOpcode.CollisionOffAll => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CollisionOffAll),
				InstructionOpcode.SoundPlay3 => new BasicInstruction(reader.Script, 3, 0, reader.Position, InstructionOpcode.SoundPlay3),
				InstructionOpcode.SoundPlay4 => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.SoundPlay4),
				InstructionOpcode.SoundPlay3ASS => new BasicInstruction(reader.Script, 3, 1, reader.Position, InstructionOpcode.SoundPlay3ASS),
				InstructionOpcode.SoundPlay4ASS => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.SoundPlay4ASS),
				InstructionOpcode.Int => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.Int),
				InstructionOpcode.Sin => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.Sin),
				InstructionOpcode.Cos => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.Cos),
				InstructionOpcode.Not => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.Not),
				InstructionOpcode.Pop => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Pop),
				InstructionOpcode.StkCmp => new StackCompareInstruction(reader.Script, reader.Position, InstructionOpcode.StkCmp),
				InstructionOpcode.Address => new AddressInstruction(reader.Script, reader.Position, InstructionOpcode.Address),
				InstructionOpcode.Jsr => new JumpSubroutineInstruction(reader.Script, reader.Position, InstructionOpcode.Jsr),
				InstructionOpcode.JsrImm => new JumpSubroutineInstruction(reader.Script, reader.Position, InstructionOpcode.JsrImm),
				InstructionOpcode.Return => new ReturnInstruction(reader.Script, reader.Position, InstructionOpcode.Return),
				InstructionOpcode.Beq => new BranchInstruction(reader.Script, reader.Position, InstructionOpcode.Beq),
				InstructionOpcode.Bne => new BranchInstruction(reader.Script, reader.Position, InstructionOpcode.Bne),
				InstructionOpcode.BeqImm => new BranchInstruction(reader.Script, reader.Position, InstructionOpcode.BeqImm),
				InstructionOpcode.BneImm => new BranchInstruction(reader.Script, reader.Position, InstructionOpcode.BneImm),
				InstructionOpcode.JumpImm => new JumpInstruction(reader.Script, reader.Position, InstructionOpcode.JumpImm),
				InstructionOpcode.EndStrat => new EndStratInstruction(reader.Script, reader.Position, InstructionOpcode.EndStrat),
				InstructionOpcode.IsPlayer => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.IsPlayer),
				InstructionOpcode.And => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.And),
				InstructionOpcode.Or => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Or),
				InstructionOpcode.Index_Jump => new IndexJumpInstruction(reader.Script, reader.Position, InstructionOpcode.Index_Jump),
				InstructionOpcode.BitwiseAnd => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.BitwiseAnd),
				InstructionOpcode.Ext_Local => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Ext_Local),
				InstructionOpcode.Ext_LocalAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Ext_LocalAddress),
				InstructionOpcode.Ext_Global => new UnimplementedInstruction(reader.Script, 1, 0, 0, reader.Position, InstructionOpcode.Ext_Global),
				InstructionOpcode.Ext_GlobalAddress => new UnimplementedInstruction(reader.Script, 1, 0, 0, reader.Position, InstructionOpcode.Ext_GlobalAddress),
				InstructionOpcode.ObjectJump => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.ObjectJump),
				InstructionOpcode.Ext_AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Ext_AlienVar),
				InstructionOpcode.Ext_AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Ext_AlienVarAddress),
				InstructionOpcode.NotEqual => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.NotEqual),
				InstructionOpcode.ShiftLeft => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.ShiftLeft),
				InstructionOpcode.ShiftRight => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.ShiftRight),
				InstructionOpcode.AnimAdvance => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.AnimAdvance),
				InstructionOpcode.GreaterEqual => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.GreaterEqual),
				InstructionOpcode.LessEqual => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.LessEqual),
				InstructionOpcode.Rnd => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.Rnd),
				InstructionOpcode.Blink => new BlinkInstruction(reader.Script, reader.Position, InstructionOpcode.Blink),
				InstructionOpcode.LoseHeart => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.LoseHeart),
				InstructionOpcode.ResetToCheckPoint => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ResetToCheckPoint),
				InstructionOpcode.ForceCollision => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ForceCollision),
				InstructionOpcode.TurnFromPlayerY => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TurnFromPlayerY),
				InstructionOpcode.PlayerAttack => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.PlayerAttack),
				InstructionOpcode.Rumble => new BasicInstruction(reader.Script, 2, 0, reader.Position, InstructionOpcode.Rumble),
				InstructionOpcode.Vibrate => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Vibrate),
				InstructionOpcode.SuspendIfTooFar => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.SuspendIfTooFar),
				InstructionOpcode.CollisionBone => new BasicInstruction(reader.Script, 2, 0, reader.Position, InstructionOpcode.CollisionBone),
				InstructionOpcode.UseBone => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.UseBone),
				InstructionOpcode.IsCamera => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.IsCamera),
				InstructionOpcode.LookAtMe => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.LookAtMe),
				InstructionOpcode.LookAtMe2 => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.LookAtMe2),
				InstructionOpcode.PushCamera => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.PushCamera),
				InstructionOpcode.PopCamera => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.PopCamera),
				InstructionOpcode.ResetCameraPos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ResetCameraPos),
				InstructionOpcode.GainHeart => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GainHeart),
				InstructionOpcode.GainHeartPot => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GainHeartPot),
				InstructionOpcode.AddInv => new ItemChangeInstruction(reader.Script, reader.Position, InstructionOpcode.AddInv),
				InstructionOpcode.GainCrystal => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.GainCrystal),
				InstructionOpcode.Cutscene => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Cutscene),
				InstructionOpcode.Inventory => new ItemCountInstruction(reader.Script, reader.Position, InstructionOpcode.Inventory),
				InstructionOpcode.DebugName => new DebugNameInstruction(reader.Script, reader.Position, InstructionOpcode.DebugName),
				InstructionOpcode.PlayerDistanceCheck => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.PlayerDistanceCheck),
				InstructionOpcode.SoundPlay1 => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SoundPlay1),
				InstructionOpcode.SoundPlay1ASS => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.SoundPlay1ASS),
				InstructionOpcode.SoundAddress => new SoundAddressInstruction(reader.Script, reader.Position, InstructionOpcode.SoundAddress),
				InstructionOpcode.OnGround => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.OnGround),
				InstructionOpcode.ObjectFallSlow => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ObjectFallSlow),
				InstructionOpcode.Player_AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Player_AlienVar),
				InstructionOpcode.Player_AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Player_AlienVarAddress),
				InstructionOpcode.CollisionOffset => new BasicInstruction(reader.Script, 3, 0, reader.Position, InstructionOpcode.CollisionOffset),
				InstructionOpcode.Abs => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.Abs),
				InstructionOpcode.Pickup => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.Pickup),
				InstructionOpcode.Min => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Min),
				InstructionOpcode.Max => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Max),
				InstructionOpcode.SpawnParticle => new BasicInstruction(reader.Script, 6, 0, reader.Position, InstructionOpcode.SpawnParticle),
				InstructionOpcode.Sgn => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.Sgn),
				InstructionOpcode.SpawnAfter => new SpawnAfterInstruction(reader.Script, reader.Position, InstructionOpcode.SpawnAfter),
				InstructionOpcode.Camera_AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Camera_AlienVar),
				InstructionOpcode.Camera_AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Camera_AlienVarAddress),
				InstructionOpcode.Target_AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Target_AlienVar),
				InstructionOpcode.Target_AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Target_AlienVarAddress),
				InstructionOpcode.Collide_AlienVar => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Collide_AlienVar),
				InstructionOpcode.Collide_AlienVarAddress => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Collide_AlienVarAddress),
				InstructionOpcode.Target2_AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Target2_AlienVar),
				//200 - 299
				InstructionOpcode.Target2_AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Target2_AlienVarAddress),
				InstructionOpcode.DontLookAtMe => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.DontLookAtMe),
				InstructionOpcode.RunAt60 => new UsedUnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.RunAt60),
				InstructionOpcode.MoveForwardq => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveForwardq),
				InstructionOpcode.MoveBackwardq => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveBackwardq),
				InstructionOpcode.ScreenPrint => new PrintInstruction(reader.Script, reader.Position, InstructionOpcode.ScreenPrint),
				InstructionOpcode.SoundPlay2 => new BasicInstruction(reader.Script, 2, 0, reader.Position, InstructionOpcode.SoundPlay2),
				InstructionOpcode.SoundPlay2ASS => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.SoundPlay2ASS),
				InstructionOpcode.SetWP => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.SetWP),
				InstructionOpcode.ResetWP => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ResetWP),
				InstructionOpcode.SoundVolume => new BasicInstruction(reader.Script, 2, 0, reader.Position, InstructionOpcode.SoundVolume),
				InstructionOpcode.Push => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.Push),
				InstructionOpcode.String => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.String),
				InstructionOpcode.SetBossHearts => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SetBossHearts),
				InstructionOpcode.LoseBossHeart => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.LoseBossHeart),
				InstructionOpcode.SoundShiftRelative => new BasicInstruction(reader.Script, 2, 0, reader.Position, InstructionOpcode.SoundShiftRelative),
				InstructionOpcode.Smin => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.Smin),
				InstructionOpcode.IsBoss => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.IsBoss),
				InstructionOpcode.TopSay => new DialogSayInstruction(reader.Script, reader.Position, InstructionOpcode.TopSay),
				InstructionOpcode.Boss_AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Boss_AlienVar),
				InstructionOpcode.Boss_AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Boss_AlienVarAddress),
				InstructionOpcode.GetParentPos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GetParentPos),
				InstructionOpcode.AfterBoss => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.AfterBoss),
				InstructionOpcode.AfterPlayer => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.AfterPlayer),
				InstructionOpcode.BeforePlayer => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.BeforePlayer),
				InstructionOpcode.BeforeBoss => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.BeforeBoss),
				InstructionOpcode.NoHang => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.NoHang),
				InstructionOpcode.Zero => new BasicInstruction(reader.Script, 0, 1, reader.Position, InstructionOpcode.Zero),
				InstructionOpcode.TopHead => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.TopHead),
				InstructionOpcode.TopDialog => new DialogSetInstruction(reader.Script, reader.Position, InstructionOpcode.TopDialog),
				InstructionOpcode.BottomSay => new DialogSayInstruction(reader.Script, reader.Position, InstructionOpcode.BottomSay),
				InstructionOpcode.BottomHead => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.BottomHead),
				InstructionOpcode.BottomDialog => new DialogSetInstruction(reader.Script, reader.Position, InstructionOpcode.BottomDialog),
				InstructionOpcode.GetPlayerPos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GetPlayerPos),
				InstructionOpcode.GetWPpos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GetWPpos),
				InstructionOpcode.GetBossPos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GetBossPos),
				InstructionOpcode.GetDoorPos => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GetDoorPos),
				InstructionOpcode.FadeOut => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.FadeOut),
				InstructionOpcode.FadeIn => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.FadeIn),
				InstructionOpcode.MoveUpq => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveUpq),
				InstructionOpcode.MoveDownq => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveDownq),
				InstructionOpcode.ForcePlayerDist => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ForcePlayerDist),
				InstructionOpcode.ShadeType => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.ShadeType),
				InstructionOpcode.NOP => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.NOP),
				InstructionOpcode.SetAnimSpeed => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SetAnimSpeed),
				InstructionOpcode.CheckLevelDoor => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CheckLevelDoor),
				InstructionOpcode.BottomHeadLeft => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.BottomHeadLeft),
				InstructionOpcode.TopHeadLeft => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.TopHeadLeft),
				InstructionOpcode.GainJigsaw => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GainJigsaw),
				InstructionOpcode.GainGoldenGobbo => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GainGoldenGobbo),
				InstructionOpcode.Gain100Crystal => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.Gain100Crystal),
				InstructionOpcode.ResetSpline => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ResetSpline),
				InstructionOpcode.CheckPoint => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CheckPoint),
				InstructionOpcode.WaterTest => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.WaterTest),
				InstructionOpcode.IsMainCamera => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.IsMainCamera),
				InstructionOpcode.ResetDialog => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ResetDialog),
				InstructionOpcode.EndLevel => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.EndLevel),
				InstructionOpcode.Dialog_AlienVar => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Dialog_AlienVar),
				InstructionOpcode.Dialog_AlienVarAddress => new VarInstruction(reader.Script, reader.Position, InstructionOpcode.Dialog_AlienVarAddress),
				InstructionOpcode.IsDialog => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.IsDialog),
				InstructionOpcode.Distance => new BasicInstruction(reader.Script, 3, 1, reader.Position, InstructionOpcode.Distance),
				InstructionOpcode.Binocs => new BinocsInstruction(reader.Script, reader.Position, InstructionOpcode.Binocs),
				InstructionOpcode.TopCloseDialog => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.TopCloseDialog),
				InstructionOpcode.BottomCloseDialog => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.BottomCloseDialog),
				InstructionOpcode.NextInventory => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.NextInventory),
				InstructionOpcode.PrevInventory => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.PrevInventory),
				InstructionOpcode.OtherPiece => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.OtherPiece),
				InstructionOpcode.NormalPiece => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.NormalPiece),
				InstructionOpcode.Climb => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.Climb),
				InstructionOpcode.DelInv => new ItemChangeInstruction(reader.Script, reader.Position, InstructionOpcode.DelInv),
				InstructionOpcode.GainReward => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GainReward),
				InstructionOpcode.WorldVector => new BasicInstruction(reader.Script, 3, 0, reader.Position, InstructionOpcode.WorldVector),
				InstructionOpcode.ObjectFallVerySlow => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ObjectFallVerySlow),
				InstructionOpcode.Slope2Controller => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.Slope2Controller),
				InstructionOpcode.LevelComplete => new BasicInstruction(reader.Script, 3, 1, reader.Position, InstructionOpcode.LevelComplete),
				InstructionOpcode.SetLevelFlag => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SetLevelFlag),
				InstructionOpcode.GetLevelFlag => new BasicInstruction(reader.Script, 3, 1, reader.Position, InstructionOpcode.GetLevelFlag),
				InstructionOpcode.CalcCarTilt => new BasicInstruction(reader.Script, 4, 0, reader.Position, InstructionOpcode.CalcCarTilt),
				InstructionOpcode.MoveLeftq => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveLeftq),
				InstructionOpcode.MoveRightq => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.MoveRightq),
				InstructionOpcode.BitwiseNot => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.BitwiseNot),
				InstructionOpcode.BordersOn => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.BordersOn),
				InstructionOpcode.BordersOff => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.BordersOff),
				InstructionOpcode.SoundAdsr => new BasicInstruction(reader.Script, 5, 0, reader.Position, InstructionOpcode.SoundAdsr),
				InstructionOpcode.SoundAdsrRelative => new BasicInstruction(reader.Script, 5, 0, reader.Position, InstructionOpcode.SoundAdsrRelative),
				InstructionOpcode.RotatePiece => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.RotatePiece),
				InstructionOpcode.SetAmbient => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.SetAmbient),
				InstructionOpcode.ResetAmbient => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.ResetAmbient),
				InstructionOpcode.InvActive => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.InvActive),
				InstructionOpcode.InvInactive => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.InvInactive),
				InstructionOpcode.SampleStatus => new BasicInstruction(reader.Script, 1, 1, reader.Position, InstructionOpcode.SampleStatus),
				InstructionOpcode.ResetToCheckPointnlh => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ResetToCheckPointnlh),
				InstructionOpcode.ResetDoor => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ResetDoor),
				InstructionOpcode.StoreDoor => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.StoreDoor),
				InstructionOpcode.Camera_modified => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.Camera_modified),
				InstructionOpcode.PushPlayer => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.PushPlayer),
				InstructionOpcode.PopPlayer => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.PopPlayer),
				InstructionOpcode.ReSetPostrn => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ReSetPostrn),
				InstructionOpcode.GainItem => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.GainItem),
				InstructionOpcode.SetItem => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SetItem),
				//300 - 322
				InstructionOpcode.SetTimer => new BasicInstruction(reader.Script, 1, 0, reader.Position, InstructionOpcode.SetTimer),
				InstructionOpcode.TimerOff => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.TimerOff),
				InstructionOpcode.DistanceNoY => new BasicInstruction(reader.Script, 2, 1, reader.Position, InstructionOpcode.DistanceNoY),
				InstructionOpcode.Swim => new FlagInstruction(reader.Script, reader.Position, InstructionOpcode.Swim),
				InstructionOpcode.Lose100Crystals => new UnimplementedInstruction(reader.Script, 0, 0, 0, reader.Position, InstructionOpcode.Lose100Crystals),
				InstructionOpcode.LoseReward => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.LoseReward),
				InstructionOpcode.LoseGoldenGobbo => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.LoseGoldenGobbo),
				InstructionOpcode.NextTribe => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.NextTribe),
				InstructionOpcode.PrevTribe => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.PrevTribe),
				InstructionOpcode.SetTimerClock => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.SetTimerClock),
				InstructionOpcode.SetTimerBomb => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.SetTimerBomb),
				InstructionOpcode.InitBurpingGame => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.InitBurpingGame),
				InstructionOpcode.CloseBurpingGame => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CloseBurpingGame),
				InstructionOpcode.Credit => new CreditInstruction(reader.Script, reader.Position, InstructionOpcode.Credit),
				InstructionOpcode.CloseCredits => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CloseCredits),
				InstructionOpcode.ShowRewardCard => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ShowRewardCard),
				InstructionOpcode.ShowHearts => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.ShowHearts),
				InstructionOpcode.Cwg => new CwgInstruction(reader.Script, reader.Position, InstructionOpcode.Cwg),
				InstructionOpcode.FadeFunction_47E960 => new FadeSetUnknownInstruction(reader.Script, reader.Position, InstructionOpcode.FadeFunction_47E960),
				InstructionOpcode.CameraFunction_47F1E0 => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CameraFunction_47F1E0),
				InstructionOpcode.CameraFunction_47F040 => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CameraFunction_47F040),
				InstructionOpcode.CameraFunction_47F0C0 => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CameraFunction_47F0C0),
				InstructionOpcode.CameraFunction_47F490 => new BasicInstruction(reader.Script, 0, 0, reader.Position, InstructionOpcode.CameraFunction_47F490),
				_ => throw new Exception("Unknown OpCode")
			};

			newInstr.Prev = prev;

			//Reset the reader to the start of the operands before reading
			reader.Position = newInstr.InstrAddr;
			newInstr.Parse(reader);
			return newInstr;
		}
	}
}