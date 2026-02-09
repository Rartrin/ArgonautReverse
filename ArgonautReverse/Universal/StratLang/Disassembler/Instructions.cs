using System.Text;
using ArgonautReverse.PC;

namespace ArgonautReverse.Universal.StratLang.Disassembler
{
	public abstract class BaseInstruction(Script script, int opCount,int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):AsmInstruction(script, address, opcode, opCount, popCount, pushCount);

	/// <summary>Non-terminal instruction that without any extra operands that can pop and push any number of args.</summary>
	public class BasicInstruction(Script script, int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 0, popCount, pushCount, address, opcode)
	{
		public override void WriteAsmString(Decompiler.Writer output) => output.Write(OpCode.ToString());
	}

	public sealed class UnimplementedInstruction:BaseInstruction
	{
		public UnimplementedInstruction(Script script, int opCount,int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):base(script, opCount, popCount, pushCount, address, opcode)
		{
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.WriteLine(" $ UNIMPLEMENTED");
		}
	}

	//Unimplemented but also used in game
	public sealed class UsedUnimplementedInstruction:BaseInstruction
	{
		public UsedUnimplementedInstruction(Script script, int opCount,int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):base(script, opCount, popCount, pushCount, address, opcode)
		{
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.WriteLine(" $ USED UNIMPLEMENTED");
		}
	}

	public sealed class AddressInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 1, address, opcode)
	{
		//It appears that stAddress is used in:
		// - Loading animation data
		// - Animation Waiting
		// - Object/Model loading
		// - Load Strat Proc Address for Spawning
		// - Loading some strings
	
		//Might also be using in midiloading?

		public bool IsDataLoad;
		public int DataOffset;

		public bool IsAnimLoad;
		public int AnimationIndex;

		//Pulled into another instruction
		public bool Consumed = false;

		public override void Parse(StratReader reader)
		{
			int arg = reader.ReadInt();

			if(arg >= 0)
			{
				IsDataLoad = true;
				DataOffset = arg;
			
				IsAnimLoad = false;
			}
			else
			{
				IsDataLoad = false;
			
				IsAnimLoad = true;
				AnimationIndex = ~arg;

				var animationCount = reader.WadFile switch
				{
					PSX.WadFilePSX psx => psx.DPSX.Animations.Count,
					PC.WadFilePC pc => pc.StratChunk.Animations.Count,
					_ => throw new NotImplementedException(),
				};

				if(arg >= animationCount)
				{
					throw new Exception("Animation index out of range");
				}
			}
		}

		//public InstructionAddress GetDataPtr(StratReader reader)
		//{
		//	if(!IsDataLoad)
		//	{
		//		throw new Exception("Wrong address type");
		//	}
		//	//On PC, this is a STPC chunk offset.
		//	//On PSX, this is a DATA chunk offset
		//	//reader.WadFile.DPSX.Data
		//	return &Wad.currentWadPtr.chunkData.data.fileData[DataOffset];
		//}

		public (Script script, InstructionAddress address) GetStratProcAddr(StratReader reader)
		{
			if(!IsDataLoad)
			{
				throw new Exception("Wrong address type");
			}
			return reader.WadFile.GetStratProcAddr(DataOffset);
		}

		public override bool Export => !Consumed;

		public override void WriteAsmString(Decompiler.Writer output)
		{
			if(/*exportForParsing && */Consumed)
			{
				return;
			}
			//if(Consumed)
			//{
			//	output.Write("$ CONSUMED: ");
			//}
			if(IsDataLoad)
			{
				output.Write(OpCode.ToString());
				output.Write(" DataOffset ");
				output.WriteInt(DataOffset);
			}
			else if(IsAnimLoad)
			{
				output.Write(OpCode.ToString());
				output.Write(" animload Anim_");
				output.WriteInt(AnimationIndex);//animload
			}
			else
			{
				throw new Exception("Unknown stAddress type");
			}
		}
	}

	public sealed class BinocsInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public int State;

		public override void Parse(StratReader reader)
		{
			State = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(State);
		}
	}

	public sealed class BlinkInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 1, 0, address, opcode)
	{
		public int Count;

		public override void Parse(StratReader reader)
		{
			Count = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Count);
		}
	}

	public sealed class BranchInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 1, 0, address, opcode)
	{
		public InstructionAddress ConditionalDestPtr;
		public AsmInstruction ConditionalDest;

		public override void Parse(StratReader reader)
		{
			this.ConditionalDestPtr = reader.ReadRelativeAddress();
		}

		public override void Setup(StratParser parser)
		{
			this.ConditionalDest = parser.ParseInstruction(this.ConditionalDestPtr, null, this);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.Write(ConditionalDest.GetLabel());
		}
	}

	public sealed class CollisionFlagInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		//TODO: Check enum values
		public uint CollisionType;

		public override void Parse(StratReader reader)
		{
			CollisionType = reader.ReadUInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt((int)CollisionType);
		}
	}

	public sealed class CommandErrorInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 0, 0, 0, address, opcode)
	{
		public override void Parse(StratReader reader)
		{
			Terminal = true;
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write("COMMAND ERROR");
		}
	}

	public sealed class CreditInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public int Operand;

		public override void Parse(StratReader reader)
		{
			Operand = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Operand);
		}
	}

	public sealed class CwgInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Value);
		}
	}

	public sealed class DialogSayInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public int StringId;
		public string EnglishString;

		public override void Parse(StratReader reader)
		{
			StringId = reader.ReadInt();

			//TODO: LanguageString
			//EnglishString = Wad.currentWadPtr.LanguageStrings[StringId][0];
			EnglishString = $"STRING_ID:{StringId}";
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');//output.Write(" \"");
			output.Write(EnglishString);
			//output.Write('"');
		}
	}

	public sealed class DialogSetInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public int Operand;

		public override void Parse(StratReader reader)
		{
			Operand = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Operand);
		}
	}


	public sealed class DebugNameInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 2, 0, 0, address, opcode)
	{
		public string Name;

		public override void Parse(StratReader reader)
		{
			//DebugName has an ST_STRING token following it. We don't need it.
			//We need its argument though which is the string offset.
			
			_ = reader.ReadInt();//ST_STRING token
			var nameOffset = reader.ReadInt();
			var nameAddr = reader.Position + nameOffset;
			Name = reader.ReadString(nameAddr);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(" \"");
			output.Write(Name);
			output.Write('"');
		}
	}

	public sealed class EndStratInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 0, 0, 0, address, opcode)
	{
		//Deletes strat

		public override void Parse(StratReader reader)
		{
			Terminal = true;
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write("EndStrat");
		}
	}

	public sealed class FlagInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		//Operand
		public bool NewState;

		public override void Parse(StratReader reader)
		{
			var state = reader.ReadUInt();
			NewState = state switch
			{
				 0 => false,
				 1 => true,
				 _ => throw new Exception("Unsupported state value")
			};
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(NewState ? " on" : " off");
		}
	}

	public sealed class FadeSetUnknownInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Value);
		}
	}

	public sealed class IndexJumpInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 1, 0, address, opcode)
	{
		public int CaseCount;
		public int[] CaseComparands;
		public InstructionAddress[] CaseDestinationPtrs;
		public AsmInstruction[] CaseDestinations;

		//Special: Switch
		//Has extra data

		public override void Parse(StratReader reader)
		{
			var jumpTable = reader.ReadRelativeAddress();
			CaseCount = reader.ReadAt(jumpTable, 1);
			CaseComparands = new int[CaseCount];
			CaseDestinationPtrs = new InstructionAddress[CaseCount];

			for(int i=0; i<CaseCount; i++)
			{
				//Entries in jump tables have the following format (stNumber, Comparand, DestinationOffset)
				this.CaseComparands[i] = reader.ReadAt(jumpTable, 2+(3*i) + 1);
				CaseDestinationPtrs[i] = reader.Position + sizeof(int)*reader.ReadAt(jumpTable, 2+(3*i) + 2);
			}
		}

		public override void Setup(StratParser parser)
		{
			CaseDestinations = new AsmInstruction[CaseCount];

			for(int i=0; i<CaseCount; i++)
			{
				this.CaseDestinations[i] = parser.ParseInstruction(CaseDestinationPtrs[i], null, this);
			}
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			//TODO: Incorrect indenting on cases?
			output.OpenLine("switch");
			for(int i=0; i<CaseCount; i++)
			{
				output.Write("case ");
				output.WriteInt(CaseComparands[i]);
				output.OpenLine();
				output.Write("goto ");
				output.Write(CaseDestinations[i].GetLabel());
				output.CloseLine("endcase");
			}
			output.CloseLine("endswitch");
		}
	}

	public sealed class ItemCountInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 1, address, opcode)
	{
		//TODO: Check enum values
		public int Item;

		public override void Parse(StratReader reader)
		{
			Item = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write('(');
			output.WriteInt(Item);
			output.Write(')');
		}
	}

	public sealed class ItemChangeInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		//Give/take item from inventory

		//TODO: Check enum values
		public int Item;

		public override void Parse(StratReader reader)
		{
			Item = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Item);
		}
	}

	public sealed class JumpInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public InstructionAddress DestinationPtr;
		public AsmInstruction Destination;

		public override void Parse(StratReader reader)
		{
			Terminal = true;
			this.DestinationPtr = reader.ReadRelativeAddress();
		}

		public override void Setup(StratParser parser)
		{
			this.Destination = parser.ParseInstruction(this.DestinationPtr, null, this);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.Write(Destination.GetLabel());
		}
	}

	public sealed class JumpSubroutineInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		//Technically pushes a value but we aren't counting it because it's popped outside the subroutine.
		//Special: Function call

		public InstructionAddress ProcPtr;
		public AsmInstruction Proc;

		public override void Parse(StratReader reader)
		{
			this.ProcPtr = reader.ReadRelativeAddress();
		}

		public override void Setup(StratParser parser)
		{
			this.Proc = parser.ParseProc(this.ProcPtr, this);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.Write(Proc.SubroutineName());
		}
	}

	public sealed class NumberInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 1, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Value);
		}
	}

	public sealed class PrintInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, -1, 0, 0, address, opcode)
	{
		public IReadOnlyList<(InstructionOpcode type, object value,bool negate)> Elements;

		public override void Parse(StratReader reader)
		{
			var elementCount = reader.ReadInt();
			var elements = new (InstructionOpcode type,object value,bool negate)[elementCount];

			for(int i=0; i<elementCount; i++)
			{
				var type = reader.WadFile.Version.MapOpcode(reader.ReadInt());
				object value;
				switch(type)
				{
					case InstructionOpcode.Local:
					{
						value = reader.ReadInt();
						break;
					}
					case InstructionOpcode.Global:
					{
						var globalVarOffset = reader.ReadRelativeAddress();
						value = reader.ReadAt(globalVarOffset, 0);
						break;
					}
					case InstructionOpcode.WorldGlobal:
					{
						value = reader.ReadInt();
						break;
					}
					case InstructionOpcode.AlienVar:
					{
						value = (AlienVarID)reader.ReadInt();
						break;
					}
					case InstructionOpcode.Number:
					{
						value = Fixed32.FromRaw(reader.ReadInt());
						break;
					}
					case InstructionOpcode.Ext_AlienVar:
					{
						value = (AlienVarID)reader.ReadInt();
						break;
					}
					case InstructionOpcode.Player_AlienVar:
					{
						value = (AlienVarID)reader.ReadInt();
						break;
					}
					case InstructionOpcode.Camera_AlienVar:
					{
						value = (AlienVarID)reader.ReadInt();
						break;
					}
					case InstructionOpcode.String:
					{
						var strOffset = reader.ReadRelativeAddress();
						value = reader.ReadString(strOffset);
						break;
					}
					default:
					{
						throw new Exception("Invalid print type");
					}
				}
				bool negate = false;
				//TODO: Mapping for different platforms
				if(reader.PeekInt() == 257+(int)InstructionOpcode.UMinus)
				{
					OperandCount++;
					reader.ReadInt();
					negate = true;
				}
				elements[i] = (type, value, negate);
			}

			Elements = elements;
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			foreach((var type,var value,var negate) in Elements)
			{
				output.Write(' ');
				if(negate)
				{
					output.Write('-');
				}
				switch(type)
				{
					case InstructionOpcode.Local:
					{
						int localValue = (int)value;
						output.Write("Local_");
						output.WriteInt(localValue);
						break;
					}
					case InstructionOpcode.Global:
					{
						int globalValue = (int)value;
						output.Write("Global_");
						output.WriteInt(globalValue);//"%05x.%03x"
						break;
					}
					case InstructionOpcode.WorldGlobal:
					{
						int worldGlobalValue = (int)value;
						output.Write("WorldGlobal_");
						output.WriteInt(worldGlobalValue);
						break;
					}
					case InstructionOpcode.AlienVar:
					{
						output.Write("this@");
						output.Write(((AlienVarID)value).GetCommandString());
						break;
					}
					case InstructionOpcode.Number:
					{
						var number = (Fixed32)value;
						output.Write(number.ToHexString());
						break;
					}
					case InstructionOpcode.Ext_AlienVar:
					{
						output.Write("parent@");
						output.Write(((AlienVarID)value).GetCommandString());
						break;
					}
					case InstructionOpcode.Player_AlienVar:
					{
						output.Write("player@");
						output.Write(((AlienVarID)value).GetCommandString());
						break;
					}
					case InstructionOpcode.Camera_AlienVar:
					{
						output.Write("camera@");
						output.Write(((AlienVarID)value).GetCommandString());
						break;
					}
					case InstructionOpcode.String:
					{
						var str = (string)value;
						//TODO: Sanitize strings
						str = str.Replace("\n", "\\n");
						output.Write('\"');
						output.Write(str);
						output.Write('\"');
						break;
					}
					default:
					{
						throw new Exception("Invalid print type");
					}
				}
			}
		}
	}

	public sealed class ReturnInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 0, 0, 0, address, opcode)
	{
		//Technically pop a values but we aren't counting it because it is pushed outisde the subroutine.
	
		//Special: Function Return

		public override void Parse(StratReader reader)
		{
			Terminal = true;
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write("return");
		}
	}

	public sealed class SoundAddressInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 1, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Value);
		}
	}

	public abstract class BaseSpawnInstruction(Script script, int opCount, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, opCount, 1, 0, address, opcode)
	{
		public int LocalVarsToPop;
		public int LocalCount;
		public int TriggerCount;
		public int CollisionSize;
		public int CollisionBoneCount;
		
		public Script SpawnStratProcScript;
		public InstructionAddress SpawnStratProcAddr;
		public AsmInstruction? SpawnStratProc;
	}

	public class SpawnInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseSpawnInstruction(script, 5, address, opcode)
	{
		public override void Parse(StratReader reader)
		{
			LocalVarsToPop = reader.ReadInt();
			LocalCount = reader.ReadInt();
			TriggerCount = reader.ReadInt();
			CollisionSize = reader.ReadInt();
			CollisionBoneCount = reader.ReadInt();
			
			var address = (AddressInstruction)Prev!;
			address.Consumed = true;
			(SpawnStratProcScript, SpawnStratProcAddr) = address.GetStratProcAddr(reader);
		}

		public override void Setup(StratParser parser)
		{
			if(this.HasLabel || Prev!.OpCode != InstructionOpcode.Address)
			{
				throw new Exception("Unsupported spawn instruction");
			}

			//TODO: We can't parse the strat because it exists in a different script.
			//SpawnStratProcAddr is entry point address within SpawnStratProcScript, not neccessarily in THIS script.
			SpawnStratProc = SpawnStratProcScript.Parser.ParseStrat(SpawnStratProcAddr, this, false);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			if(SpawnStratProc != null)
			{
				output.Write(SpawnStratProc.SubroutineName());
			}
			else
			{
				output.Write(((AddressInstruction)Prev!).DataOffset.ToString("X8"));
			}
			output.Write(' ');
			output.WriteInt(LocalVarsToPop);
			output.Write(' ');
			output.WriteInt(LocalCount);
			output.Write(' ');
			output.WriteInt(TriggerCount);
			output.Write(' ');
			output.WriteInt(CollisionSize);
			output.Write(' ');
			output.WriteInt(CollisionBoneCount);
		}
	}

	public sealed class SpawnAfterInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):SpawnInstruction(script, address,opcode);

	public sealed class SpawnFromInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseSpawnInstruction(script, 6, address, opcode)
	{
		public int BoneToSpawnFrom;

		public override void Parse(StratReader reader)
		{
			LocalVarsToPop = reader.ReadInt();
			LocalCount = reader.ReadInt();
			TriggerCount = reader.ReadInt();
			CollisionSize = reader.ReadInt();
			CollisionBoneCount = reader.ReadInt();

			BoneToSpawnFrom = reader.ReadInt();

			var address = (AddressInstruction)Prev!;
			address.Consumed = true;
			(SpawnStratProcScript, SpawnStratProcAddr) = address.GetStratProcAddr(reader);
		}

		public override void Setup(StratParser parser)
		{
			if(this.HasLabel || Prev!.OpCode != InstructionOpcode.Address)
			{
				throw new Exception("Unsupported spawn instruction");
			}

			//TODO: We can't parse the strat because it exists in a different script.
			//SpawnStratProcAddr is entry point address within SpawnStratProcScript, not neccessarily in THIS script.
			SpawnStratProc = SpawnStratProcScript.Parser.ParseStrat(SpawnStratProcAddr, this, false);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			if(SpawnStratProc != null)
			{
				output.Write(SpawnStratProc.SubroutineName());
			}
			else
			{
				output.Write(((AddressInstruction)Prev!).DataOffset.ToString("X8"));
			}
			output.Write(' ');
			output.WriteInt(LocalVarsToPop);
			output.Write(' ');
			output.WriteInt(LocalCount);
			output.Write(' ');
			output.WriteInt(TriggerCount);
			output.Write(' ');
			output.WriteInt(CollisionSize);
			output.Write(' ');
			output.WriteInt(CollisionBoneCount);

			output.Write(' ');
			output.WriteInt(BoneToSpawnFrom);
		}
	}

	public sealed class StackCompareInstruction:BaseInstruction
	{
		//This is never used in Croc 2. It isn't in the Aladdin's ASL compiler either.

		//This techniclaly could be considered pulling one value and pushing two.

		//Special: Uses Peek on stack
		public int Comparand;

		public StackCompareInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):base(script, 1, 0, 1, address, opcode)
		{
			//Peeking values or pushing multiple values would make stack analysis and reconstruction very difficult.
			//Since it isn't even used in any code I've seen, we are not going to support it.
			throw new Exception();
		}

		public override void Parse(StratReader reader)
		{
			Comparand = reader.ReadInt();
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.WriteInt(Comparand);
		}
	}

	public sealed class TriggerCreateInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 2, 0, 0, address, opcode)
	{
		//Special: Extra data

		public int TriggerIndex;

		public TriggerType Type;
		public int Arg;
		public InstructionAddress StreamPtr;
		public AsmInstruction Stream;

		public override void Parse(StratReader reader)
		{
			var dataOffset = reader.ReadRelativeAddress();
			//TODO: Account for other Trigger Types
			Type = reader.WadFile.Version.MapTriggerType(reader.ReadAt(dataOffset, 0));
			Arg = reader.ReadAt(dataOffset, 1);

			var streamOffset = reader.ReadAt(dataOffset, 2) * sizeof(int);
			StreamPtr = reader.Position + streamOffset;

			TriggerIndex = reader.ReadInt();
		}

		public override void Setup(StratParser parser)
		{
			Stream = parser.ParseTrigger(StreamPtr, TriggerIndex, this);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.Write(Type.ToString());
			output.Write(' ');
			output.WriteInt(Arg);
			output.Write(' ');
			output.Write(Stream.SubroutineName());
		}
	}

	public sealed class TriggerUpdateInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 0, address, opcode)
	{
		public int TriggerIndex;
		public AsmInstruction TriggerProc;

		public override void Parse(StratReader reader)
		{
			TriggerIndex = reader.ReadInt();
		}

		public override void Setup(StratParser parser)
		{
			TriggerProc = parser.ParseTrigger(null, TriggerIndex, this);
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			//TODO: TriggerIndex should be replaced with trigger function name
			output.Write(OpCode.ToString());
			output.Write(' ');
			output.Write(TriggerProc.SubroutineName());
		}
	}

	public sealed class VarInstruction(Script script, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(script, 1, 0, 1, address, opcode)
	{
		public enum SourceStrat
		{
			This,Parent,Child,
			Player,Camera,Boss,Dialog,
			Target,Target2,Collide
		}
		public enum VarType
		{
			Local,
			Global,
			WorldGlobal,
			Alien
		}
		public SourceStrat Source;
		public VarType Type;


		//The address ones seem to be used exclusively for setting a particular varible, no pointer math.
		//Strat Lang didn't have pointers
		public bool GetAddress;//false to get value of var, true to get address of var

		//TODO: Check enum values for Alien vars
		public int VarId;

		public override void Parse(StratReader reader)
		{
			VarId = reader.ReadInt();
			(Source, Type, GetAddress) = OpCode switch
			{
				InstructionOpcode.Local => (SourceStrat.This, VarType.Local, false),
				InstructionOpcode.Global => (SourceStrat.This, VarType.Global, false),
				InstructionOpcode.WorldGlobal => (SourceStrat.This, VarType.WorldGlobal, false),
				InstructionOpcode.AlienVar => (SourceStrat.This, VarType.Alien, false),
				InstructionOpcode.LocalAddress => (SourceStrat.This, VarType.Local, true),
				InstructionOpcode.GlobalAddress => (SourceStrat.This, VarType.Global, true),
				InstructionOpcode.WorldGlobalAddress => (SourceStrat.This, VarType.WorldGlobal, true),
				InstructionOpcode.AlienVarAddress => (SourceStrat.This, VarType.Alien, true),
				InstructionOpcode.Ext_Local => (SourceStrat.Parent, VarType.Local, false),
				InstructionOpcode.Ext_LocalAddress => (SourceStrat.Parent, VarType.Local, true),
				//InstructionOpcode.Ext_Global
				//InstructionOpcode.Ext_GlobalAddress
				InstructionOpcode.Ext_AlienVar => (SourceStrat.Parent, VarType.Alien, false),
				InstructionOpcode.Ext_AlienVarAddress => (SourceStrat.Parent, VarType.Alien, true),
				InstructionOpcode.Player_AlienVar => (SourceStrat.Player, VarType.Alien, false),
				InstructionOpcode.Player_AlienVarAddress => (SourceStrat.Player, VarType.Alien, true),
				InstructionOpcode.Camera_AlienVar => (SourceStrat.Camera, VarType.Alien, false),
				InstructionOpcode.Camera_AlienVarAddress => (SourceStrat.Camera, VarType.Alien, true),
				InstructionOpcode.Target_AlienVar => (SourceStrat.Target, VarType.Alien, false),
				InstructionOpcode.Target_AlienVarAddress => (SourceStrat.Target, VarType.Alien, true),
				//InstructionOpcode.Collide_AlienVar
				//InstructionOpcode.Collide_AlienVarAddress
				InstructionOpcode.Target2_AlienVar => (SourceStrat.Target2, VarType.Alien, false),
				InstructionOpcode.Target2_AlienVarAddress => (SourceStrat.Target2, VarType.Alien, true),
				InstructionOpcode.Boss_AlienVar => (SourceStrat.Boss, VarType.Alien, false),
				InstructionOpcode.Boss_AlienVarAddress => (SourceStrat.Boss, VarType.Alien, true),
				InstructionOpcode.Dialog_AlienVar => (SourceStrat.Dialog, VarType.Alien, false),
				InstructionOpcode.Dialog_AlienVarAddress => (SourceStrat.Dialog, VarType.Alien, true),
				_ => throw new Exception("Unimplemented VarInstruction OpCode")
			};
		}

		public override void WriteAsmString(Decompiler.Writer output)
		{
			if(GetAddress)
			{
				output.Write('&');
			}

			string sourceString = Source switch
			{
				SourceStrat.This => "this",
				SourceStrat.Parent => "parent",
				SourceStrat.Child => "child",
				SourceStrat.Player => "player",
				SourceStrat.Camera => "camera",
				SourceStrat.Boss => "boss",
				SourceStrat.Dialog => "dialog",
				SourceStrat.Target => "target",
				SourceStrat.Target2 => "target2",
				SourceStrat.Collide => "collide",
				_ => throw new Exception("VarInstruction Unknown SourceStrat"),
			};
			output.Write(sourceString);
			output.Write('@');

			string typeString = Type switch
			{
				VarType.Local => "Local_" + VarId,
				VarType.Global => "Global_" + VarId,
				VarType.WorldGlobal => "WorldGlobal_" + VarId,
				VarType.Alien => ((AlienVarID)VarId).GetCommandString(),
				_ => throw new Exception("VarInstruction Unknown VarType"),
			};
			output.Write(typeString);
		}
	}
}