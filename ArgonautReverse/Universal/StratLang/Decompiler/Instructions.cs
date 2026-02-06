using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using ArgonautReverse.Universal.StratLang.Disassembler;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public static class InstructionLookup
	{
		private delegate Instruction DelCreateInstruction();
		private static readonly Dictionary<string,DelCreateInstruction> byName = new Dictionary<string,DelCreateInstruction>(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<InstructionOpcode,DelCreateInstruction> byOpcode = new Dictionary<InstructionOpcode,DelCreateInstruction>();

		public static readonly HashSet<string> Used = new HashSet<string>();//TODO: Make InstructionOpcode

		static InstructionLookup()
		{
			var createInstructionGenericInfo = ((DelCreateInstruction)CreateInstructionInner<NOPInstruction>).Method.GetGenericMethodDefinition();
			var types = typeof(Instruction).Assembly.GetTypes();
			foreach(var type in types)
			{
				if(type.GetCustomAttribute<OpcodeAttribute>() is OpcodeAttribute opcodeAttribute)
				{
					var createInstruction = createInstructionGenericInfo.MakeGenericMethod(type).CreateDelegate<DelCreateInstruction>();
					byName.Add(opcodeAttribute.Opcode.ToString(), createInstruction);
					byOpcode.Add(opcodeAttribute.Opcode, createInstruction);
				}
			}

			static Instruction CreateInstructionInner<T>() where T:Instruction,new()
			{
				var instr = new T();
				return instr;
			}
		}

		public static Instruction CreateInstruction(string name)
		{
			Used.Add(name);

			return byName[name]();
		}

		public static Instruction CreateInstruction(InstructionOpcode opcode)
		{
			Used.Add(opcode.ToString());

			return byOpcode[opcode]();
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class OpcodeAttribute(InstructionOpcode opcode):Attribute
	{
		public InstructionOpcode Opcode{get;} = opcode;
	}

	public abstract class UnimplementedInstruction:Instruction
	{
		public sealed override IStackOperation? StackOperation => null;

		public UnimplementedInstruction(bool fail = true)
		{
			if(fail)
			{
				throw new NotImplementedException("Instruction is not implmented in Croc 2");
			}
			else
			{
				Console.WriteLine($"Instruction {GetType()} is not implmented in Croc 2");
			}
		}
	}

	public abstract class BaseOperandInstruction:Instruction
	{
		public abstract class BaseOperandStack<TInstruction>(TInstruction instruction):Stack<TInstruction>(instruction) where TInstruction:BaseOperandInstruction;
	}

	public abstract class BaseConsumerInstruction:BaseOperandInstruction
	{
		public abstract class BaseConsumerStack<TInstruction>(TInstruction instruction):BaseOperandStack<TInstruction>(instruction),IStackConsumer where TInstruction:BaseConsumerInstruction
		{
			public abstract int PopCount{get;}

			public IStackProducer[] Operands{get;private set;}

			public override void Analyze(StackAnalyzer stack)
			{
				Operands = new IStackProducer[PopCount];
				//Reverse order
				for(int i=PopCount-1; i>=0; i--)
				{
					var operand = stack.Pop();
					if(operand.Consumer != null)
					{
						throw new Exception("Consumer already set");
					}
					operand.Consumer = this;

					Operands[i] = operand;
				}
			}

			public override IEnumerable<IStackOperation> GetRootOperations() => Operands.SelectMany(o => o.GetRootOperations());

			public override bool TryGetSubroutine([MaybeNullWhen(false)]out AsmInstruction subroutine)
			{
				foreach(var operand in Operands)
				{
					var producer = (IStackLabellable)operand;
					if(producer.TryGetSubroutine(out var producerSubroutine))
					{
						subroutine = producerSubroutine;
						return true;
					}
				}
				subroutine = null;
				return false;
			}

			public override bool TryGetLabel([MaybeNullWhen(false)]out AsmInstruction label)
			{
				label = null;
				foreach(var operand in Operands)
				{
					var producer = (IStackLabellable)operand;
					if(producer.TryGetLabel(out var producerLabel))
					{
						if(label != null)
						{
							throw new Exception("Label already found");
						}
						label = producerLabel;
					}
				}
				return label != null && label.HasLabel;
			}
		}
	}

	public abstract class PureConsumerInstruction:BaseConsumerInstruction
	{
		public sealed override IStackOperation StackOperation => StackStatement;
		public abstract IStackStatement StackStatement{get;}
		public abstract FlowStatement FlowStatement{get;}

		public abstract class PureConsumerStack<TInstruction>(TInstruction instruction):BaseConsumerStack<TInstruction>(instruction),IStackStatement where TInstruction:PureConsumerInstruction
		{
			public Instruction StatementInstruction => Instruction;
			public FlowStatement FlowStatement => Instruction.FlowStatement;

			public override IStackStatement Statement => this;

			public AsmInstruction StatementLabel{get;private set;}
			public Instruction FirstInstruction{get;private set;}
			
			public IStackStatement NextStatement{get;set;}
			public IStackStatement PrevStatement{get;set;}

			public abstract string ToStatement();

			public override void Analyze(StackAnalyzer stack)
			{
				base.Analyze(stack);
			
				StatementLabel = stack.CurrentStatementFirstInstruction.AsmLabel;
				FirstInstruction = stack.CurrentStatementFirstInstruction;
			}
		}

		public abstract class PureConsumerFlow<TInstruction>(TInstruction instruction):FlowStatement<TInstruction>(instruction) where TInstruction:PureConsumerInstruction
		{
			public sealed override IStackStatement StackStatement => Instruction.StackStatement;
		}
	}

	public abstract class SimplePureConsumerInstruction:PureConsumerInstruction
	{
		public sealed override SimplePureConsumerFlow FlowStatement{get;}

		public SimplePureConsumerInstruction()
		{
			FlowStatement = new(this);
		}

		public abstract class SimplePureConsumerStack<TInstruction>(TInstruction instruction):PureConsumerStack<TInstruction>(instruction) where TInstruction:SimplePureConsumerInstruction;

		public sealed class SimplePureConsumerFlow(SimplePureConsumerInstruction instruction):PureConsumerFlow<SimplePureConsumerInstruction>(instruction)
		{
			public override void Analyze(FlowAnalyzer flow){}
		}
	}

	public abstract class PureProducerInstruction:BaseOperandInstruction
	{
		public sealed override IStackOperation StackOperation => StackProducer;
		public abstract IStackProducer StackProducer{get;}

		public virtual bool Literal => false;

		public abstract class PureProducerStack<TInstruction>(TInstruction instruction):BaseOperandStack<TInstruction>(instruction),IStackProducer where TInstruction:PureProducerInstruction
		{
			public IStackConsumer Consumer{get;set;}

			public sealed override IStackStatement Statement => Consumer.Statement;

			public virtual bool Literal => false;

			public sealed override void Analyze(StackAnalyzer stack)
			{
				stack.Push(this);
			}

			public override IEnumerable<IStackOperation> GetRootOperations(){yield return this;}

			public abstract string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown);
			public virtual string ToConditionStr(bool checkTrue) => checkTrue ? $"{ToExpressionString()} != 0" : $"{ToExpressionString()} = 0";

			public sealed override bool TryGetSubroutine(out AsmInstruction subroutine)
			{
				subroutine = Instruction.AsmLabel;
				return Instruction.AsmLabel.IsSubroutineEntry;
			}

			public sealed override bool TryGetLabel(out AsmInstruction label)
			{
				label = Instruction.AsmLabel;
				return label.HasLabel;
			}
		}
	}

	public abstract class BaseExpressionInstruction:BaseConsumerInstruction
	{
		public abstract class BaseExpressionStack<TInstruction>(TInstruction instruction):BaseConsumerStack<TInstruction>(instruction),IStackExpression where TInstruction:BaseExpressionInstruction
		{
			public IStackConsumer Consumer{get;set;}

			public sealed override IStackStatement Statement => Consumer.Statement;

			public sealed override void Analyze(StackAnalyzer stack)
			{
				base.Analyze(stack);
				stack.Push(this);
			}

			public abstract string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown);
			public virtual string ToConditionStr(bool checkTrue) => checkTrue ? $"{ToExpressionString()} != 0" : $"{ToExpressionString()} = 0";
		}
	}

	/// <summary>Instructions that don't use the stack</summary>
	public abstract class NoStackInstruction:Instruction
	{
		public sealed override IStackOperation StackOperation => StackStatement;
		public abstract IStackStatement StackStatement{get;}

		public abstract FlowStatement FlowStatement{get;}

		public abstract class NoStackStack<TInstruction>(TInstruction instruction):Stack<TInstruction>(instruction),IStackStatement where TInstruction:NoStackInstruction
		{
			public Instruction OperationInstruction => Instruction;

			public Instruction StatementInstruction => Instruction;
			public FlowStatement FlowStatement => Instruction.FlowStatement;

			public AsmInstruction StatementLabel => Instruction.AsmLabel;
			public Instruction FirstInstruction => Instruction;

			public sealed override IStackStatement Statement => this;

			public IStackStatement NextStatement{get;set;}
			public IStackStatement PrevStatement{get;set;}

			public IStackStatement StackStatement => this;

			public override void Analyze(StackAnalyzer stack){}

			public abstract string ToStatement();

			public sealed override IEnumerable<IStackOperation> GetRootOperations() => [this];

			public sealed override bool TryGetSubroutine([MaybeNullWhen(false)]out AsmInstruction subroutine)
			{
				if(Instruction.AsmLabel.IsSubroutineEntry)
				{
					subroutine = Instruction.AsmLabel;
					return true;
				}
				subroutine = null;
				return false;
			}

			public sealed override bool TryGetLabel([MaybeNullWhen(false)]out AsmInstruction label)
			{
				if(Instruction.AsmLabel.HasLabel)
				{
					label = Instruction.AsmLabel;
					return true;
				}
				label = null;
				return false;
			}
		}

		public abstract class NoStackFlow<TInstruction>(TInstruction instruction):FlowStatement<TInstruction>(instruction) where TInstruction:NoStackInstruction
		{
			public sealed override IStackStatement StackStatement => Instruction.StackStatement;
		}
	}
	public abstract class SimpleNoStackInstruction:NoStackInstruction
	{
		public sealed override SimpleNoStackFlow FlowStatement{get;}

		public SimpleNoStackInstruction()
		{
			FlowStatement = new(this);
		}

		public abstract class SimpleNoStackStack<TInstruction>(TInstruction instruction):NoStackStack<TInstruction>(instruction) where TInstruction:SimpleNoStackInstruction;

		public sealed class SimpleNoStackFlow(SimpleNoStackInstruction instruction):NoStackFlow<SimpleNoStackInstruction>(instruction)
		{
			public override void Analyze(FlowAnalyzer flow){}
		}
	}

	public abstract class SimpleNoOperandsNoStackInstruction:SimpleNoStackInstruction
	{
		public sealed override SimpleNoOperandsNoStackStack StackStatement{get;}

		public SimpleNoOperandsNoStackInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SimpleNoOperandsNoStackStack(SimpleNoOperandsNoStackInstruction instruction):SimpleNoStackStack<SimpleNoOperandsNoStackInstruction>(instruction)
		{
			public sealed override string ToStatement()
			{
				//TODO: Make this better
				string name = Instruction.GetType().GetCustomAttribute<OpcodeAttribute>()!.Opcode.ToString();
				return name;
			}
		}
	}

	public abstract class BinaryOperationInstruction:BaseExpressionInstruction
	{
		public abstract class BinaryOperationStack<TInstruction>(TInstruction instruction):BaseExpressionStack<TInstruction>(instruction) where TInstruction:BinaryOperationInstruction
		{
			public override int PopCount => 2;

			public IStackProducer ValueA => Operands[0];
			public IStackProducer ValueB => Operands[1];
		}
	}

	public abstract class UnaryOperationInstruction:BaseExpressionInstruction
	{
		public abstract class UnaryOperationStack<TInstruction>(TInstruction instruction):BaseExpressionStack<TInstruction>(instruction) where TInstruction:UnaryOperationInstruction
		{
			public override int PopCount => 1;

			public IStackProducer Value => Operands[0];
		}
	}

	public abstract class BranchInstruction:PureConsumerInstruction
	{
		public Instruction ConditionalDest;
		public sealed override BranchFlow FlowStatement{get;}

		public BranchInstruction()
		{
			FlowStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);

			var branchAsmInstr = GetAsmInstruction<Disassembler.BranchInstruction>();
			ConditionalDest = parser.GetInstruction(branchAsmInstr.ConditionalDest, null, this);
		}

		public abstract class BranchStack<TInstruction>(TInstruction instruction):PureConsumerStack<TInstruction>(instruction) where TInstruction:BranchInstruction
		{
			public override int PopCount => 1;

			public IStackProducer Condition => Operands[0];

			public override void Analyze(StackAnalyzer stack)
			{
				base.Analyze(stack);
				stack.AddDest(Instruction.ConditionalDest);
			}
		}

		public sealed class BranchFlow(BranchInstruction instruction):PureConsumerFlow<BranchInstruction>(instruction),IFlowControl
		{
			public IStackStatement ControlStatement => Instruction.StackStatement;

			public FlowStatement FlowConditionalDest => FlowDestinations[0];

			public override void Analyze(FlowAnalyzer flow)
			{
				flow.AddDest(this, Instruction.ConditionalDest);
			}
		}
	}

	public abstract class DialogSayInstruction:SimpleNoStackInstruction
	{
		public string EnglishString;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var dialogSayAsmInstr = GetAsmInstruction<Disassembler.DialogSayInstruction>();

			EnglishString = dialogSayAsmInstr.EnglishString;
		}

		public abstract class DialogSayStack<TInstruction>(TInstruction instruction):SimpleNoStackStack<TInstruction>(instruction) where TInstruction:DialogSayInstruction;
	}

	public abstract class DialogSetInstruction:SimpleNoStackInstruction
	{
		public bool State;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var dialogSetAsmInstr = GetAsmInstruction<Disassembler.DialogSetInstruction>();

			State = dialogSetAsmInstr.Operand switch
			{
				0 => false,
				1 => true,
				_ => throw new Exception("Unknown state")
			};
		}

		public abstract class DialogSetStack<TInstruction>(TInstruction instruction):SimpleNoStackStack<TInstruction>(instruction) where TInstruction:DialogSetInstruction;
	}

	public abstract class FlagInstruction:SimpleNoStackInstruction
	{
		public readonly string Flag;

		//Operation
		//public int FlagGroup;
		//public int FlagChanged;

		//Operand
		public bool NewState;

		public sealed override FlagStack StackStatement{get;}

		public FlagInstruction()
		{
			StackStatement = new(this);
			Flag = this.GetType().GetCustomAttribute<OpcodeAttribute>().Opcode.ToString();
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var flagAsmInstr = GetAsmInstruction<Disassembler.FlagInstruction>();

			NewState = flagAsmInstr.NewState;
		}

		public sealed class FlagStack(FlagInstruction instruction):SimpleNoStackStack<FlagInstruction>(instruction)
		{
			public override string ToStatement() => $"{Instruction.Flag} {(Instruction.NewState ? "on" : "off")}";
		}
	}

	public abstract class ItemChangeInstruction:SimpleNoStackInstruction
	{
		//Give/take item from inventory

		//Operand
		public int Item;

		//Operation
		public abstract int Change{get;}

		public sealed override ItemChangeStack StackStatement{get;}

		public ItemChangeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var itemChangeAsmInstr = GetAsmInstruction<Disassembler.ItemChangeInstruction>();

			Item = itemChangeAsmInstr.Item;
		}

		public sealed class ItemChangeStack(ItemChangeInstruction instruction):SimpleNoStackStack<ItemChangeInstruction>(instruction)
		{
			public override string ToStatement() => Instruction.Change switch
			{
				-1 => $"DelInv {Instruction.Item}",
				+1 => $"AddInv {Instruction.Item}",
				_ => throw new Exception()
			};
		}
	}

	public abstract class BaseJumpInstruction:NoStackInstruction
	{
		public sealed override bool Terminal => true;

		public Instruction Destination;

		public sealed override JumpFlow FlowStatement{get;}

		public BaseJumpInstruction()
		{
			FlowStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var jumpAsmInstr = GetAsmInstruction<Disassembler.JumpInstruction>();
			Destination = parser.GetInstruction(jumpAsmInstr.Destination, null, this);
		}

		public abstract class BaseJumpStack<TInstruction>(TInstruction instruction):NoStackStack<TInstruction>(instruction) where TInstruction:BaseJumpInstruction
		{
			public override void Analyze(StackAnalyzer stack)
			{
				base.Analyze(stack);

				stack.AddDest(Instruction.Destination);
			}
		}

		public sealed class JumpFlow(BaseJumpInstruction instruction):NoStackFlow<BaseJumpInstruction>(instruction),IFlowTerminal,IFlowControl
		{
			public IStackStatement ControlStatement => Instruction.StackStatement;

			public FlowStatement FlowDestination => FlowDestinations[0];

			public override void Analyze(FlowAnalyzer flow)
			{
				flow.AddDest(this, Instruction.Destination);
			}
		}
	}

	public abstract class BaseJumpSubroutineInstruction:SimpleNoStackInstruction
	{
		//Technically pushes a value but we aren't counting it because it's popped outside of this subroutine.

		public Instruction Proc;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var jumpSubroutineAsmInstr = GetAsmInstruction<Disassembler.JumpSubroutineInstruction>();
			Proc = parser.GetProc(jumpSubroutineAsmInstr.Proc, this);
		}

		public abstract class BaseJumpSubroutineStack<TInstruction>(TInstruction instruction):SimpleNoStackStack<BaseJumpSubroutineInstruction>(instruction) where TInstruction:BaseJumpSubroutineInstruction;
	}

	public abstract class BaseMoveInstruction:SimplePureConsumerInstruction
	{
		public abstract class BaseMoveStack<TInstruction>(TInstruction instruction):SimplePureConsumerStack<BaseMoveInstruction>(instruction) where TInstruction:BaseMoveInstruction
		{
			public override int PopCount => 1;

			public IStackProducer Amount => Operands[0];
		}
	}

	public abstract class BasePrintInstruction:SimpleNoStackInstruction
	{
		public string Data;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var printAsmInstr = GetAsmInstruction<Disassembler.PrintInstruction>();
			//TODO: Handle PrintInstruction string args with spaces
			Data = GetPrintString(printAsmInstr);
		}

		public static string GetPrintString(Disassembler.PrintInstruction printAsmInstr)
		{
			var parts = new List<string>();
			foreach((var type,var value,var negate) in printAsmInstr.Elements)
			{
				string part;
				switch(type)
				{
					//TODO: Modify Opcode mappings for DUMMY

					case InstructionOpcode.Local:
					{
						int localValue = (int)value;
						part = $"Local_{localValue}";
						break;
					}
					case InstructionOpcode.Global:
					{
						int globalValue = (int)value;
						part = $"{globalValue>>12 : X5}.{globalValue&0xFFF : X3}";//"%05x.%03x"
						break;
					}
					case InstructionOpcode.WorldGlobal:
					{
						int worldGlobalValue = (int)value;
						part = $"WorldGlobal_{worldGlobalValue}";
						break;
					}
					case InstructionOpcode.AlienVar:
					{
						part = $"this@{((AlienVarID)value).GetCommandString()}";
						break;
					}
					case InstructionOpcode.Number:
					{
						int number = (int)value;
						part = $"{number>>12 : X5}.{number&0xFFF : X3}";//"%05x.%03x"
						break;
					}
					case InstructionOpcode.Ext_AlienVar:
					{
						part = $"parent@{((AlienVarID)value).GetCommandString()}";
						break;
					}
					case InstructionOpcode.Player_AlienVar:
					{
						part = $"player@{((AlienVarID)value).GetCommandString()}";
						break;
					}
					case InstructionOpcode.Camera_AlienVar:
					{
						part = $"camera@{((AlienVarID)value).GetCommandString()}";
						break;
					}
					case InstructionOpcode.String or (InstructionOpcode)225://225 is for DUMMY
					{
						var str = (string)value;
						//TODO: More string sanitization
						str = str.Replace("\n", "\\n");
						part = $"\"{str}\"";
						break;
					}
					default:
					{
						throw new Exception("Invalid print type");
					}
				}
				if(negate)
				{
					part = '-' + part;
				}
				parts.Add(part);
			}
			return string.Join(';', parts);
		}
	}

	public abstract class BaseSpawnInstruction:SimpleNoStackInstruction
	{
		//Normally this would pop the Proc address off the stack,
		//but we merged it with this instruction in the extractor.
		
		public int LocalVarsToPop;
		public int LocalCount;
		public int TriggerCount;
		public int CollisionSize;
		public int CollisionBoneCount;

		public Instruction SpawnStratProc;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);

			var spawnAsmInstr = GetAsmInstruction<Disassembler.BaseSpawnInstruction>();
			
			SpawnStratProc = parser.GetStrat(spawnAsmInstr.SpawnStratProc, this);

			LocalVarsToPop = spawnAsmInstr.LocalVarsToPop;
			LocalCount = spawnAsmInstr.LocalCount;
			TriggerCount = spawnAsmInstr.TriggerCount;
			CollisionSize = spawnAsmInstr.CollisionSize;
			CollisionBoneCount = spawnAsmInstr.CollisionBoneCount;
		}

		public abstract class BaseSpawnStack<TInstruction>(TInstruction instruction):SimpleNoStackStack<TInstruction>(instruction) where TInstruction:BaseSpawnInstruction;
	}

	public abstract class TriggerUpdateInstruction:SimpleNoStackInstruction
	{
		public Instruction TriggerProc;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var triggerUpdateAsmInstr = GetAsmInstruction<Disassembler.TriggerUpdateInstruction>();
			TriggerProc = parser.GetTrigger(triggerUpdateAsmInstr.TriggerProc, this);
		}

		public abstract class TriggerUpdateStack<TInstruction>(TInstruction instruction):SimpleNoStackStack<TInstruction>(instruction) where TInstruction:TriggerUpdateInstruction;
	}

	public abstract class VarInstruction:PureProducerInstruction
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
		public abstract SourceStrat Source{get;}
		public abstract VarType Type{get;}
		public string ID;

		//The address ones seem to be used exclusively for setting a particular varible, no pointer math.
		//Strat Lang didn't have pointers
		public abstract bool GetAddress{get;}//false to get value of var, true to get address of var

		public sealed override VarStack StackProducer{get;}

		public VarInstruction()
		{
			StackProducer = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var varAsmInstr = GetAsmInstruction<Disassembler.VarInstruction>();
			var varId = varAsmInstr.VarId;

			ID = Type switch
			{
				VarType.Local => "Local_" + varId,
				VarType.Global => "Global_" + varId,
				VarType.WorldGlobal => "WorldGlobal_" + varId,
				VarType.Alien => ((AlienVarID)varId).GetCommandString(),
				_ => throw new Exception("VarInstruction Unknown VarType"),
			};
		}

		public sealed class VarStack(VarInstruction instruction):PureProducerStack<VarInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown)
			{
				string varName;
				if(Instruction.Source != SourceStrat.This)
				{
					varName = $"{Instruction.Source}@{Instruction.ID}";
				}
				else
				{
					varName = Instruction.ID;
				}
				if((requestType==ExpressionType.Dereference) == Instruction.GetAddress)
				{
					return varName;
				}
				else if((requestType!=ExpressionType.Dereference) && Instruction.GetAddress)
				{
					return "&" + varName;
				}
				else//if (dereference && !GetAddress)
				{
					throw new Exception();
				}
			}
		}
	}

	[Opcode(InstructionOpcode.CommandError)]
	public sealed class CommandErrorInstruction:NoStackInstruction
	{
		public sealed override bool Terminal => true;
		
		public sealed override CommandErrorStack StackStatement{get;}
		public sealed override CommandErrorFlow FlowStatement{get;}

		public CommandErrorInstruction()
		{
			StackStatement = new(this);
			FlowStatement = new(this);
		}

		public sealed class CommandErrorStack(CommandErrorInstruction instruction):NoStackStack<CommandErrorInstruction>(instruction)
		{
			public override string ToStatement() => "COMMAND ERROR";
		}

		public sealed class CommandErrorFlow(CommandErrorInstruction instruction):NoStackFlow<CommandErrorInstruction>(instruction),IFlowTerminal
		{
			public override void Analyze(FlowAnalyzer flow){}
		}
	}

	[Opcode(InstructionOpcode.Local)]
	public sealed class LocalInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.Local;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Global)]
	public sealed class GlobalInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.Global;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.WorldGlobal)]
	public sealed class WorldGlobalInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.WorldGlobal;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.AlienVar)]
	public sealed class AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.LocalAddress)]
	public sealed class LocalAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.Local;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.GlobalAddress)]
	public sealed class GlobalAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.Global;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.WorldGlobalAddress)]
	public sealed class WorldGlobalAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.WorldGlobal;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.AlienVarAddress)]
	public sealed class AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.This;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Print)]
	public sealed class PrintInstruction:BasePrintInstruction
	{
		public sealed override PrintStack StackStatement{get;}

		public PrintInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class PrintStack(PrintInstruction instruction):SimpleNoStackStack<PrintInstruction>(instruction)
		{
			public override string ToStatement() => $"print {Instruction.Data}";
		}
	}

	[Opcode(InstructionOpcode.Number)]
	public sealed class NumberInstruction:PureProducerInstruction
	{
		public sealed override bool Literal => true;

		public int Value;

		public sealed override NumberStack StackProducer{get;}

		public NumberInstruction()
		{
			StackProducer = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var numberAsmInstr = GetAsmInstruction<Disassembler.NumberInstruction>();

			Value = numberAsmInstr.Value;
		}

		//TODO: We will want to move away from this
		private static bool TryGetValue(int fixed12, out double value)
		{
			//Numbers were converted to ASL using the following formal:
			//((Floor(value)*0xFFFFF)<<12) | (Floor(value*4096)&0xFFF)
			//A lot of common numbers would have been truncated so this is a lookup for common values

			if((fixed12&0xFFF) == 0)
			{
				value = fixed12 >> 12;
				return true;
			}

			switch(fixed12)
			{
				case 81:	value = 0.02;return true;
				case 204:	value = 0.05;return true;
				case 409:	value = 0.10;return true;
				case 2048:	value = 0.50;return true;
				case 2867:	value = 0.70;return true;
				case 4915:	value = 1.20;return true;
				case 5120:	value = 1.25;return true;
				case 6144:	value = 1.50;return true;
			};
			value = 0;
			return false;
		}

		public sealed class NumberStack(NumberInstruction instruction):PureProducerStack<NumberInstruction>(instruction)
		{
			//TODO: We will want to move away from this
			private static bool TryGetValue(int fixed12, out double value)
			{
				//Numbers were converted to ASL using the following formal:
				//((Floor(value)*0xFFFFF)<<12) | (Floor(value*4096)&0xFFF)
				//A lot of common numbers would have been truncated so this is a lookup for common values

				if((fixed12&0xFFF) == 0)
				{
					value = fixed12 >> 12;
					return true;
				}

				switch(fixed12)
				{
					case 81:	value = 0.02;return true;
					case 204:	value = 0.05;return true;
					case 409:	value = 0.10;return true;
					case 2048:	value = 0.50;return true;
					case 2867:	value = 0.70;return true;
					case 4915:	value = 1.20;return true;
					case 5120:	value = 1.25;return true;
					case 6144:	value = 1.50;return true;
				};
				value = 0;
				return false;
			}

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown)
			{
				if(requestType == ExpressionType.Integer)
				{
					return Instruction.Value.ToString();
				}
				if(requestType == ExpressionType.FixedPoint)
				{
					if(TryGetValue(Instruction.Value, out var converted))
					{
						return converted.ToString();
					}
					return (Instruction.Value/4096.0).ToString();
				}
				return $"UnknownValue({Instruction.Value})";
			}
		}
	}

	[Opcode(InstructionOpcode.UMinus)]
	public sealed class UMinusInstruction:UnaryOperationInstruction
	{
		public sealed override UMinusStack StackOperation{get;}

		public UMinusInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class UMinusStack(UMinusInstruction instruction):UnaryOperationStack<UMinusInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"-{Value.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Increase)]
	public sealed class IncreaseInstruction:SimplePureConsumerInstruction
	{
		public sealed override IncreaseStack StackStatement{get;}

		public IncreaseInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class IncreaseStack(IncreaseInstruction instruction):SimplePureConsumerStack<IncreaseInstruction>(instruction)
		{
			public override int PopCount => 1;

			public VarInstruction.VarStack Address => (VarInstruction.VarStack)Operands[0];

			public override string ToStatement() => $"{Address.ToExpressionString(ExpressionType.Dereference)}++";
		}
	}

	[Opcode(InstructionOpcode.Decrease)]
	public sealed class DecreaseInstruction:SimplePureConsumerInstruction
	{
		public sealed override DecreaseStack StackStatement{get;}

		public DecreaseInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class DecreaseStack(DecreaseInstruction instruction):SimplePureConsumerStack<DecreaseInstruction>(instruction)
		{
			public override int PopCount => 1;

			public VarInstruction.VarStack Address => (VarInstruction.VarStack)Operands[0];

			public override string ToStatement() => $"{Address.ToExpressionString(ExpressionType.Dereference)}--";
		}
	}

	[Opcode(InstructionOpcode.Add)]
	public sealed class AddInstruction:BinaryOperationInstruction
	{
		public sealed override AddStack StackOperation{get;}

		public AddInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class AddStack(AddInstruction instruction):BinaryOperationStack<AddInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} + {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Sub)]
	public sealed class SubInstruction:BinaryOperationInstruction
	{
		public sealed override SubStack StackOperation{get;}

		public SubInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SubStack(SubInstruction instruction):BinaryOperationStack<SubInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} - {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Mul)]
	public sealed class MulInstruction:BinaryOperationInstruction
	{
		public sealed override MulStack StackOperation{get;}

		public MulInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class MulStack(MulInstruction instruction):BinaryOperationStack<MulInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} * {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Div)]
	public sealed class DivInstruction:BinaryOperationInstruction
	{
		public sealed override DivStack StackOperation{get;}

		public DivInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class DivStack(DivInstruction instruction):BinaryOperationStack<DivInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} / {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Equals)]
	public sealed class EqualsInstruction:SimplePureConsumerInstruction
	{
		public sealed override EqualsStack StackStatement{get;}

		public EqualsInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class EqualsStack(EqualsInstruction instruction):SimplePureConsumerStack<EqualsInstruction>(instruction)
		{
			public override int PopCount => 2;

			public VarInstruction.VarStack Address => (VarInstruction.VarStack)Operands[0];
			public IStackProducer Value => Operands[1];

			//TODO: Value isn't always fixed point, if it is a NumberInstruction, it generally will be.
			public override string ToStatement() => $"{Address.ToExpressionString(ExpressionType.Dereference)} = {Value.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Compare)]
	public sealed class CompareInstruction:BinaryOperationInstruction
	{
		public sealed override CompareStack StackOperation{get;}

		public CompareInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class CompareStack(CompareInstruction instruction):BinaryOperationStack<CompareInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} = {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} = {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} != {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.LessThan)]
	public sealed class LessThanInstruction:BinaryOperationInstruction
	{
		public sealed override LessThanStack StackOperation{get;}

		public LessThanInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class LessThanStack(LessThanInstruction instruction):BinaryOperationStack<LessThanInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} < {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} < {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} >= {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.GreaterThan)]
	public sealed class GreaterThanInstruction:BinaryOperationInstruction
	{
		public sealed override GreaterThanStack StackOperation{get;}

		public GreaterThanInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class GreaterThanStack(GreaterThanInstruction instruction):BinaryOperationStack<GreaterThanInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} > {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} > {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} <= {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.SetModel)]
	public sealed class SetModelInstruction:SimplePureConsumerInstruction
	{
		public sealed override SetModelStack StackStatement{get;}

		public SetModelInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SetModelStack(SetModelInstruction instruction):SimplePureConsumerStack<SetModelInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Model => Operands[0];

			public override string ToStatement() => $"setmodel {Model.ToExpressionString(ExpressionType.Reference)}";
		}
	}

	[Opcode(InstructionOpcode.Scale)]
	public sealed class ScaleInstruction:SimplePureConsumerInstruction
	{
		public sealed override ScaleStack StackStatement{get;}

		public ScaleInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ScaleStack(ScaleInstruction instruction):SimplePureConsumerStack<ScaleInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Scale => Operands[0];

			public override string ToStatement() => $"scale {Scale.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.ScaleX)]
	public sealed class ScaleXInstruction:SimplePureConsumerInstruction
	{
		public sealed override ScaleXStack StackStatement{get;}

		public ScaleXInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ScaleXStack(ScaleXInstruction instruction):SimplePureConsumerStack<ScaleXInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ScaleX => Operands[0];

			public override string ToStatement() => $"scalex {ScaleX.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.ScaleY)]
	public sealed class ScaleYInstruction:SimplePureConsumerInstruction
	{
		public sealed override ScaleYStack StackStatement{get;}

		public ScaleYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ScaleYStack(ScaleYInstruction instruction):SimplePureConsumerStack<ScaleYInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ScaleY => Operands[0];

			public override string ToStatement() => $"scaley {ScaleY.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.ScaleZ)]
	public sealed class ScaleZInstruction:SimplePureConsumerInstruction
	{
		public sealed override ScaleZStack StackStatement{get;}

		public ScaleZInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ScaleZStack(ScaleZInstruction instruction):SimplePureConsumerStack<ScaleZInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ScaleZ => Operands[0];

			public override string ToStatement() => $"scalez {ScaleZ.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Shadow)]
	public sealed class ShadowInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.ShadowSize)]
	public sealed class ShadowSizeInstruction:SimplePureConsumerInstruction
	{
		public sealed override ShadowSizeStack StackStatement{get;}

		public ShadowSizeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ShadowSizeStack(ShadowSizeInstruction instruction):SimplePureConsumerStack<ShadowSizeInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ShadowSize => Operands[0];

			public override string ToStatement() => $"ShadowSize {ShadowSize.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.ShadowType)]
	public sealed class ShadowTypeInstruction:SimplePureConsumerInstruction
	{
		public sealed override ShadowTypeStack StackStatement{get;}

		public ShadowTypeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ShadowTypeStack(ShadowTypeInstruction instruction):SimplePureConsumerStack<ShadowTypeInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ShadowType => Operands[0];

			public override string ToStatement() => $"ShadowType {ShadowType.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.Hide)]
	public sealed class HideInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Flash)]
	public sealed class FlashInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Trans)]
	public sealed class TransInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MoveUp)]
	public sealed class MoveUpInstruction:BaseMoveInstruction
	{
		public sealed override MoveUpStack StackStatement{get;}

		public MoveUpInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveUpStack(MoveUpInstruction instruction):BaseMoveStack<MoveUpInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveUp {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveDown)]
	public sealed class MoveDownInstruction:BaseMoveInstruction
	{
		public sealed override MoveDownStack StackStatement{get;}

		public MoveDownInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveDownStack(MoveDownInstruction instruction):BaseMoveStack<MoveDownInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveDown {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveForward)]
	public sealed class MoveForwardInstruction:BaseMoveInstruction
	{
		public sealed override MoveForwardStack StackStatement{get;}

		public MoveForwardInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveForwardStack(MoveForwardInstruction instruction):BaseMoveStack<MoveForwardInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveForward {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveBackward)]
	public sealed class MoveBackwardInstruction:BaseMoveInstruction
	{
		public sealed override MoveBackwardStack StackStatement{get;}

		public MoveBackwardInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveBackwardStack(MoveBackwardInstruction instruction):BaseMoveStack<MoveBackwardInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveBackward {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveLeft)]
	public sealed class MoveLeftInstruction:BaseMoveInstruction
	{
		public sealed override MoveLeftStack StackStatement{get;}

		public MoveLeftInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveLeftStack(MoveLeftInstruction instruction):BaseMoveStack<MoveLeftInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveLeft {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveRight)]
	public sealed class MoveRightInstruction:BaseMoveInstruction
	{
		public sealed override MoveRightStack StackStatement{get;}

		public MoveRightInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveRightStack(MoveRightInstruction instruction):BaseMoveStack<MoveRightInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveRight {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnRight)]
	public sealed class TurnRightInstruction:BaseMoveInstruction
	{
		public sealed override TurnRightStack StackStatement{get;}

		public TurnRightInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnRightStack(TurnRightInstruction instruction):BaseMoveStack<TurnRightInstruction>(instruction)
		{
			public override string ToStatement() => $"TurnRight {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnLeft)]
	public sealed class TurnLeftInstruction:BaseMoveInstruction
	{
		public sealed override TurnLeftStack StackStatement{get;}

		public TurnLeftInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnLeftStack(TurnLeftInstruction instruction):BaseMoveStack<TurnLeftInstruction>(instruction)
		{
			public override string ToStatement() => $"TurnLeft {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TiltLeft)]
	public sealed class TiltLeftInstruction:BaseMoveInstruction
	{
		public sealed override TiltLeftStack StackStatement{get;}

		public TiltLeftInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TiltLeftStack(TiltLeftInstruction instruction):BaseMoveStack<TiltLeftInstruction>(instruction)
		{
			public override string ToStatement() => $"TiltLeft {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TiltRight)]
	public sealed class TiltRightInstruction:BaseMoveInstruction
	{
		public sealed override TiltRightStack StackStatement{get;}

		public TiltRightInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TiltRightStack(TiltRightInstruction instruction):BaseMoveStack<TiltRightInstruction>(instruction)
		{
			public override string ToStatement() => $"TiltRight {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TiltForward)]
	public sealed class TiltForwardInstruction:BaseMoveInstruction
	{
		public sealed override TiltForwardStack StackStatement{get;}

		public TiltForwardInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TiltForwardStack(TiltForwardInstruction instruction):BaseMoveStack<TiltForwardInstruction>(instruction)
		{
			public override string ToStatement() => $"TiltForward {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TiltBackward)]
	public sealed class TiltBackwardInstruction:BaseMoveInstruction
	{
		public sealed override TiltBackwardStack StackStatement{get;}

		public TiltBackwardInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TiltBackwardStack(TiltBackwardInstruction instruction):BaseMoveStack<TiltBackwardInstruction>(instruction)
		{
			public override string ToStatement() => $"TiltBackward {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnToPlayerX)]
	public sealed class TurnToPlayerXInstruction:SimplePureConsumerInstruction
	{
		public sealed override TurnToPlayerXStack StackStatement{get;}

		public TurnToPlayerXInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnToPlayerXStack(TurnToPlayerXInstruction instruction):SimplePureConsumerStack<TurnToPlayerXInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override string ToStatement() => $"TurnToPlayerX {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnToPlayerY)]
	public sealed class TurnToPlayerYInstruction:SimplePureConsumerInstruction
	{
		public sealed override TurnToPlayerYStack StackStatement{get;}

		public TurnToPlayerYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnToPlayerYStack(TurnToPlayerYInstruction instruction):SimplePureConsumerStack<TurnToPlayerYInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override string ToStatement() => $"TurnToPlayerY {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnToPlayerXY)]
	public sealed class TurnToPlayerXYInstruction:SimplePureConsumerInstruction
	{
		public sealed override TurnToPlayerXYStack StackStatement{get;}

		public TurnToPlayerXYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnToPlayerXYStack(TurnToPlayerXYInstruction instruction):SimplePureConsumerStack<TurnToPlayerXYInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override string ToStatement() => $"TurnToPlayerXY {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnToX)]
	public sealed class TurnToXInstruction:SimplePureConsumerInstruction
	{
		public sealed override TurnToXStack StackStatement{get;}

		public TurnToXInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnToXStack(TurnToXInstruction instruction):SimplePureConsumerStack<TurnToXInstruction>(instruction)
		{
			public override int PopCount => 4;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];
			public IStackProducer MaxTurnSpeed => Operands[3];

			public override string ToStatement() => $"TurnToX {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}, {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnToY)]
	public sealed class TurnToYInstruction:SimplePureConsumerInstruction
	{
		public sealed override TurnToYStack StackStatement{get;}

		public TurnToYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnToYStack(TurnToYInstruction instruction):SimplePureConsumerStack<TurnToYInstruction>(instruction)
		{
			public override int PopCount => 4;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];
			public IStackProducer MaxTurnSpeed => Operands[3];

			public override string ToStatement() => $"TurnToY {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}, {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TurnToXY)]
	public sealed class TurnToXYInstruction:SimplePureConsumerInstruction
	{
		public sealed override TurnToXYStack StackStatement{get;}

		public TurnToXYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnToXYStack(TurnToXYInstruction instruction):SimplePureConsumerStack<TurnToXYInstruction>(instruction)
		{
			public override int PopCount => 4;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];
			public IStackProducer MaxTurnSpeed => Operands[3];

			public override string ToStatement() => $"TurnToXY {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}, {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Wobble)]
	public sealed class WobbleInstruction:SimplePureConsumerInstruction
	{
		public sealed override WobbleStack StackStatement{get;}

		public WobbleInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class WobbleStack(WobbleInstruction instruction):SimplePureConsumerStack<WobbleInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override string ToStatement() => $"Wobble {Value.ToExpressionString()}";
		}
	}

	[Opcode(InstructionOpcode.ReSetPos)]
	public sealed class ReSetPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetPos)]
	public sealed class SetPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Jump)]
	public sealed class JumpInstruction:BaseJumpInstruction
	{

		public sealed override JumpStack StackStatement{get;}

		public JumpInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class JumpStack(JumpInstruction instruction):BaseJumpStack<JumpInstruction>(instruction)
		{
			public override string ToStatement() => $"goto {Instruction.Destination.AsmLabel.GetLabel()} $ DONE";
		}
	}

	[Opcode(InstructionOpcode.ObjectFall)]
	public sealed class ObjectFallInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Hang)]
	public sealed class HangInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.WPFirst)]
	public sealed class WPFirstInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WPLast)]
	public sealed class WPLastInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WPNext)]
	public sealed class WPNextInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WPPrev)]
	public sealed class WPPrevInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WPDel)]
	public sealed class WPDelInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.WPNew)]
	public sealed class WPNewInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.WPNearest)]
	public sealed class WPNearestInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WPFurthest)]
	public sealed class WPFurthestInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WPTurnToX)]
	public sealed class WPTurnToXInstruction:SimplePureConsumerInstruction
	{
		public sealed override WPTurnToXStack StackStatement{get;}

		public WPTurnToXInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class WPTurnToXStack(WPTurnToXInstruction instruction):SimplePureConsumerStack<WPTurnToXInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override string ToStatement() => $"WPTurnToX {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.WPTurnToY)]
	public sealed class WPTurnToYInstruction:SimplePureConsumerInstruction
	{
		public sealed override WPTurnToYStack StackStatement{get;}

		public WPTurnToYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class WPTurnToYStack(WPTurnToYInstruction instruction):SimplePureConsumerStack<WPTurnToYInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override string ToStatement() => $"WPTurnToY {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.WPTurnToXY)]
	public sealed class WPTurnToXYInstruction:SimplePureConsumerInstruction
	{
		public sealed override WPTurnToXYStack StackStatement{get;}

		public WPTurnToXYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class WPTurnToXYStack(WPTurnToXYInstruction instruction):SimplePureConsumerStack<WPTurnToXYInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override string ToStatement() => $"WPTurnToXY {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.AnimPlay)]
	public sealed class AnimPlayInstruction:SimplePureConsumerInstruction
	{
		public sealed override AnimPlayStack StackStatement{get;}

		public AnimPlayInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class AnimPlayStack(AnimPlayInstruction instruction):SimplePureConsumerStack<AnimPlayInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer AnimationAddr => Operands[0];

			public override string ToStatement() => $"AnimPlay {AnimationAddr.ToExpressionString(ExpressionType.Reference)}";
		}
	}

	[Opcode(InstructionOpcode.AnimStop)]
	public sealed class AnimStopInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.AnimClear)]
	public sealed class AnimClearInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.AnimSetSpeed)]
	public sealed class AnimSetSpeedInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollisionType)]
	public sealed class CollisionTypeInstruction:SimplePureConsumerInstruction
	{
		public sealed override CollisionTypeStack StackStatement{get;}

		public CollisionTypeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CollisionTypeStack(CollisionTypeInstruction instruction):SimplePureConsumerStack<CollisionTypeInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer CollisionType => Operands[0];

			//TODO: This may use custom values for it.
			public override string ToStatement() => $"CollisionType {CollisionType.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.CollRadius)]
	public sealed class CollisionRadiusInstruction:SimplePureConsumerInstruction
	{
		public sealed override CollisionRadiusStack StackStatement{get;}

		public CollisionRadiusInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CollisionRadiusStack(CollisionRadiusInstruction instruction):SimplePureConsumerStack<CollisionRadiusInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer CollRadius => Operands[0];

			public override string ToStatement() => $"CollisionRadius {CollRadius.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.CollHeight)]
	public sealed class CollisionHeightInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollExtent)]
	public sealed class CollisionExtentInstruction:SimplePureConsumerInstruction
	{
		public sealed override CollisionExtentStack StackStatement{get;}

		public CollisionExtentInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CollisionExtentStack(CollisionExtentInstruction instruction):SimplePureConsumerStack<CollisionExtentInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer CollExtent => Operands[0];

			public override string ToStatement() => $"CollisionExtent {CollExtent.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.CollView)]
	public sealed class CollisionViewInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollPoints)]
	public sealed class CollisionPointsInstruction:SimplePureConsumerInstruction
	{
		public sealed override CollisionPointsStack StackStatement{get;}

		public CollisionPointsInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CollisionPointsStack(CollisionPointsInstruction instruction):SimplePureConsumerStack<CollisionPointsInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override string ToStatement() => $"CollisionPoints {Value.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.CollSetPoint)]
	public sealed class CollSetPointInstruction:SimplePureConsumerInstruction
	{
		public sealed override CollSetPointStack StackStatement{get;}

		public CollSetPointInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CollSetPointStack(CollSetPointInstruction instruction):SimplePureConsumerStack<CollSetPointInstruction>(instruction)
		{
			public override int PopCount => 4;

			public IStackProducer PointIndex => Operands[0];
			public IStackProducer X => Operands[1];
			public IStackProducer Y => Operands[2];
			public IStackProducer Z => Operands[3];

			public override string ToStatement() => $"CollisionSetPoint {PointIndex.ToFxString()}, {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.CreateTrigger)]
	public sealed class CreateTriggerInstruction:SimpleNoStackInstruction
	{
		public TriggerType Type;
		public int Arg;
		public Instruction TriggerProc;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var triggerCreateAsmInstr = GetAsmInstruction<Disassembler.TriggerCreateInstruction>();
			Type = triggerCreateAsmInstr.Type;
			Arg = triggerCreateAsmInstr.Arg;
			TriggerProc = parser.GetTrigger(triggerCreateAsmInstr.Stream, this);
		}

		public sealed override CreateTriggerStack StackStatement{get;}

		public CreateTriggerInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CreateTriggerStack(CreateTriggerInstruction instruction):SimpleNoStackStack<CreateTriggerInstruction>(instruction)
		{
			public override string ToStatement()
			{
				var ret = new StringBuilder();
				ret.Append($"CreateTrigger {Instruction.Type} ");
				switch(Instruction.Type)
				{
					case TriggerType.Every or TriggerType.In://Frame Count
					case TriggerType.Anim://Animation Number
					case TriggerType.WhenNear or TriggerType.WhenFar://Distance
						ret.Append(Instruction.Arg + " ");
						break;
				}
				ret.Append(Instruction.TriggerProc.AsmLabel.SubroutineName());
				return ret.ToString();
			}
		}
	}

	[Opcode(InstructionOpcode.KillTrigger)]
	public sealed class KillTriggerInstruction:TriggerUpdateInstruction
	{
		public sealed override KillTriggerStack StackStatement{get;}

		public KillTriggerInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class KillTriggerStack(KillTriggerInstruction instruction):TriggerUpdateStack<KillTriggerInstruction>(instruction)
		{
			public override string ToStatement() => $"KillTrigger {Instruction.TriggerProc.AsmLabel.SubroutineName()}";
		}
	}

	[Opcode(InstructionOpcode.HoldTriggers)]
	public sealed class HoldTriggersInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ReleaseTriggers)]
	public sealed class ReleaseTriggersInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.HoldTrigger)]
	public sealed class HoldTriggerInstruction:TriggerUpdateInstruction
	{
		public sealed override HoldTriggerStack StackStatement{get;}

		public HoldTriggerInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class HoldTriggerStack(HoldTriggerInstruction instruction):TriggerUpdateStack<HoldTriggerInstruction>(instruction)
		{
			public override string ToStatement() => $"HoldTrigger {Instruction.TriggerProc.AsmLabel.SubroutineName()}";
		}
	}

	[Opcode(InstructionOpcode.ReleaseTrigger)]
	public sealed class ReleaseTriggerInstruction:TriggerUpdateInstruction
	{
		public sealed override ReleaseTriggerStack StackStatement{get;}

		public ReleaseTriggerInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ReleaseTriggerStack(ReleaseTriggerInstruction instruction):TriggerUpdateStack<ReleaseTriggerInstruction>(instruction)
		{
			public override string ToStatement() => $"ReleaseTrigger {Instruction.TriggerProc.AsmLabel.SubroutineName()}";
		}
	}

	[Opcode(InstructionOpcode.Wait)]
	public sealed class WaitInstruction:SimplePureConsumerInstruction
	{
		public sealed override WaitStack StackStatement{get;}

		public WaitInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class WaitStack(WaitInstruction instruction):SimplePureConsumerStack<WaitInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override string ToStatement() => $"Wait {Value.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Hold)]
	public sealed class HoldInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Release)]
	public sealed class ReleaseInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Remove)]
	public sealed class RemoveInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.MapRemove)]
	public sealed class MapRemoveInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.MapAdd)]
	public sealed class MapAddInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MapReplace)]
	public sealed class MapReplaceInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Activated)]
	public sealed class ActivatedInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Collected)]
	public sealed class CollectedInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Spawn)]
	public sealed class SpawnInstruction:BaseSpawnInstruction
	{
		public sealed override SpawnStack StackStatement{get;}

		public SpawnInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SpawnStack(SpawnInstruction instruction):BaseSpawnStack<SpawnInstruction>(instruction)
		{
			public override string ToStatement() => $"Spawn {Instruction.SpawnStratProc.AsmLabel.SubroutineName()}, {Instruction.LocalVarsToPop}, {Instruction.LocalCount}, {Instruction.TriggerCount}, {Instruction.CollisionSize}, {Instruction.CollisionBoneCount}";
		}
	}

	[Opcode(InstructionOpcode.SpawnFrom)]
	public sealed class SpawnFromInstruction:BaseSpawnInstruction
	{
		public int BoneToSpawnFrom;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);

			var spawnFromAsmInstr = GetAsmInstruction<Disassembler.SpawnFromInstruction>();
			BoneToSpawnFrom = spawnFromAsmInstr.BoneToSpawnFrom;
		}

		public sealed override SpawnFromStack StackStatement{get;}

		public SpawnFromInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SpawnFromStack(SpawnFromInstruction instruction):BaseSpawnStack<SpawnFromInstruction>(instruction)
		{
			public override string ToStatement() => $"SpawnFrom {Instruction.SpawnStratProc.AsmLabel.SubroutineName()}, {Instruction.LocalVarsToPop}, {Instruction.LocalCount}, {Instruction.TriggerCount}, {Instruction.CollisionSize}, {Instruction.CollisionBoneCount}, {Instruction.BoneToSpawnFrom}";
		}
	}

	[Opcode(InstructionOpcode.Link)]
	public sealed class LinkInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Unlink)]
	public sealed class UnlinkInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SoundShift)]
	public sealed class SoundShiftInstruction:SimplePureConsumerInstruction
	{
		public sealed override SoundShiftStack StackStatement{get;}

		public SoundShiftInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundShiftStack(SoundShiftInstruction instruction):SimplePureConsumerStack<SoundShiftInstruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer Channel => Operands[0];
			public IStackProducer Pitch => Operands[1];

			public override string ToStatement() => $"SoundShift {Channel.ToIntStr()}, {Pitch.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SoundStop)]
	public sealed class SoundStopInstruction:SimplePureConsumerInstruction
	{
		public sealed override SoundStopStack StackStatement{get;}

		public SoundStopInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundStopStack(SoundStopInstruction instruction):SimplePureConsumerStack<SoundStopInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Channel => Operands[0];

			public override string ToStatement() => $"SoundStop {Channel.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.CdPlay)]
	public sealed class CdPlayInstruction:SimplePureConsumerInstruction
	{
		public sealed override CdPlayStack StackStatement{get;}

		public CdPlayInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CdPlayStack(CdPlayInstruction instruction):SimplePureConsumerStack<CdPlayInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MusicId => Operands[0];

			public override string ToStatement() => $"CdPlay {MusicId.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MidiLoop)]
	public sealed class MidiLoopInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MidiVolume)]
	public sealed class MidiVolumeInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CdFade)]
	public sealed class CdFadeInstruction:SimplePureConsumerInstruction
	{
		public sealed override CdFadeStack StackStatement{get;}

		public CdFadeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CdFadeStack(CdFadeInstruction instruction):SimplePureConsumerStack<CdFadeInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer TimeLength => Operands[0];

			public override string ToStatement() => $"CdFade {TimeLength.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MidiStop)]
	public sealed class MidiStopInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MidiQueue)]
	public sealed class MidiQueueInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.IsLight)]
	public sealed class IsLightInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.LightCol)]
	public sealed class LightColInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.LightFade)]
	public sealed class LightFadeInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.LightAtten)]
	public sealed class LightAttenInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.LightType)]
	public sealed class LightTypeInstruction:UnimplementedInstruction;

	public abstract class BaseCollisionInstruction:SimpleNoStackInstruction
	{
		public readonly bool NewState;
		public uint CollisionFlag;

		public sealed override BaseCollisionStack StackStatement{get;}

		public BaseCollisionInstruction(bool newState)
		{
			NewState = newState;
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var collisionFlagAsmInstr = GetAsmInstruction<Disassembler.CollisionFlagInstruction>();
			CollisionFlag = collisionFlagAsmInstr.CollisionType;
		}

		public sealed class BaseCollisionStack(BaseCollisionInstruction instruction):SimpleNoStackStack<BaseCollisionInstruction>(instruction)
		{
			public override string ToStatement() => $"collision {(Instruction.NewState ? "on" : "off")} {Instruction.CollisionFlag}";
		}
	}

	[Opcode(InstructionOpcode.CollisionOn)]
	public sealed class CollisionOnInstruction():BaseCollisionInstruction(true);

	[Opcode(InstructionOpcode.CollisionOff)]
	public sealed class CollisionOffInstruction():BaseCollisionInstruction(false);

	[Opcode(InstructionOpcode.CollisionOffAll)]
	public sealed class CollisionOffAllInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SoundPlay3)]
	public sealed class SoundPlay3Instruction:SimplePureConsumerInstruction
	{
		public sealed override SoundPlay3Stack StackStatement{get;}

		public SoundPlay3Instruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundPlay3Stack(SoundPlay3Instruction instruction):SimplePureConsumerStack<SoundPlay3Instruction>(instruction)
		{
			public override int PopCount => 3;

			public IStackProducer SoundIndex => Operands[0];
			public IStackProducer Volume => Operands[1];
			public IStackProducer Flags => Operands[2];

			public override string ToStatement() => $"SoundPlay {SoundIndex.ToIntStr()}, {Volume.ToFxString()}, {Flags.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SoundPlay4)]
	public sealed class SoundPlay4Instruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SoundPlay3ASS)]
	public sealed class SoundPlay3AssignmentInstruction:BaseExpressionInstruction
	{
		public sealed override SoundPlay3AssignmentStack StackOperation{get;}

		public SoundPlay3AssignmentInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SoundPlay3AssignmentStack(SoundPlay3AssignmentInstruction instruction):BaseExpressionStack<SoundPlay3AssignmentInstruction>(instruction)
		{
			public override int PopCount => 3;

			public IStackProducer SoundIndex => Operands[0];
			public IStackProducer Volume => Operands[1];
			public IStackProducer Flags => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToIntStr()}, {Volume.ToFxString()}, {Flags.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SoundPlay4ASS)]
	public sealed class SoundPlay4AssignmentInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Int)]
	public sealed class IntInstruction:UnaryOperationInstruction
	{
		public sealed override IntStack StackOperation{get;}

		public IntInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class IntStack(IntInstruction instruction):UnaryOperationStack<IntInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"int({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Sin)]
	public sealed class SinInstruction:UnaryOperationInstruction
	{
		public sealed override SinStack StackOperation{get;}

		public SinInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SinStack(SinInstruction instruction):UnaryOperationStack<SinInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"sin({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Cos)]
	public sealed class CosInstruction:UnaryOperationInstruction
	{
		public sealed override CosStack StackOperation{get;}

		public CosInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class CosStack(CosInstruction instruction):UnaryOperationStack<CosInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"cos({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Not)]
	public sealed class NotInstruction:UnaryOperationInstruction
	{
		public sealed override NotStack StackOperation{get;}

		public NotInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class NotStack(NotInstruction instruction):UnaryOperationStack<NotInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"NOT {Value.ToExpressionString()}";
		}
	}

	[Opcode(InstructionOpcode.Pop)]
	public sealed class PopInstruction:SimplePureConsumerInstruction
	{
		public sealed override PopStack StackStatement{get;}

		public PopInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class PopStack(PopInstruction instruction):SimplePureConsumerStack<PopInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Discarded => Operands[0];

			public override string ToStatement() => $"Pop {Discarded.ToExpressionString()}";
		}
	}

	[Opcode(InstructionOpcode.Address)]
	public sealed class AddressInstruction:PureProducerInstruction
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

		public sealed override AddressStack StackProducer{get;}

		public AddressInstruction()
		{
			StackProducer = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var addressAsmInstr = GetAsmInstruction<Disassembler.AddressInstruction>();

			if(addressAsmInstr.IsAnimLoad)
			{
				IsAnimLoad = true;
				AnimationIndex = addressAsmInstr.AnimationIndex;
			}
			else if(addressAsmInstr.IsDataLoad)
			{
				IsDataLoad = true;
				DataOffset = addressAsmInstr.DataOffset;
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public sealed class AddressStack(AddressInstruction instruction):PureProducerStack<AddressInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown)
			{
				if(Instruction.IsAnimLoad)
				{
					return $"animload Anim_{Instruction.AnimationIndex}";
				}
				else if(Instruction.IsDataLoad)
				{
					return $"Address Offset {Instruction.DataOffset}";
				}
				else
				{
					throw new Exception();
				}
			}
		}
	}

	[Opcode(InstructionOpcode.Jsr)]
	public sealed class JsrInstruction:BaseJumpSubroutineInstruction
	{
		public sealed override JsrStack StackStatement{get;}

		public JsrInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class JsrStack(JsrInstruction instruction):BaseJumpSubroutineStack<JsrInstruction>(instruction)
		{
			public override string ToStatement() => $"proc {Instruction.Proc.AsmLabel.SubroutineName()} $ DONE";
		}
	}

	[Opcode(InstructionOpcode.JsrImm)]
	public sealed class JsrImmInstruction:BaseJumpSubroutineInstruction
	{
		public sealed override JsrImmStack StackStatement{get;}

		public JsrImmInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class JsrImmStack(JsrImmInstruction instruction):BaseJumpSubroutineStack<JsrImmInstruction>(instruction)
		{
			public override string ToStatement() => $"proc {Instruction.Proc.AsmLabel.SubroutineName()} $ IMM";
		}
	}

	[Opcode(InstructionOpcode.Return)]
	public sealed class ReturnInstruction:NoStackInstruction
	{
		//Technically pops a value but we aren't counting it because it is pushed outisde the subroutine/trigger.

		public sealed override bool Terminal => true;

		public sealed override ReturnStack StackStatement{get;}
		public sealed override ReturnFlow FlowStatement{get;}

		public ReturnInstruction()
		{
			StackStatement = new(this);
			FlowStatement = new(this);
		}

		public sealed class ReturnStack(ReturnInstruction instruction):NoStackStack<ReturnInstruction>(instruction)
		{
			public override string ToStatement() => "Return";
		}

		public sealed class ReturnFlow(ReturnInstruction instruction):NoStackFlow<ReturnInstruction>(instruction),IFlowTerminal
		{
			public override void Analyze(FlowAnalyzer flow){}
		}
	}

	[Opcode(InstructionOpcode.Beq)]
	public sealed class BeqInstruction:BranchInstruction
	{
		public sealed override BeqStack StackStatement{get;}

		public BeqInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class BeqStack(BeqInstruction instruction):BranchStack<BeqInstruction>(instruction)
		{
			//Branch if equal to zero
			public override string ToStatement() => $"if {Condition.ToConditionStr(false)} then goto {Instruction.ConditionalDest.AsmLabel.GetLabel()} endif $ DONE";
		}
	}

	[Opcode(InstructionOpcode.Bne)]
	public sealed class BneInstruction:BranchInstruction
	{
		public sealed override BneStack StackStatement{get;}

		public BneInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class BneStack(BneInstruction instruction):BranchStack<BneInstruction>(instruction)
		{
			//Branch if not equal to zero
			public override string ToStatement() => $"if {Condition.ToConditionStr(true)} then goto {Instruction.ConditionalDest.AsmLabel.GetLabel()} endif $ DONE";
		}
	}

	[Opcode(InstructionOpcode.BeqImm)]
	public sealed class BeqImmInstruction:BranchInstruction
	{
		public sealed override BeqImmStack StackStatement{get;}

		public BeqImmInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class BeqImmStack(BeqImmInstruction instruction):BranchStack<BeqImmInstruction>(instruction)
		{
			//Branch if equal to zero
			public override string ToStatement() => $"if {Condition.ToConditionStr(false)} then goto {Instruction.ConditionalDest.AsmLabel.GetLabel()} endif $ IMM";
		}
	}

	[Opcode(InstructionOpcode.BneImm)]
	public sealed class BneImmInstruction:BranchInstruction
	{
		public sealed override BneImmStack StackStatement{get;}

		public BneImmInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class BneImmStack(BneImmInstruction instruction):BranchStack<BneImmInstruction>(instruction)
		{
			//Branch if not equal to zero
			public override string ToStatement() => $"if {Condition.ToConditionStr(true)} then goto {Instruction.ConditionalDest.AsmLabel.GetLabel()} endif $ IMM";
		}
	}

	[Opcode(InstructionOpcode.JumpImm)]
	public sealed class JumpImmInstruction:BaseJumpInstruction
	{
		public sealed override JumpImmStack StackStatement{get;}

		public JumpImmInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class JumpImmStack(JumpImmInstruction instruction):BaseJumpStack<JumpImmInstruction>(instruction)
		{
			public override string ToStatement() => $"goto {Instruction.Destination.AsmLabel.GetLabel()} $ IMM";
		}
	}

	[Opcode(InstructionOpcode.EndStrat)]
	public sealed class EndStratInstruction:NoStackInstruction
	{
		public sealed override bool Terminal => true;

		public sealed override EndStratStack StackStatement{get;}
		public sealed override EndStratFlow FlowStatement{get;}

		public EndStratInstruction()
		{
			StackStatement = new(this);
			FlowStatement = new(this);
		}

		public sealed class EndStratStack(EndStratInstruction instruction):NoStackStack<EndStratInstruction>(instruction)
		{
			public override string ToStatement() => "EndStrat";
		}

		public sealed class EndStratFlow(EndStratInstruction instruction):NoStackFlow<EndStratInstruction>(instruction),IFlowTerminal
		{
			public override void Analyze(FlowAnalyzer flow){}
		}
	}

	[Opcode(InstructionOpcode.IsPlayer)]
	public sealed class IsPlayerInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.And)]
	public sealed class AndInstruction:BinaryOperationInstruction
	{
		public sealed override AndStack StackOperation{get;}

		public AndInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class AndStack(AndInstruction instruction):BinaryOperationStack<AndInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToExpressionString()} AND {ValueB.ToExpressionString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToConditionStr(true)} AND {ValueB.ToConditionStr(true)})" : $"({ValueA.ToConditionStr(false)} OR {ValueB.ToConditionStr(false)})";
		}
	}

	[Opcode(InstructionOpcode.Or)]
	public sealed class OrInstruction:BinaryOperationInstruction
	{
		//TODO: This is bitwise |, not logical ||.

		public sealed override OrStack StackOperation{get;}

		public OrInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class OrStack(OrInstruction instruction):BinaryOperationStack<OrInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToExpressionString()} OR {ValueB.ToExpressionString()})";
		}
	}

	[Opcode(InstructionOpcode.Index_Jump)]
	public sealed class IndexJumpInstruction:PureConsumerInstruction
	{
		public (int[] Comparands,Instruction Destination)[] Cases;

		public sealed override SwitchStack StackStatement{get;}
		public sealed override SwitchFlow FlowStatement{get;}

		public IndexJumpInstruction()
		{
			StackStatement = new(this);
			FlowStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var indexJumpAsmInstr = GetAsmInstruction<Disassembler.IndexJumpInstruction>();

			var cases = new List<(List<int> comparands, Instruction destination)>();
			for(int i=0; i<indexJumpAsmInstr.CaseCount; i++)
			{
				var comparand = indexJumpAsmInstr.CaseComparands[i];
				var destination = parser.GetInstruction(indexJumpAsmInstr.CaseDestinations[i], null, this);
				if(cases.Count>0 && destination == cases[^1].destination)
				{
					cases[^1].comparands.Add(comparand);
				}
				else
				{
					cases.Add((new List<int>{comparand}, destination));
				}
			}
			Cases = cases.Select(c => (c.comparands.ToArray(), c.destination)).ToArray();

			if(Cases.Length == 0)
			{
				throw new Exception("Empty Switches are not supported and will break the FlowAnalyzer");
			}
		}

		public sealed class SwitchStack(IndexJumpInstruction instruction):PureConsumerStack<IndexJumpInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override void Analyze(StackAnalyzer stack)
			{
				base.Analyze(stack);

				foreach(var c in Instruction.Cases)
				{
					stack.AddDest(c.Destination);
				}
			}

			public override string ToStatement()
			{
				//TODO: This was commented out. Might fail.

				var ret = new StringBuilder();
				ret.AppendLine($"switch {Value.ToExpressionString()}");
				foreach(var c in Instruction.Cases)
				{
					foreach(var comparand in c.Comparands)
					{
						ret.AppendLine($"\tcase {comparand}");
					}
					ret.AppendLine($"\t\tgoto {c.Destination.AsmLabel.GetLabel()}");
					ret.AppendLine($"\tendcase");
				}
				ret.AppendLine("endswitch");

				return ret.ToString();
			}
		}

		public sealed class SwitchFlow(IndexJumpInstruction instruction):PureConsumerFlow<IndexJumpInstruction>(instruction),IFlowControl
		{
			public IStackStatement ControlStatement => Instruction.StackStatement;

			public IReadOnlyList<FlowStatement> FlowCaseDestinations => FlowDestinations;

			public override void Analyze(FlowAnalyzer flow)
			{
				for(int i=0; i<Instruction.Cases.Length; i++)
				{
					flow.AddDest(this, Instruction.Cases[i].Destination);
				}
			}
		}
	}

	[Opcode(InstructionOpcode.BitwiseAnd)]
	public sealed class BitwiseAndInstruction:BinaryOperationInstruction
	{
		public sealed override BitwiseAndStack StackOperation{get;}

		public BitwiseAndInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class BitwiseAndStack(BitwiseAndInstruction instruction):BinaryOperationStack<BitwiseAndInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToIntStr()} & {ValueB.ToIntStr()})";
		}
	}

	[Opcode(InstructionOpcode.Ext_Local)]
	public sealed class Ext_LocalInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Parent;
		public sealed override VarType Type => VarType.Local;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Ext_LocalAddress)]
	public sealed class Ext_LocalAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Parent;
		public sealed override VarType Type => VarType.Local;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Ext_Global)]
	public sealed class Ext_GlobalInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.Ext_GlobalAddress)]
	public sealed class Ext_GlobalAddressInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.ObjectJump)]
	public sealed class ObjectJumpInstruction:SimplePureConsumerInstruction
	{
		public sealed override ObjectJumpStack StackStatement{get;}

		public ObjectJumpInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ObjectJumpStack(ObjectJumpInstruction instruction):SimplePureConsumerStack<ObjectJumpInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer VerticalVelocity => Operands[0];

			public override string ToStatement() => $"ObjectJump {VerticalVelocity.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Ext_AlienVar)]
	public sealed class Ext_AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Parent;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Ext_AlienVarAddress)]
	public sealed class Ext_AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Parent;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.NotEqual)]
	public sealed class NotEqualInstruction:BinaryOperationInstruction
	{
		public sealed override NotEqualStack StackOperation{get;}

		public NotEqualInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class NotEqualStack(NotEqualInstruction instruction):BinaryOperationStack<NotEqualInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} != {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} != {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} = {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.ShiftLeft)]
	public sealed class ShiftLeftInstruction:BinaryOperationInstruction
	{
		public sealed override ShiftLeftStack StackOperation{get;}

		public ShiftLeftInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class ShiftLeftStack(ShiftLeftInstruction instruction):BinaryOperationStack<ShiftLeftInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} << {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.ShiftRight)]
	public sealed class ShiftRightInstruction:BinaryOperationInstruction
	{
		public sealed override ShiftRightStack StackOperation{get;}

		public ShiftRightInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class ShiftRightStack(ShiftRightInstruction instruction):BinaryOperationStack<ShiftRightInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} >> {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.AnimAdvance)]
	public sealed class AnimAdvanceInstruction:SimplePureConsumerInstruction
	{
		public sealed override AnimAdvanceStack StackStatement{get;}

		public AnimAdvanceInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class AnimAdvanceStack(AnimAdvanceInstruction instruction):SimplePureConsumerStack<AnimAdvanceInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer DesiredIndex => Operands[0];

			public override string ToStatement() => $"AnimAdvance {DesiredIndex.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.GreaterEqual)]
	public sealed class GreaterEqualInstruction:BinaryOperationInstruction
	{
		public sealed override GreaterEqualStack StackOperation{get;}

		public GreaterEqualInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class GreaterEqualStack(GreaterEqualInstruction instruction):BinaryOperationStack<GreaterEqualInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} >= {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} >= {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} < {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.LessEqual)]
	public sealed class LessEqualInstruction:BinaryOperationInstruction
	{
		public sealed override LessEqualStack StackOperation{get;}

		public LessEqualInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class LessEqualStack(LessEqualInstruction instruction):BinaryOperationStack<LessEqualInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} <= {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} <= {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} > {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Rnd)]
	public sealed class RndInstruction:BaseExpressionInstruction
	{
		public sealed override RndStack StackOperation{get;}

		public RndInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class RndStack(RndInstruction instruction):BaseExpressionStack<RndInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxExclusive => Operands[0];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"rnd({MaxExclusive.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Blink)]
	public sealed class BlinkInstruction:SimplePureConsumerInstruction
	{
		public int Count;

		public sealed override BlinkStack StackStatement{get;}

		public BlinkInstruction()
		{
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var blinkAsmInstr = GetAsmInstruction<Disassembler.BlinkInstruction>();

			Count = blinkAsmInstr.Count;
		}

		public sealed class BlinkStack(BlinkInstruction instruction):SimplePureConsumerStack<BlinkInstruction>(instruction)
		{
			public override int PopCount => Instruction.Count;

			//This is just the operands but in reverse
			//TODO: Cache this
			public IStackProducer[] Bones => Operands.Reverse().ToArray();

			public override string ToStatement() => $"blink {string.Join(", ", Bones.Select(b => b.ToFxString()))}";
		}
	}

	[Opcode(InstructionOpcode.LoseHeart)]
	public sealed class LoseHeartInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ResetToCheckPoint)]
	public sealed class ResetToCheckPointInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ForceCollision)]
	public sealed class ForceCollisionInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.TurnFromPlayerY)]
	public sealed class TurnFromPlayerYInstruction:SimplePureConsumerInstruction
	{
		public sealed override TurnFromPlayerYStack StackStatement{get;}

		public TurnFromPlayerYInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TurnFromPlayerYStack(TurnFromPlayerYInstruction instruction):SimplePureConsumerStack<TurnFromPlayerYInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override string ToStatement() => $"TurnFromPlayerY {MaxTurnSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.PlayerAttack)]
	public sealed class PlayerAttackInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Rumble)]
	public sealed class RumbleInstruction:SimplePureConsumerInstruction
	{
		public sealed override RumbleStack StackStatement{get;}

		public RumbleInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class RumbleStack(RumbleInstruction instruction):SimplePureConsumerStack<RumbleInstruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer Rumble => Operands[0];
			public IStackProducer RumbleDecay => Operands[1];

			public override string ToStatement() => $"Rumble {Rumble.ToFxString()}, {RumbleDecay.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Vibrate)]
	public sealed class VibrateInstruction:SimplePureConsumerInstruction
	{
		public sealed override VibrateStack StackStatement{get;}

		public VibrateInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class VibrateStack(VibrateInstruction instruction):SimplePureConsumerStack<VibrateInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Vibrate => Operands[0];

			public override string ToStatement() => $"Vibrate {Vibrate.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SuspendIfTooFar)]
	public sealed class SuspendIfTooFarInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.CollisionBone)]
	public sealed class CollisionBoneInstruction:SimplePureConsumerInstruction
	{
		public sealed override CollisionBoneStack StackStatement{get;}

		public CollisionBoneInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CollisionBoneStack(CollisionBoneInstruction instruction):SimplePureConsumerStack<CollisionBoneInstruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer Bone => Operands[0];
			public IStackProducer Radius => Operands[1];

			public override string ToStatement() => $"CollisionBone {Bone.ToFxString()}, {Radius.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.UseBone)]
	public sealed class UseBoneInstruction:SimplePureConsumerInstruction
	{
		public sealed override UseBoneStack StackStatement{get;}

		public UseBoneInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class UseBoneStack(UseBoneInstruction instruction):SimplePureConsumerStack<UseBoneInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Frame => Operands[0];

			public override string ToStatement() => $"UseBone {Frame.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.IsCamera)]
	public sealed class IsCameraInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.LookAtMe)]
	public sealed class LookAtMeInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.LookAtMe2)]
	public sealed class LookAtMe2Instruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.PushCamera)]
	public sealed class PushCameraInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.PopCamera)]
	public sealed class PopCameraInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ResetCameraPos)]
	public sealed class ResetCameraPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GainHeart)]
	public sealed class GainHeartInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GainHeartPot)]
	public sealed class GainHeartPotInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.AddInv)]
	public sealed class AddInvInstruction:ItemChangeInstruction
	{
		public sealed override int Change => +1;
	}

	[Opcode(InstructionOpcode.GainCrystal)]
	public sealed class GainCrystalInstruction:SimplePureConsumerInstruction
	{
		public sealed override GainCrystalStack StackStatement{get;}

		public GainCrystalInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class GainCrystalStack(GainCrystalInstruction instruction):SimplePureConsumerStack<GainCrystalInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer CrystalType => Operands[0];

			public override string ToStatement() => $"GainCrystal {CrystalType.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Cutscene)]
	public sealed class CutsceneInstruction:SimplePureConsumerInstruction
	{
		public sealed override CutsceneStack StackStatement{get;}

		public CutsceneInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CutsceneStack(CutsceneInstruction instruction):SimplePureConsumerStack<CutsceneInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer CutsceneAddr => Operands[0];

			public override string ToStatement() => $"Cutscene {CutsceneAddr.ToExpressionString(ExpressionType.Reference)}";
		}
	}

	[Opcode(InstructionOpcode.Inventory)]
	public sealed class InventoryInstruction:PureProducerInstruction
	{
		//Item count
		public int Item;

		public sealed override InventoryStack StackProducer{get;}

		public InventoryInstruction()
		{
			StackProducer = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var inventoryAsmInstr = GetAsmInstruction<Disassembler.ItemCountInstruction>();

			Item = inventoryAsmInstr.Item;
		}

		public sealed class InventoryStack(InventoryInstruction instruction):PureProducerStack<InventoryInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"inventory({Instruction.Item})";
		}
	}

	[Opcode(InstructionOpcode.DebugName)]
	public sealed class DebugNameInstruction:SimpleNoStackInstruction
	{
		public string Name;

		public sealed override DebugNameStack StackStatement{get;}

		public DebugNameInstruction()
		{
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var debugNameAsmInstr = GetAsmInstruction<Disassembler.DebugNameInstruction>();

			Name = debugNameAsmInstr.Name;
		}

		public sealed class DebugNameStack(DebugNameInstruction instruction):SimpleNoStackStack<DebugNameInstruction>(instruction)
		{
			public override string ToStatement() => $"DebugName \"{Instruction.Name}\"";
		}
	}

	[Opcode(InstructionOpcode.PlayerDistanceCheck)]
	public sealed class PlayerDistanceCheckInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.SoundPlay1)]
	public sealed class SoundPlay1Instruction:SimplePureConsumerInstruction
	{
		public sealed override SoundPlay1Stack StackStatement{get;}

		public SoundPlay1Instruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundPlay1Stack(SoundPlay1Instruction instruction):SimplePureConsumerStack<SoundPlay1Instruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer SoundIndex => Operands[0];

			public override string ToStatement() => $"SoundPlay {SoundIndex.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.SoundPlay1ASS)]
	public sealed class SoundPlay1AssignmentInstruction:BaseExpressionInstruction
	{
		public sealed override SoundPlay1AssignmentStack StackOperation{get;}

		public SoundPlay1AssignmentInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SoundPlay1AssignmentStack(SoundPlay1AssignmentInstruction instruction):BaseExpressionStack<SoundPlay1AssignmentInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer SoundIndex => Operands[0];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.SoundAddress)]
	public sealed class SoundAddressInstruction:PureProducerInstruction
	{
		public int Value;

		public sealed override SoundAddressStack StackProducer{get;}

		public SoundAddressInstruction()
		{
			StackProducer = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var soundAddrAsmInstr = GetAsmInstruction<Disassembler.SoundAddressInstruction>();

			Value = soundAddrAsmInstr.Value;
		}
		

		public sealed class SoundAddressStack(SoundAddressInstruction instruction):PureProducerStack<SoundAddressInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundAddress {Instruction.Value}";
		}
	}

	[Opcode(InstructionOpcode.OnGround)]
	public sealed class OnGroundInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.ObjectFallSlow)]
	public sealed class ObjectFallSlowInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Player_AlienVar)]
	public sealed class Player_AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Player;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Player_AlienVarAddress)]
	public sealed class Player_AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Player;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.CollisionOffset)]
	public sealed class CollisionOffsetInstruction:SimplePureConsumerInstruction
	{
		public sealed override CollisionOffsetStack StackStatement{get;}

		public CollisionOffsetInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CollisionOffsetStack(CollisionOffsetInstruction instruction):SimplePureConsumerStack<CollisionOffsetInstruction>(instruction)
		{
			public override int PopCount => 3;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];

			public override string ToStatement() => $"CollisionOffset {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Abs)]
	public sealed class AbsInstruction:UnaryOperationInstruction
	{
		public sealed override AbsStack StackOperation{get;}

		public AbsInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class AbsStack(AbsInstruction instruction):UnaryOperationStack<AbsInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"abs({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Pickup)]
	public sealed class PickupInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Min)]
	public sealed class MinInstruction:BinaryOperationInstruction
	{
		public sealed override MinStack StackOperation{get;}

		public MinInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class MinStack(MinInstruction instruction):BinaryOperationStack<MinInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"min({ValueA.ToFxString()}, {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Max)]
	public sealed class MaxInstruction:BinaryOperationInstruction
	{
		public sealed override MaxStack StackOperation{get;}

		public MaxInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class MaxStack(MaxInstruction instruction):BinaryOperationStack<MaxInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"max({ValueA.ToFxString()}, {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.SpawnParticle)]
	public sealed class SpawnParticleInstruction:SimplePureConsumerInstruction
	{
		public enum ParticleType
		{
			particle_dot = 1,
			particle_sprite = 2,
			particle_bubble = 3,
			particle_zapper = 4,
			particle_froth = 5,
			particle_ripple = 6,
			particle_snow = 7,
			particle_rain = 8,
			particle_spark = 9,
			particle_smoke = 10,
			particle_arcdot = 11,
			particle_triangle = 12,
			particle_vectsmoke = 13,
			particle_upsmoke = 14,
			particle_vectsparkle = 15,
			particle_vectfallsparkle = 16,
			particle_fire = 17,
			particle_vectsprite = 18,
			particle_vectfallsprite = 19,
			particle_snowflurry = 20,
			particle_leaf = 21,
			particle_trailsprite = 22,
			particle_vectfadesprite = 23,
			particle_vectfadefallsprite = 24,
			particle_scale = 25,
			particle_footstep_1 = 26,
			particle_loader = 27,
			particle_animator = 28,
			particle_arcdotwater = 29,
			particle_colorize = 30,
			particle_vertsprite = 31,
			particle_shockwave = 32,	
			particle_line = 33,
			particle_linebounce = 34,
			particle_vectsparklespin = 35,
			particle_vectfallsparklespin = 36,
			particle_firespin = 37,
			particle_vectspritespin = 38,
			particle_vectfallspritespin = 39,
			particle_trailspritespin = 40,
			particle_vectfadespritespin = 41,
			particle_vectfadefallspritespin = 42,
		}

		public sealed override SpawnParticleStack StackStatement{get;}

		public SpawnParticleInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SpawnParticleStack(SpawnParticleInstruction instruction):SimplePureConsumerStack<SpawnParticleInstruction>(instruction)
		{
			public override int PopCount => 6;

			public IStackProducer Type => Operands[0];
			public IStackProducer Count => Operands[1];
			public IStackProducer X => Operands[2];
			public IStackProducer Y => Operands[3];
			public IStackProducer Z => Operands[4];
			public IStackProducer Arg6 => Operands[5];

			public override string ToStatement()
			{
				string typeStr;
				if(Type.OperationInstruction is NumberInstruction typeNum)
				{
					typeStr = ((ParticleType)typeNum.Value).ToString();
				}
				else
				{
					typeStr = Type.ToIntStr();
				}
				return $"SpawnParticle {typeStr}, {Count.ToFxString()}, {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}, {Arg6.ToIntStr()}";
			}
		}
	}

	[Opcode(InstructionOpcode.Sgn)]
	public sealed class SgnInstruction:UnaryOperationInstruction
	{
		public sealed override SgnStack StackOperation{get;}

		public SgnInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SgnStack(SgnInstruction instruction):UnaryOperationStack<SgnInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"sgn({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.SpawnAfter)]
	public sealed class SpawnAfterInstruction:BaseSpawnInstruction
	{
		public sealed override SpawnAfterStack StackStatement{get;}

		public SpawnAfterInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SpawnAfterStack(SpawnAfterInstruction instruction):BaseSpawnStack<SpawnAfterInstruction>(instruction)
		{
			public override string ToStatement() => $"SpawnAfter {Instruction.SpawnStratProc.AsmLabel.SubroutineName()}, {Instruction.LocalVarsToPop}, {Instruction.LocalCount}, {Instruction.TriggerCount}, {Instruction.CollisionSize}, {Instruction.CollisionBoneCount}";
		}
	}

	[Opcode(InstructionOpcode.Camera_AlienVar)]
	public sealed class Camera_AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Camera;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Camera_AlienVarAddress)]
	public sealed class Camera_AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Camera;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Target_AlienVar)]
	public sealed class Target_AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Target;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Target_AlienVarAddress)]
	public sealed class Target_AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Target;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Collide_AlienVar)]
	public sealed class Collide_AlienVarInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.Collide_AlienVarAddress)]
	public sealed class Collide_AlienVarAddressInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.Target2_AlienVar)]
	public sealed class Target2_AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Target2;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Target2_AlienVarAddress)]
	public sealed class Target2_AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Target2;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.DontLookAtMe)]
	public sealed class DontLookAtMeInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.RunAt60)]
	public sealed class RunAt60Instruction():UnimplementedInstruction(fail:false);

	[Opcode(InstructionOpcode.MoveForwardq)]
	public sealed class MoveForwardqInstruction:BaseMoveInstruction
	{
		public sealed override MoveForwardqStack StackStatement{get;}

		public MoveForwardqInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveForwardqStack(MoveForwardqInstruction instruction):BaseMoveStack<MoveForwardqInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveForwardQ {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveBackwardq)]
	public sealed class MoveBackwardqInstruction:BaseMoveInstruction
	{
		public sealed override MoveBackwardqStack StackStatement{get;}

		public MoveBackwardqInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveBackwardqStack(MoveBackwardqInstruction instruction):BaseMoveStack<MoveBackwardqInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveBackwardQ {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.ScreenPrint)]
	public sealed class ScreenPrintInstruction:BasePrintInstruction
	{
		public sealed override ScreenPrintStack StackStatement{get;}

		public ScreenPrintInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ScreenPrintStack(ScreenPrintInstruction instruction):SimpleNoStackStack<ScreenPrintInstruction>(instruction)
		{
			public override string ToStatement() => $"screenprint {Instruction.Data}";
		}
	}

	[Opcode(InstructionOpcode.SoundPlay2)]
	public sealed class SoundPlay2Instruction:SimplePureConsumerInstruction
	{
		public sealed override SoundPlay2Stack StackStatement{get;}

		public SoundPlay2Instruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundPlay2Stack(SoundPlay2Instruction instruction):SimplePureConsumerStack<SoundPlay2Instruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer SoundIndex => Operands[0];
			public IStackProducer Volume => Operands[1];

			public override string ToStatement() => $"SoundPlay {SoundIndex.ToIntStr()}, {Volume.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SoundPlay2ASS)]
	public sealed class SoundPlay2AssignmentInstruction:BaseExpressionInstruction
	{
		public sealed override SoundPlay2AssignmentStack StackOperation{get;}

		public SoundPlay2AssignmentInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SoundPlay2AssignmentStack(SoundPlay2AssignmentInstruction instruction):BaseExpressionStack<SoundPlay2AssignmentInstruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer SoundIndex => Operands[0];
			public IStackProducer Volume => Operands[1];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToIntStr()}, {Volume.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SetWP)]
	public sealed class SetWPInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ResetWP)]
	public sealed class ResetWPInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SoundVolume)]
	public sealed class SoundVolumeInstruction:SimplePureConsumerInstruction
	{
		public sealed override SoundVolumeStack StackStatement{get;}

		public SoundVolumeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundVolumeStack(SoundVolumeInstruction instruction):SimplePureConsumerStack<SoundVolumeInstruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer Channel => Operands[0];
			public IStackProducer Volume => Operands[1];

			public override string ToStatement() => $"SoundVolume {Channel.ToIntStr()}, {Volume.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Push)]
	public sealed class PushInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.String)]
	public sealed class StringInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SetBossHearts)]
	public sealed class SetBossHeartsInstruction:SimplePureConsumerInstruction
	{
		public sealed override SetBossHeartsStack StackStatement{get;}

		public SetBossHeartsInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SetBossHeartsStack(SetBossHeartsInstruction instruction):SimplePureConsumerStack<SetBossHeartsInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Health => Operands[0];

			public override string ToStatement() => $"SetBossHearts {Health.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.LoseBossHeart)]
	public sealed class LoseBossHeartInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SoundShiftRelative)]
	public sealed class SoundShiftRelativeInstruction:SimplePureConsumerInstruction
	{
		public sealed override SoundShiftRelativeStack StackStatement{get;}

		public SoundShiftRelativeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundShiftRelativeStack(SoundShiftRelativeInstruction instruction):SimplePureConsumerStack<SoundShiftRelativeInstruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer Channel => Operands[0];
			public IStackProducer Pitch => Operands[1];

			public override string ToStatement() => $"SoundShiftRelative {Channel.ToIntStr()}, {Pitch.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Smin)]
	public sealed class SminInstruction:BinaryOperationInstruction
	{
		public sealed override SminStack StackOperation{get;}

		public SminInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SminStack(SminInstruction instruction):BinaryOperationStack<SminInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"smin({ValueA.ToFxString()}, {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.IsBoss)]
	public sealed class IsBossInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.TopSay)]
	public sealed class TopSayInstruction:DialogSayInstruction
	{
		public sealed override TopSayStack StackStatement{get;}

		public TopSayInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TopSayStack(TopSayInstruction instruction):DialogSayStack<TopSayInstruction>(instruction)
		{
			public override string ToStatement() => $"TopSay \"{Instruction.EnglishString}\"";
		}
	}

	[Opcode(InstructionOpcode.Boss_AlienVar)]
	public sealed class Boss_AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Boss;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Boss_AlienVarAddress)]
	public sealed class Boss_AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Boss;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.GetParentPos)]
	public sealed class GetParentPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.AfterBoss)]
	public sealed class AfterBossInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.AfterPlayer)]
	public sealed class AfterPlayerInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.BeforePlayer)]
	public sealed class BeforePlayerInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.BeforeBoss)]
	public sealed class BeforeBossInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.NoHang)]
	public sealed class NoHangInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Zero)]
	public sealed class ZeroInstruction:PureProducerInstruction
	{
		public sealed override bool Literal => true;

		public sealed override ZeroStack StackProducer{get;}

		public ZeroInstruction()
		{
			StackProducer = new(this);
		}

		public sealed class ZeroStack(ZeroInstruction instruction):PureProducerStack<ZeroInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => "0";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? "0" : "(NOT 0)";
		}
	}

	[Opcode(InstructionOpcode.TopHead)]
	public sealed class TopHeadInstruction:SimplePureConsumerInstruction
	{
		public sealed override TopHeadStack StackStatement{get;}

		public TopHeadInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TopHeadStack(TopHeadInstruction instruction):SimplePureConsumerStack<TopHeadInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Sprite => Operands[0];

			public override string ToStatement() => $"TopHead {Sprite.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.TopDialog)]
	public sealed class TopDialogInstruction:DialogSetInstruction
	{
		public sealed override TopDialogStack StackStatement{get;}

		public TopDialogInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class TopDialogStack(TopDialogInstruction instruction):DialogSetStack<TopDialogInstruction>(instruction)
		{
			public override string ToStatement() => Instruction.State ? "TopDialog on" : "TopDialog off";
		}
	}

	[Opcode(InstructionOpcode.BottomSay)]
	public sealed class BottomSayInstruction:DialogSayInstruction
	{
		public sealed override BottomSayStack StackStatement{get;}

		public BottomSayInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class BottomSayStack(BottomSayInstruction instruction):DialogSayStack<BottomSayInstruction>(instruction)
		{
			public override string ToStatement() => $"BottomSay \"{Instruction.EnglishString}\"";
		}
	}

	[Opcode(InstructionOpcode.BottomHead)]
	public sealed class BottomHeadInstruction:SimplePureConsumerInstruction
	{
		public sealed override BottomHeadStack StackStatement{get;}

		public BottomHeadInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class BottomHeadStack(BottomHeadInstruction instruction):SimplePureConsumerStack<BottomHeadInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Sprite => Operands[0];

			public override string ToStatement() => $"BottomHead {Sprite.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.BottomDialog)]
	public sealed class BottomDialogInstruction:DialogSetInstruction
	{
		public sealed override BottomDialogStack StackStatement{get;}

		public BottomDialogInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class BottomDialogStack(BottomDialogInstruction instruction):DialogSetStack<BottomDialogInstruction>(instruction)
		{
			public override string ToStatement() => Instruction.State ? "BottomDialog on" : "BottomDialog off";
		}
	}

	[Opcode(InstructionOpcode.GetPlayerPos)]
	public sealed class GetPlayerPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GetWPpos)]
	public sealed class GetWPposInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GetBossPos)]
	public sealed class GetBossPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GetDoorPos)]
	public sealed class GetDoorPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.FadeOut)]
	public sealed class FadeOutInstruction:SimplePureConsumerInstruction
	{
		public sealed override FadeOutStack StackStatement{get;}

		public FadeOutInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class FadeOutStack(FadeOutInstruction instruction):SimplePureConsumerStack<FadeOutInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer FadeType => Operands[0];

			public override string ToStatement() => $"FadeOut {FadeType.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.FadeIn)]
	public sealed class FadeInInstruction:SimplePureConsumerInstruction
	{
		public sealed override FadeInStack StackStatement{get;}

		public FadeInInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class FadeInStack(FadeInInstruction instruction):SimplePureConsumerStack<FadeInInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer FadeType => Operands[0];

			public override string ToStatement() => $"FadeIn {FadeType.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveUpq)]
	public sealed class MoveUpqInstruction:BaseMoveInstruction
	{
		public sealed override MoveUpqStack StackStatement{get;}

		public MoveUpqInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveUpqStack(MoveUpqInstruction instruction):BaseMoveStack<MoveUpqInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveUpQ {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveDownq)]
	public sealed class MoveDownqInstruction:BaseMoveInstruction
	{
		public sealed override MoveDownqStack StackStatement{get;}

		public MoveDownqInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveDownqStack(MoveDownqInstruction instruction):BaseMoveStack<MoveDownqInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveDownQ {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.ForcePlayerDist)]
	public sealed class ForcePlayerDistInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ShadeType)]
	public sealed class ShadeTypeInstruction:SimplePureConsumerInstruction
	{
		public sealed override ShadeTypeStack StackStatement{get;}

		public ShadeTypeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class ShadeTypeStack(ShadeTypeInstruction instruction):SimplePureConsumerStack<ShadeTypeInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ShadeType => Operands[0];

			//TODO: Custom int values?
			public override string ToStatement() => $"ShadeType {ShadeType.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.NOP)]
	public sealed class NOPInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetAnimSpeed)]
	public sealed class SetAnimSpeedInstruction:SimplePureConsumerInstruction
	{
		public sealed override SetAnimSpeedStack StackStatement{get;}

		public SetAnimSpeedInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SetAnimSpeedStack(SetAnimSpeedInstruction instruction):SimplePureConsumerStack<SetAnimSpeedInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer AnimSpeed => Operands[0];

			public override string ToStatement() => $"SetAnimSpeed {AnimSpeed.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.CheckLevelDoor)]
	public sealed class CheckLevelDoorInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.BottomHeadLeft)]
	public sealed class BottomHeadLeftInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.TopHeadLeft)]
	public sealed class TopHeadLeftInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GainJigsaw)]
	public sealed class GainJigsawInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GainGoldenGobbo)]
	public sealed class GainGoldenGobboInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Gain100Crystal)]
	public sealed class Gain100CrystalInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ResetSpline)]
	public sealed class ResetSplineInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.CheckPoint)]
	public sealed class CheckPointInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WaterTest)]
	public sealed class WaterTestInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.IsMainCamera)]
	public sealed class IsMainCameraInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ResetDialog)]
	public sealed class ResetDialogInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.EndLevel)]
	public sealed class EndLevelInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Dialog_AlienVar)]
	public sealed class Dialog_AlienVarInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Dialog;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Dialog_AlienVarAddress)]
	public sealed class Dialog_AlienVarAddressInstruction:VarInstruction
	{
		public sealed override SourceStrat Source => SourceStrat.Dialog;
		public sealed override VarType Type => VarType.Alien;
		public sealed override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.IsDialog)]
	public sealed class IsDialogInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Distance)]
	public sealed class DistanceInstruction:BaseExpressionInstruction
	{
		public sealed override DistanceStack StackOperation{get;}

		public DistanceInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class DistanceStack(DistanceInstruction instruction):BaseExpressionStack<DistanceInstruction>(instruction)
		{
			public override int PopCount => 3;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"Distance {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Binocs)]
	public sealed class BinocsInstruction:SimpleNoStackInstruction
	{
		public bool State;

		public sealed override BinocsStack StackStatement{get;}

		public BinocsInstruction()
		{
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var binocsAsmInstr = GetAsmInstruction<Disassembler.BinocsInstruction>();

			State = binocsAsmInstr.State switch
			{
				1 => true,
				0 => false,
				_ => throw new Exception("Invalid binoc state")
			};
		}

		public sealed class BinocsStack(BinocsInstruction instruction):SimpleNoStackStack<BinocsInstruction>(instruction)
		{
			public override string ToStatement() => Instruction.State ? "Binocs on" : "Binocs off";
		}
	}

	[Opcode(InstructionOpcode.TopCloseDialog)]
	public sealed class TopCloseDialogInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.BottomCloseDialog)]
	public sealed class BottomCloseDialogInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.NextInventory)]
	public sealed class NextInventoryInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.PrevInventory)]
	public sealed class PrevInventoryInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.OtherPiece)]
	public sealed class OtherPieceInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.NormalPiece)]
	public sealed class NormalPieceInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Climb)]
	public sealed class ClimbInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.DelInv)]
	public sealed class DelInvInstruction:ItemChangeInstruction
	{
		public sealed override int Change => -1;
	}

	[Opcode(InstructionOpcode.GainReward)]
	public sealed class GainRewardInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.WorldVector)]
	public sealed class WorldVectorInstruction:SimplePureConsumerInstruction
	{
		public sealed override WorldVectorStack StackStatement{get;}

		public WorldVectorInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class WorldVectorStack(WorldVectorInstruction instruction):SimplePureConsumerStack<WorldVectorInstruction>(instruction)
		{
			public override int PopCount => 3;

			//This is the correct pop order
			public IStackProducer Z => Operands[0];
			public IStackProducer X => Operands[1];
			public IStackProducer Angle => Operands[2];

			public override string ToStatement() => $"WorldVector {Z.ToFxString()}, {X.ToFxString()}, {Angle.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.ObjectFallVerySlow)]
	public sealed class ObjectFallVerySlowInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Slope2Controller)]
	public sealed class Slope2ControllerInstruction:SimplePureConsumerInstruction
	{
		public sealed override Slope2ControllerStack StackStatement{get;}

		public Slope2ControllerInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class Slope2ControllerStack(Slope2ControllerInstruction instruction):SimplePureConsumerStack<Slope2ControllerInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Unused => Operands[0];

			public override string ToStatement()
			{ 
				if(Unused is not NumberInstruction.NumberStack num || num.Instruction.Value != 0)
				{
					throw new Exception("Slope2Controller has non-zero operand");
				}
				return $"Slope2Controller {Unused.ToIntStr()}";
			}
		}
	}

	[Opcode(InstructionOpcode.LevelComplete)]
	public sealed class LevelCompleteInstruction:BaseExpressionInstruction
	{
		public sealed override LevelCompleteStack StackOperation{get;}

		public LevelCompleteInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class LevelCompleteStack(LevelCompleteInstruction instruction):BaseExpressionStack<LevelCompleteInstruction>(instruction)
		{
			public override int PopCount => 3;

			public IStackProducer Tribe => Operands[0];
			public IStackProducer Level => Operands[1];
			public IStackProducer Type => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"LevelComplete {Tribe.ToFxString()}, {Level.ToFxString()}, {Type.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SetLevelFlag)]
	public sealed class SetLevelFlagInstruction:SimplePureConsumerInstruction
	{
		public sealed override SetLevelFlagStack StackStatement{get;}

		public SetLevelFlagInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SetLevelFlagStack(SetLevelFlagInstruction instruction):SimplePureConsumerStack<SetLevelFlagInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer LevelFlag => Operands[0];

			public override string ToStatement() => $"SetLevelFlag {LevelFlag.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.GetLevelFlag)]
	public sealed class GetLevelFlagInstruction:BaseExpressionInstruction
	{
		public sealed override GetLevelFlagStack StackOperation{get;}

		public GetLevelFlagInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class GetLevelFlagStack(GetLevelFlagInstruction instruction):BaseExpressionStack<GetLevelFlagInstruction>(instruction)
		{
			public override int PopCount => 3;

			public IStackProducer Tribe => Operands[0];
			public IStackProducer Level => Operands[1];
			public IStackProducer Type => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"GetLevelFlag {Tribe.ToFxString()}, {Level.ToFxString()}, {Type.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.CalcCarTilt)]
	public sealed class CalcCarTiltInstruction:SimplePureConsumerInstruction
	{
		public sealed override CalcCarTiltStack StackStatement{get;}

		public CalcCarTiltInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class CalcCarTiltStack(CalcCarTiltInstruction instruction):SimplePureConsumerStack<CalcCarTiltInstruction>(instruction)
		{
			public override int PopCount => 4;

			//Notice the backwards numbering
			public IStackProducer Op4 => Operands[0];
			public IStackProducer Op3 => Operands[1];
			public IStackProducer Op2 => Operands[2];
			public IStackProducer Op1 => Operands[3];

			public override string ToStatement() => $"CalcCarTilt {Op4.ToFxString()}, {Op3.ToFxString()}, {Op2.ToFxString()}, {Op1.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveLeftq)]
	public sealed class MoveLeftqInstruction:BaseMoveInstruction
	{
		public sealed override MoveLeftqStack StackStatement{get;}

		public MoveLeftqInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveLeftqStack(MoveLeftqInstruction instruction):BaseMoveStack<MoveLeftqInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveLeftQ {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.MoveRightq)]
	public sealed class MoveRightqInstruction:BaseMoveInstruction
	{
		public sealed override MoveRightqStack StackStatement{get;}

		public MoveRightqInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class MoveRightqStack(MoveRightqInstruction instruction):BaseMoveStack<MoveRightqInstruction>(instruction)
		{
			public override string ToStatement() => $"MoveRightQ {Amount.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.BitwiseNot)]
	public sealed class BitwiseNotInstruction:UnaryOperationInstruction
	{
		public sealed override BitwiseNotStack StackOperation{get;}

		public BitwiseNotInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class BitwiseNotStack(BitwiseNotInstruction instruction):UnaryOperationStack<BitwiseNotInstruction>(instruction)
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"~{Value.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.BordersOn)]
	public sealed class BordersOnInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.BordersOff)]
	public sealed class BordersOffInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SoundAdsr)]
	public sealed class SoundAdsrInstruction:SimplePureConsumerInstruction
	{
		public sealed override SoundAdsrStack StackStatement{get;}

		public SoundAdsrInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundAdsrStack(SoundAdsrInstruction instruction):SimplePureConsumerStack<SoundAdsrInstruction>(instruction)
		{
			public override int PopCount => 5;

			public IStackProducer Channel => Operands[0];
			public IStackProducer A => Operands[1];
			public IStackProducer D => Operands[2];
			public IStackProducer S => Operands[3];
			public IStackProducer R => Operands[4];

			public override string ToStatement() => $"SoundAdsr {Channel.ToIntStr()}, {A.ToFxString()}, {D.ToFxString()}, {S.ToFxString()}, {R.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SoundAdsrRelative)]
	public sealed class SoundAdsrRelativeInstruction:SimplePureConsumerInstruction
	{
		public sealed override SoundAdsrRelativeStack StackStatement{get;}

		public SoundAdsrRelativeInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SoundAdsrRelativeStack(SoundAdsrRelativeInstruction instruction):SimplePureConsumerStack<SoundAdsrRelativeInstruction>(instruction)
		{
			public override int PopCount => 5;

			public IStackProducer Channel => Operands[0];
			public IStackProducer A => Operands[1];
			public IStackProducer D => Operands[2];
			public IStackProducer S => Operands[3];
			public IStackProducer R => Operands[4];

			public override string ToStatement() => $"SoundAdsrRelative {Channel.ToIntStr()}, {A.ToFxString()}, {D.ToFxString()}, {S.ToFxString()}, {R.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.RotatePiece)]
	public sealed class RotatePieceInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetAmbient)]
	public sealed class SetAmbientInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.ResetAmbient)]
	public sealed class ResetAmbientInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.InvActive)]
	public sealed class InvActiveInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.InvInactive)]
	public sealed class InvInactiveInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SampleStatus)]
	public sealed class SampleStatusInstruction:BaseExpressionInstruction
	{
		public sealed override SampleStatusStack StackOperation{get;}

		public SampleStatusInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class SampleStatusStack(SampleStatusInstruction instruction):BaseExpressionStack<SampleStatusInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ID => Operands[0];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SampleStatus {ID.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.ResetToCheckPointnlh)]
	public sealed class ResetToCheckPointnlhInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ResetDoor)]
	public sealed class ResetDoorInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.StoreDoor)]
	public sealed class StoreDoorInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Camera_modified)]
	public sealed class Camera_modifiedInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.PushPlayer)]
	public sealed class PushPlayerInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.PopPlayer)]
	public sealed class PopPlayerInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ReSetPostrn)]
	public sealed class ReSetPostrnInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.GainItem)]
	public sealed class GainItemInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetItem)]
	public sealed class SetItemInstruction:SimplePureConsumerInstruction
	{
		public sealed override SetItemStack StackStatement{get;}

		public SetItemInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SetItemStack(SetItemInstruction instruction):SimplePureConsumerStack<SetItemInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer ExtraMax => Operands[0];

			public override string ToStatement() => $"SetItem {ExtraMax.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SetTimer)]
	public sealed class SetTimerInstruction:SimplePureConsumerInstruction
	{
		public sealed override SetTimerStack StackStatement{get;}

		public SetTimerInstruction()
		{
			StackStatement = new(this);
		}

		public sealed class SetTimerStack(SetTimerInstruction instruction):SimplePureConsumerStack<SetTimerInstruction>(instruction)
		{
			public override int PopCount => 1;

			public IStackProducer Timer => Operands[0];

			public override string ToStatement() => $"SetTimer {Timer.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.TimerOff)]
	public sealed class TimerOffInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.DistanceNoY)]
	public sealed class DistanceNoYInstruction:BaseExpressionInstruction
	{
		public sealed override DistanceNoYStack StackOperation{get;}

		public DistanceNoYInstruction()
		{
			StackOperation = new(this);
		}

		public sealed class DistanceNoYStack(DistanceNoYInstruction instruction):BaseExpressionStack<DistanceNoYInstruction>(instruction)
		{
			public override int PopCount => 2;

			public IStackProducer X => Operands[0];
			public IStackProducer Z => Operands[1];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"DistanceNoY {X.ToFxString()}, {Z.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Swim)]
	public sealed class SwimInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Lose100Crystals)]
	public sealed class Lose100CrystalsInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.LoseReward)]
	public sealed class LoseRewardInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.LoseGoldenGobbo)]
	public sealed class LoseGoldenGobboInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.NextTribe)]
	public sealed class NextTribeInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.PrevTribe)]
	public sealed class PrevTribeInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetTimerClock)]
	public sealed class SetTimerClockInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetTimerBomb)]
	public sealed class SetTimerBombInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.InitBurpingGame)]
	public sealed class InitBurpingGameInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.CloseBurpingGame)]
	public sealed class CloseBurpingGameInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Credit)]
	public sealed class CreditInstruction:SimpleNoStackInstruction
	{
		public int Operand;

		public sealed override CreditStack StackStatement{get;}

		public CreditInstruction()
		{
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var creditAsmInstr = GetAsmInstruction<Disassembler.CreditInstruction>();

			Operand = creditAsmInstr.Operand;
		}

		public sealed class CreditStack(CreditInstruction instruction):SimpleNoStackStack<CreditInstruction>(instruction)
		{
			public override string ToStatement() => $"Credit {Instruction.Operand}";
		}
	}

	[Opcode(InstructionOpcode.CloseCredits)]
	public sealed class CloseCreditsInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ShowRewardCard)]
	public sealed class ShowRewardCardInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ShowHearts)]
	public sealed class ShowHeartsInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Cwg)]
	public sealed class CwgInstruction:SimpleNoStackInstruction
	{
		public int Value;

		public sealed override CwgStack StackStatement{get;}

		public CwgInstruction()
		{
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var cwgAsmInstr = GetAsmInstruction<Disassembler.CwgInstruction>();

			Value = cwgAsmInstr.Value;
		}

		public sealed class CwgStack(CwgInstruction instruction):SimpleNoStackStack<CwgInstruction>(instruction)
		{
			public override string ToStatement() => $"Cwg {Instruction.Value}";
		}
	}

	[Opcode(InstructionOpcode.FadeFunction_47E960)]
	public sealed class FadeFunction_47E960Instruction:SimpleNoStackInstruction
	{
		public int Value;

		public sealed override FadeFunction_47E960Stack StackStatement{get;}

		public FadeFunction_47E960Instruction()
		{
			StackStatement = new(this);
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var fadeSetUnknownAsmInstr = GetAsmInstruction<Disassembler.FadeSetUnknownInstruction>();
			Value = fadeSetUnknownAsmInstr.Value;
		}

		public sealed class FadeFunction_47E960Stack(FadeFunction_47E960Instruction instruction):SimpleNoStackStack<FadeFunction_47E960Instruction>(instruction)
		{
			public override string ToStatement() => $"FadeFunction_47E960 {Instruction.Value}";
		}
	}

	[Opcode(InstructionOpcode.CameraFunction_47F1E0)]
	public sealed class CameraFunction_47F1E0Instruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.CameraFunction_47F040)]
	public sealed class CameraFunction_47F040Instruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.CameraFunction_47F0C0)]
	public sealed class CameraFunction_47F0C0Instruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.CameraFunction_47F490)]
	public sealed class CameraFunction_47F490Instruction:SimpleNoOperandsNoStackInstruction;
}