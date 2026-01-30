using System.Text;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.PSX.StratLang
{
	public abstract class BaseInstruction(int opCount,int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):AsmInstruction(address, opcode, opCount, popCount, pushCount);

	/// <summary>Non-terminal instruction that without any extra operands that can pop and push any number of args.</summary>
	public class BasicInstruction(int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):BaseInstruction(0, popCount, pushCount, address, opcode)
	{
		public override string ToAsmString(bool exportForParsing) => OpCode.ToString();
	}

	public sealed class UnimplementedInstruction:BaseInstruction
	{
		public UnimplementedInstruction(int opCount,int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):base(opCount, popCount, pushCount, address, opcode)
		{
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} $ UNIMPLEMENTED";
		}
	};

	//Unimplemented but also used in game
	public sealed class UsedUnimplementedInstruction:BaseInstruction
	{
		public UsedUnimplementedInstruction(int opCount,int popCount,int pushCount, InstructionAddress address, InstructionOpcode opcode):base(opCount, popCount, pushCount, address, opcode)
		{
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} $ USED UNIMPLEMENTED";
		}
	}

	public sealed class AddressInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 1, address, opcode)
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

				if(arg >= reader.WadFile.DPSX.Animations.Count)
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

		public (ActorDataPSX script, InstructionAddress address) GetStratProcAddr(StratReader reader)
		{
			if(!IsDataLoad)
			{
				throw new Exception("Wrong address type");
			}
			//On PC, this is a STPC chunk offset.
			//On PSX, this is a DATA chunk offset
			var script = reader.WadFile.DPSX.GetScript(DataOffset);
			//if(script != reader.Script)
			//{
			//	throw new Exception();
			//}
			return (script, (InstructionAddress)(DataOffset - script.DataChunkAddress));
		}

		public override bool Export => !Consumed;

		public override string ToAsmString(bool exportForParsing)
		{
			if(/*exportForParsing && */Consumed)
			{
				return "";
			}
			var stream = new StringBuilder();
			if(Consumed)
			{
				stream.Append("$ CONSUMED: ");
			}
			if(IsDataLoad)
			{
				stream.Append(OpCode).Append(" DataOffset ").Append(DataOffset);
			}
			else if(IsAnimLoad)
			{
				stream.Append(OpCode).Append(" animload Anim_").Append(AnimationIndex);//animload
			}
			else
			{
				throw new Exception("Unknown stAddress type");
			}
			return stream.ToString();
		}
	}

	public sealed class BinocsInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
	{
		public int State;

		public override void Parse(StratReader reader)
		{
			State = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {State}";
		}
	}

	public sealed class BlinkInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 1, 0, address, opcode)
	{
		public int Count;

		public override void Parse(StratReader reader)
		{
			Count = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Count}";
		}
	}

	public sealed class BranchInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 1, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			var name = OpCode.ToString();
			var label = ConditionalDest.GetLabel();
			return $"{name} {label}";
		}
	}

	public sealed class CollisionFlagInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
	{
		//TODO: Check enum values
		public uint CollisionType;

		public override void Parse(StratReader reader)
		{
			CollisionType = reader.ReadUInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return OpCode switch
			{
				InstructionOpcode.CollisionOn => $"CollisionOn {CollisionType}",
				InstructionOpcode.CollisionOff => $"CollisionOff {CollisionType}",
				_ => throw new NotSupportedException("Unsupported opcode"),
			};
		}
	}

	public sealed class CommandErrorInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(0, 0, 0, address, opcode)
	{
		public override void Parse(StratReader reader)
		{
			Terminal = true;
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return "COMMAND ERROR";
		}
	}

	public sealed class CreditInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
	{
		public int Operand;

		public override void Parse(StratReader reader)
		{
			Operand = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Operand}";
		}
	}

	public sealed class CwgInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Value}";
		}
	}

	public sealed class DialogSayInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} \"{EnglishString}\"";
		}
	}

	public sealed class DialogSetInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
	{
		public int Operand;

		public override void Parse(StratReader reader)
		{
			Operand = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Operand}";
		}
	}


	public sealed class DebugNameInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(2, 0, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Name}";
		}
	}

	public sealed class EndStratInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(0, 0, 0, address, opcode)
	{
		//Deletes strat

		public override void Parse(StratReader reader)
		{
			Terminal = true;
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return "EndStrat";
		}
	}

	public sealed class FlagInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			return NewState ? $"{OpCode} on" : $"{OpCode} off";
		}
	}

	public sealed class FadeSetUnknownInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Value}";
		}
	}

	public sealed class IndexJumpInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 1, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			var stream = new StringBuilder();
			if(exportForParsing)
			{
				stream.Append(OpCode);
				for(int i=0; i<CaseCount; i++)
				{
					stream.Append(' ').Append(CaseComparands[i]).Append(',').Append(this.CaseDestinations[i].GetLabel());
				}
			}
			else
			{
				stream.Append("switch");
				for(int i=0; i<CaseCount; i++)
				{
					stream.Append("\n\t\tcase ").Append(CaseComparands[i]);
					stream.Append("\n\t\t\tgoto ").Append(this.CaseDestinations[i].GetLabel());
				}
				stream.Append("\n\tendswitch");
			}
			return stream.ToString();
		}
	}

	public sealed class ItemCountInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 1, address, opcode)
	{
		//TODO: Check enum values
		public int Item;

		public override void Parse(StratReader reader)
		{
			Item = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode}({Item})";
		}
	}

	public sealed class ItemChangeInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
	{
		//Give/take item from inventory

		//TODO: Check enum values
		public int Item;

		public override void Parse(StratReader reader)
		{
			Item = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Item}";
		}
	}

	public sealed class JumpInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Destination.GetLabel()}";
		}
	}

	public sealed class JumpSubroutineInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Proc.SubroutineName()}";
		}
	}

	public sealed class NumberInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 1, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Value}";
		}
	}

	public sealed class PrintInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(-1, 0, 0, address, opcode)
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
						value = reader.ReadInt();
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
					case InstructionOpcode.String or (InstructionOpcode)225://225 is for DUMMY
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

		public override string ToAsmString(bool exportForParsing)
		{
			var stream = new StringBuilder();
			stream.Append(OpCode);
			foreach((var type,var value,var negate) in Elements)
			{
				stream.Append(' ');
				if(negate)
				{
					stream.Append('-');
				}
				switch(type)
				{
					case InstructionOpcode.Local:
					{
						int localValue = (int)value;
						stream.Append($"Local_{localValue}");
						break;
					}
					case InstructionOpcode.Global:
					{
						int globalValue = (int)value;
						stream.AppendFormat("{0:X5}.{1:X3}", globalValue >> 12, globalValue & 0xFFF);//"%05x.%03x"
						break;
					}
					case InstructionOpcode.WorldGlobal:
					{
						int worldGlobalValue = (int)value;
						stream.Append($"WorldGlobal_{worldGlobalValue}");
						break;
					}
					case InstructionOpcode.AlienVar:
					{
						stream.Append("this@").Append(((AlienVarID)value).GetCommandString());
						break;
					}
					case InstructionOpcode.Number:
					{
						int number = (int)value;
						stream.AppendFormat("{0:X5}.{1:X3}", number >> 12, number & 0xFFF);//"%05x.%03x"
						break;
					}
					case InstructionOpcode.Ext_AlienVar:
					{
						stream.Append("parent@").Append(((AlienVarID)value).GetCommandString());
						break;
					}
					case InstructionOpcode.Player_AlienVar:
					{
						stream.Append("player@").Append(((AlienVarID)value).GetCommandString());
						break;
					}
					case InstructionOpcode.Camera_AlienVar:
					{
						stream.Append("camera@").Append(((AlienVarID)value).GetCommandString());
						break;
					}
					//TODO: Modify String Opcode mapping for DUMMY
					case InstructionOpcode.String or (InstructionOpcode)225://225 is for DUMMY
					{
						var str = (string)value;
						//TODO: Sanitize strings
						str = str.Replace("\n", "\\n");
						stream.Append('\"').Append(str).Append('\"');
						break;
					}
					default:
					{
						throw new Exception("Invalid print type");
					}
				}
			}
			return stream.ToString();
		}
	}

	public sealed class ReturnInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(0, 0, 0, address, opcode)
	{
		//Technically pop a values but we aren't counting it because it is pushed outisde the subroutine.
	
		//Special: Function Return

		public override void Parse(StratReader reader)
		{
			Terminal = true;
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return "return";
		}
	}

	public sealed class SoundAddressInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 1, address, opcode)
	{
		public int Value;

		public override void Parse(StratReader reader)
		{
			Value = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Value}";
		}
	}

	public abstract class BaseSpawnInstruction(int opCount, InstructionAddress address, InstructionOpcode opcode):AsmInstruction(address, opcode, opCount, 1, 0)
	{
		public int LocalVarsToPop;
		public int LocalCount;
		public int TriggerCount;
		public int CollisionSize;
		public int CollisionBoneCount;
		
		public ActorDataPSX SpawnStratProcScript;
		public InstructionAddress SpawnStratProcAddr;
		public AsmInstruction SpawnStratProc;
	}

	public class SpawnInstruction(InstructionAddress address, InstructionOpcode opcode):BaseSpawnInstruction(5, address, opcode)
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
			SpawnStratProc = parser.ParseStrat(SpawnStratProcAddr, this, false);
		}

		public override string ToAsmString(bool exportForParsing)
		{
			//return $"{OpCode} {((AddressInstruction)Prev!).DataOffset:X8} {LocalVarsToPop} {LocalCount} {TriggerCount} {CollisionSize} {CollisionBoneCount}";
			return $"{OpCode} {SpawnStratProc.SubroutineName()} {LocalVarsToPop} {LocalCount} {TriggerCount} {CollisionSize} {CollisionBoneCount}";
		}
	}

	public sealed class SpawnAfterInstruction(InstructionAddress address, InstructionOpcode opcode):SpawnInstruction(address,opcode);

	public sealed class SpawnFromInstruction(InstructionAddress address, InstructionOpcode opcode):BaseSpawnInstruction(6, address, opcode)
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
			SpawnStratProc = parser.ParseStrat(SpawnStratProcAddr, this, false);
		}

		public override string ToAsmString(bool exportForParsing)
		{
			//return $"{OpCode} {((AddressInstruction)Prev!).DataOffset:X8} {LocalVarsToPop} {LocalCount} {TriggerCount} {CollisionSize} {CollisionBoneCount} {BoneToSpawnFrom}";
			return $"{OpCode} {SpawnStratProc.SubroutineName()} {LocalVarsToPop} {LocalCount} {TriggerCount} {CollisionSize} {CollisionBoneCount} {BoneToSpawnFrom}";
		}
	}

	public sealed class StackCompareInstruction:BaseInstruction
	{
		//This is never used in Croc 2. It isn't in the Aladdin's ASL compiler either.

		//This techniclaly could be considered pulling one value and pushing two.

		//Special: Uses Peek on stack
		public int Comparand;

		public StackCompareInstruction(InstructionAddress address, InstructionOpcode opcode):base(1, 0, 1, address, opcode)
		{
			//Peeking values or pushing multiple values would make stack analysis and reconstruction very difficult.
			//Since it isn't even used in any code I've seen, we are not going to support it.
			throw new NotSupportedException();
		}

		public override void Parse(StratReader reader)
		{
			Comparand = reader.ReadInt();
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {Comparand}";
		}
	}

	public sealed class TriggerCreateInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(2, 0, 0, address, opcode)
	{
		//Special: Extra data

		public int TriggerIndex;

		public TriggerTypePSX Type;
		public int Arg;
		public InstructionAddress StreamPtr;
		public AsmInstruction Stream;

		public override void Parse(StratReader reader)
		{
			var dataOffset = reader.ReadRelativeAddress();
			Type = (TriggerTypePSX)reader.ReadAt(dataOffset, 0);
			Arg = reader.ReadAt(dataOffset, 1);

			var streamOffset = reader.ReadAt(dataOffset, 2) * sizeof(int);
			StreamPtr = reader.Position + streamOffset;

			TriggerIndex = reader.ReadInt();
		}

		public override void Setup(StratParser parser)
		{
			Stream = parser.ParseTrigger(StreamPtr, TriggerIndex, this);
		}

		public override string ToAsmString(bool exportForParsing)
		{
			return $"{OpCode} {(int)Type} {Arg} {Stream.SubroutineName()}";
		}
	}

	public sealed class TriggerUpdateInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 0, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			//TODO: TriggerIndex should be replaced with trigger function name
			return $"{OpCode} {TriggerProc.SubroutineName()}";
		}
	}

	public sealed class VarInstruction(InstructionAddress address, InstructionOpcode opcode):BaseInstruction(1, 0, 1, address, opcode)
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

		public override string ToAsmString(bool exportForParsing)
		{
			var stream = new StringBuilder();
			if(exportForParsing)
			{
				stream.Append(OpCode).Append(' ');
			}
			else
			{
				if(GetAddress)
				{
					stream.Append('&');
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
				stream.Append(sourceString);
				stream.Append('@');
			}

			string typeString = Type switch
			{
				VarType.Local => "Local_" + VarId,
				VarType.Global => "Global_" + VarId,
				VarType.WorldGlobal => "WorldGlobal_" + VarId,
				VarType.Alien => ((AlienVarID)VarId).GetCommandString(),
				_ => throw new Exception("VarInstruction Unknown VarType"),
			};
			stream.Append(typeString);
			return stream.ToString();
		}
	}
}