using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using ArgonautReverse.Universal.StratLang.Disassembler;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public static class InstructionLookup
	{
		private delegate Instruction DelCreateInstruction(AsmInstruction label, AsmInstruction operation);
		private static readonly Dictionary<InstructionOpcode,DelCreateInstruction> byOpcode = new Dictionary<InstructionOpcode,DelCreateInstruction>();

		public static readonly HashSet<InstructionOpcode> Used = new HashSet<InstructionOpcode>();

		static InstructionLookup()
		{
			var createInstructionGenericInfo = ((DelCreateInstruction)CreateInstructionInner<NOPInstruction>).Method.GetGenericMethodDefinition();
			var types = typeof(Instruction).Assembly.GetTypes();
			foreach(var type in types)
			{
				if(type.GetCustomAttribute<OpcodeAttribute>() is OpcodeAttribute opcodeAttribute)
				{
					var createInstruction = createInstructionGenericInfo.MakeGenericMethod(type).CreateDelegate<DelCreateInstruction>();
					byOpcode.Add(opcodeAttribute.Opcode, createInstruction);
				}
			}

			static Instruction CreateInstructionInner<T>(AsmInstruction label, AsmInstruction operation) where T:Instruction,new()
			{
				var instr = new T
				{
					AsmOperation = operation,
					AsmLabel = label,
				};
				return instr;
			}
		}

		public static Instruction CreateInstruction(AsmInstruction label, AsmInstruction operation)
		{
			Used.Add(operation.OpCode);

			return byOpcode[operation.OpCode](label, operation);
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class OpcodeAttribute(InstructionOpcode opcode):Attribute
	{
		public InstructionOpcode Opcode => opcode;
	}

	public abstract class UnimplementedInstruction:BaseOperationInstruction<UnimplementedInstruction,UnimplementedInstruction.UnimplementedStack>
	{
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

		public sealed class UnimplementedStack:BaseOperationStack<UnimplementedInstruction,UnimplementedStack>
		{
			public override IStackStatement Statement => throw new NotImplementedException();

			public override void Analyze(StackAnalyzer stack){}
			public override IEnumerable<IStackOperation> GetRootOperations() => throw new NotImplementedException();
			public override bool TryGetLabel([MaybeNullWhen(false)] out AsmInstruction label) => throw new NotImplementedException();
			public override bool TryGetSubroutine([MaybeNullWhen(false)] out AsmInstruction subroutine) => throw new NotImplementedException();
		}
	}

	public abstract class BaseOperationInstruction<TInstruction,TStack>:Instruction where TInstruction:BaseOperationInstruction<TInstruction,TStack> where TStack:BaseOperationStack<TInstruction,TStack>,new()
	{
		public sealed override TStack StackOperation{get;}

		public BaseOperationInstruction()
		{
			StackOperation = new(){Instruction = (TInstruction)this};
		}
	}
	public abstract class BaseOperationStack<TInstruction,TStack>:OperationStack<TInstruction> where TInstruction:BaseOperationInstruction<TInstruction,TStack> where TStack:BaseOperationStack<TInstruction,TStack>,new();
	public abstract class BaseOperationFlow<TInstruction,TStack,TFlow>:FlowStatement<TInstruction> where TInstruction:BaseOperationInstruction<TInstruction,TStack> where TStack:BaseOperationStack<TInstruction,TStack>,new() where TFlow:BaseOperationFlow<TInstruction,TStack,TFlow>,new();

	public abstract class BaseConsumerInstruction<TInstruction,TStack>:BaseOperationInstruction<TInstruction,TStack> where TInstruction:BaseConsumerInstruction<TInstruction,TStack> where TStack:BaseConsumerStack<TInstruction,TStack>,new();
	public abstract class BaseConsumerStack<TInstruction,TStack>:BaseOperationStack<TInstruction,TStack>,IStackConsumer where TInstruction:BaseConsumerInstruction<TInstruction,TStack> where TStack:BaseConsumerStack<TInstruction,TStack>,new()
	{
		protected abstract int PopCount{get;}

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
				var producer = operand;
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
				var producer = operand;
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

	public abstract class PureConsumerInstruction<TInstruction,TStack,TFlow>:BaseConsumerInstruction<TInstruction,TStack> where TInstruction:PureConsumerInstruction<TInstruction,TStack,TFlow> where TStack:PureConsumerStack<TInstruction,TStack,TFlow>,new() where TFlow:PureConsumerFlow<TInstruction,TStack,TFlow>,new()
	{
		public TFlow FlowStatement{get;}

		public PureConsumerInstruction()
		{
			FlowStatement = new(){Instruction = (TInstruction)this};
		}
	}
	public abstract class PureConsumerStack<TInstruction,TStack,TFlow>:BaseConsumerStack<TInstruction,TStack>,IStackStatement where TInstruction:PureConsumerInstruction<TInstruction,TStack,TFlow> where TStack:PureConsumerStack<TInstruction,TStack,TFlow>,new() where TFlow:PureConsumerFlow<TInstruction,TStack,TFlow>,new()
	{
		public Instruction StatementInstruction => Instruction;
		public FlowStatement FlowStatement => Instruction.FlowStatement;

		public override IStackStatement Statement => this;

		public AsmInstruction StatementLabel{get;private set;}
		public Instruction FirstInstruction{get;private set;}

		public IStackStatement NextStatement{get;set;}
		public IStackStatement PrevStatement{get;set;}

		public abstract void WriteStatement(Writer writer);

		public override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);

			StatementLabel = stack.CurrentStatementFirstInstruction.AsmLabel;
			FirstInstruction = stack.CurrentStatementFirstInstruction;
		}
	}

	public abstract class PureConsumerFlow<TInstruction,TStack,TFlow>:BaseOperationFlow<TInstruction,TStack,TFlow> where TInstruction:PureConsumerInstruction<TInstruction,TStack,TFlow> where TStack:PureConsumerStack<TInstruction,TStack,TFlow>,new() where TFlow:PureConsumerFlow<TInstruction,TStack,TFlow>,new()
	{
		public sealed override IStackStatement StackStatement => Instruction.StackOperation;
	}

	public abstract class SimplePureConsumerInstruction<TInstruction,TStack>:PureConsumerInstruction<TInstruction,TStack,SimplePureConsumerFlow<TInstruction,TStack>> where TInstruction:SimplePureConsumerInstruction<TInstruction,TStack> where TStack:SimplePureConsumerStack<TInstruction,TStack>,new();
	public abstract class SimplePureConsumerStack<TInstruction,TStack>:PureConsumerStack<TInstruction,TStack,SimplePureConsumerFlow<TInstruction,TStack>> where TInstruction:SimplePureConsumerInstruction<TInstruction,TStack> where TStack:SimplePureConsumerStack<TInstruction,TStack>,new();
	public sealed class SimplePureConsumerFlow<TInstruction,TStack>:PureConsumerFlow<TInstruction,TStack,SimplePureConsumerFlow<TInstruction,TStack>> where TInstruction:SimplePureConsumerInstruction<TInstruction,TStack> where TStack:SimplePureConsumerStack<TInstruction,TStack>,new();

	public abstract class PureProducerInstruction<TInstruction,TStack>:BaseOperationInstruction<TInstruction,TStack> where TInstruction:PureProducerInstruction<TInstruction,TStack> where TStack:PureProducerStack<TInstruction,TStack>,new();
	public abstract class PureProducerStack<TInstruction,TStack>:BaseOperationStack<TInstruction,TStack>,IStackProducer where TInstruction:PureProducerInstruction<TInstruction,TStack> where TStack:PureProducerStack<TInstruction,TStack>,new()
	{
		public IStackConsumer Consumer{get;set;}

		public sealed override IStackStatement Statement => Consumer.Statement;

		public virtual bool Literal => false;

		public sealed override void Analyze(StackAnalyzer stack)
		{
			stack.Push(this);
		}

		public override IEnumerable<IStackOperation> GetRootOperations() => [this];

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

	public abstract class BaseExpressionInstruction<TInstruction,TStack>:BaseConsumerInstruction<TInstruction,TStack> where TInstruction:BaseExpressionInstruction<TInstruction,TStack> where TStack:BaseExpressionStack<TInstruction,TStack>,new();

	public abstract class BaseExpressionStack<TInstruction,TStack>:BaseConsumerStack<TInstruction,TStack>,IStackExpression where TInstruction:BaseExpressionInstruction<TInstruction,TStack> where TStack:BaseExpressionStack<TInstruction,TStack>,new()
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

	/// <summary>Instructions that don't use the stack</summary>
	public abstract class NoStackInstruction<TInstruction,TStack,TFlow>:BaseOperationInstruction<TInstruction,TStack> where TInstruction:NoStackInstruction<TInstruction,TStack,TFlow> where TStack:NoStackStack<TInstruction,TStack,TFlow>,new() where TFlow:NoStackFlow<TInstruction,TStack,TFlow>,new()
	{
		public TFlow FlowStatement{get;}

		public NoStackInstruction()
		{
			FlowStatement = new(){Instruction = (TInstruction)this};
		}
	}
	public abstract class NoStackStack<TInstruction,TStack,TFlow>:BaseOperationStack<TInstruction,TStack>,IStackStatement where TInstruction:NoStackInstruction<TInstruction,TStack,TFlow> where TStack:NoStackStack<TInstruction,TStack,TFlow>,new() where TFlow:NoStackFlow<TInstruction,TStack,TFlow>,new()
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

		public abstract void WriteStatement(Writer writer);

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

	public abstract class NoStackFlow<TInstruction,TStack,TFlow>:BaseOperationFlow<TInstruction,TStack,TFlow> where TInstruction:NoStackInstruction<TInstruction,TStack,TFlow> where TStack:NoStackStack<TInstruction,TStack,TFlow>,new() where TFlow:NoStackFlow<TInstruction,TStack,TFlow>,new()
	{
		public sealed override IStackStatement StackStatement => Instruction.StackOperation;
	}

	public abstract class SimpleNoStackInstruction<TInstruction,TStack>:NoStackInstruction<TInstruction,TStack,SimpleNoStackFlow<TInstruction,TStack>> where TInstruction:SimpleNoStackInstruction<TInstruction,TStack> where TStack:SimpleNoStackStack<TInstruction,TStack>,new();
	public abstract class SimpleNoStackStack<TInstruction,TStack>:NoStackStack<TInstruction,TStack,SimpleNoStackFlow<TInstruction,TStack>> where TInstruction:SimpleNoStackInstruction<TInstruction,TStack> where TStack:SimpleNoStackStack<TInstruction,TStack>,new();

	public sealed class SimpleNoStackFlow<TInstruction,TStack>:NoStackFlow<TInstruction,TStack,SimpleNoStackFlow<TInstruction,TStack>> where TInstruction:SimpleNoStackInstruction<TInstruction,TStack> where TStack:SimpleNoStackStack<TInstruction,TStack>,new();

	public abstract class SimpleNoOperandsNoStackInstruction:SimpleNoStackInstruction<SimpleNoOperandsNoStackInstruction,SimpleNoOperandsNoStackStack>;
	public sealed class SimpleNoOperandsNoStackStack:SimpleNoStackStack<SimpleNoOperandsNoStackInstruction,SimpleNoOperandsNoStackStack>
	{
		public sealed override void WriteStatement(Writer writer)
		{
			//TODO: Make this better
			string name = Instruction.GetType().GetCustomAttribute<OpcodeAttribute>()!.Opcode.ToString();
			writer.Write(name);
		}
	}

	public abstract class BinaryOperationInstruction<TInstruction,TStack>:BaseExpressionInstruction<TInstruction,TStack> where TInstruction:BinaryOperationInstruction<TInstruction,TStack> where TStack:BinaryOperationStack<TInstruction,TStack>,new();
	public abstract class BinaryOperationStack<TInstruction,TStack>:BaseExpressionStack<TInstruction,TStack> where TInstruction:BinaryOperationInstruction<TInstruction,TStack> where TStack:BinaryOperationStack<TInstruction,TStack>,new()
	{
		protected sealed override int PopCount => 2;

		public IStackProducer ValueA => Operands[0];
		public IStackProducer ValueB => Operands[1];
	}

	public abstract class UnaryOperationInstruction<TInstruction,TStack>:BaseExpressionInstruction<TInstruction,TStack> where TInstruction:UnaryOperationInstruction<TInstruction,TStack> where TStack:UnaryOperationStack<TInstruction,TStack>,new();
	public abstract class UnaryOperationStack<TInstruction,TStack>:BaseExpressionStack<TInstruction,TStack> where TInstruction:UnaryOperationInstruction<TInstruction,TStack> where TStack:UnaryOperationStack<TInstruction,TStack>,new()
	{
		protected sealed override int PopCount => 1;

		public IStackProducer Value => Operands[0];
	}

	public abstract class BranchInstruction<TInstruction,TStack>:PureConsumerInstruction<TInstruction,TStack,BranchFlow<TInstruction,TStack>> where TInstruction:BranchInstruction<TInstruction,TStack> where TStack:BranchStack<TInstruction,TStack>,new()
	{
		public Instruction ConditionalDest;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);

			var branchAsmInstr = GetAsmInstruction<Disassembler.BranchInstruction>();
			ConditionalDest = parser.GetInstruction(branchAsmInstr.ConditionalDest, null, this);
		}
	}
	public abstract class BranchStack<TInstruction,TStack>:PureConsumerStack<TInstruction,TStack,BranchFlow<TInstruction,TStack>> where TInstruction:BranchInstruction<TInstruction,TStack> where TStack:BranchStack<TInstruction,TStack>,new()
	{
		protected override int PopCount => 1;

		public IStackProducer Condition => Operands[0];

		public override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);
			stack.AddDest(Instruction.ConditionalDest);
		}
	}

	public sealed class BranchFlow<TInstruction,TStack>:PureConsumerFlow<TInstruction,TStack,BranchFlow<TInstruction,TStack>>,IFlowControl,IFlowBranch where TInstruction:BranchInstruction<TInstruction,TStack> where TStack:BranchStack<TInstruction,TStack>,new()
	{
		FlowStatement IFlowBranch.FlowStatement => this;

		public IStackStatement ControlStatement => Instruction.StackOperation;

		public FlowStatement FlowConditionalDest => FlowDestinations[0];

		public void Analyze(FlowAnalyzer flow)
		{
			flow.AddDest(this, Instruction.ConditionalDest);
		}
	}

	public abstract class DialogSayInstruction<TInstruction,TStack>:SimpleNoStackInstruction<TInstruction,TStack> where TInstruction:DialogSayInstruction<TInstruction,TStack> where TStack:DialogSayStack<TInstruction,TStack>,new()
	{
		public string EnglishString;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var dialogSayAsmInstr = GetAsmInstruction<Disassembler.DialogSayInstruction>();

			EnglishString = dialogSayAsmInstr.EnglishString;
		}
	}
	public abstract class DialogSayStack<TInstruction,TStack>:SimpleNoStackStack<TInstruction,TStack> where TInstruction:DialogSayInstruction<TInstruction,TStack> where TStack:DialogSayStack<TInstruction,TStack>,new();

	public abstract class DialogSetInstruction<TInstruction,TStack>:SimpleNoStackInstruction<TInstruction,TStack> where TInstruction:DialogSetInstruction<TInstruction,TStack> where TStack:DialogSetStack<TInstruction,TStack>,new()
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
	}
	public abstract class DialogSetStack<TInstruction,TStack>:SimpleNoStackStack<TInstruction,TStack> where TInstruction:DialogSetInstruction<TInstruction,TStack> where TStack:DialogSetStack<TInstruction,TStack>,new();

	public abstract class FlagInstruction:SimpleNoStackInstruction<FlagInstruction,FlagStack>
	{
		public readonly string Flag;

		//Operation
		//public int FlagGroup;
		//public int FlagChanged;

		//Operand
		public bool NewState;

		public FlagInstruction()
		{
			Flag = this.GetType().GetCustomAttribute<OpcodeAttribute>().Opcode.ToString();
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var flagAsmInstr = GetAsmInstruction<Disassembler.FlagInstruction>();

			NewState = flagAsmInstr.NewState;
		}
	}
	public sealed class FlagStack:SimpleNoStackStack<FlagInstruction,FlagStack>
	{
		public override void WriteStatement(Writer writer)
		{
			writer.Write(Instruction.Flag);
			writer.Write(Instruction.NewState ? " on" : " off");
		}
	}

	public abstract class ItemChangeInstruction:SimpleNoStackInstruction<ItemChangeInstruction,ItemChangeStack>
	{
		//Give/take item from inventory

		//Operand
		public int Item;

		//Operation
		public abstract int Change{get;}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var itemChangeAsmInstr = GetAsmInstruction<Disassembler.ItemChangeInstruction>();

			Item = itemChangeAsmInstr.Item;
		}
	}
	public sealed class ItemChangeStack:SimpleNoStackStack<ItemChangeInstruction,ItemChangeStack>
	{
		public override void WriteStatement(Writer writer)
		{
			writer.Write(Instruction.Change switch
			{
				-1 => "DelInv ",
				+1 => "AddInv ",
				_ => throw new Exception()
			});
			writer.WriteInt(Instruction.Item);
		}
	}

	public abstract class BaseJumpInstruction<TInstruction,TStack,TFlow>:NoStackInstruction<TInstruction,TStack,TFlow> where TInstruction:BaseJumpInstruction<TInstruction,TStack,TFlow> where TStack:BaseJumpStack<TInstruction,TStack,TFlow>,new() where TFlow:BaseJumpFlow<TInstruction,TStack,TFlow>,new()
	{
		public sealed override bool Terminal => true;

		public Instruction Destination;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var jumpAsmInstr = GetAsmInstruction<Disassembler.JumpInstruction>();
			Destination = parser.GetInstruction(jumpAsmInstr.Destination, null, this);
		}
	}
	public abstract class BaseJumpStack<TInstruction,TStack,TFlow>:NoStackStack<TInstruction,TStack,TFlow> where TInstruction:BaseJumpInstruction<TInstruction,TStack,TFlow> where TStack:BaseJumpStack<TInstruction,TStack,TFlow>,new() where TFlow:BaseJumpFlow<TInstruction,TStack,TFlow>,new()
	{
		public override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);

			stack.AddDest(Instruction.Destination);
		}
	}

	public abstract class BaseJumpFlow<TInstruction,TStack,TFlow>:NoStackFlow<TInstruction,TStack,TFlow>,IFlowTerminal,IFlowControl,IFlowJump where TInstruction:BaseJumpInstruction<TInstruction,TStack,TFlow> where TStack:BaseJumpStack<TInstruction,TStack,TFlow>,new() where TFlow:BaseJumpFlow<TInstruction,TStack,TFlow>,new()
	{
		FlowStatement IFlowJump.FlowStatement => this;

		public IStackStatement ControlStatement => Instruction.StackOperation;

		public FlowStatement FlowDestination => FlowDestinations[0];

		public void Analyze(FlowAnalyzer flow)
		{
			flow.AddDest(this, Instruction.Destination);
		}
	}

	public abstract class BaseJumpSubroutineInstruction<TInstruction,TStack>:SimpleNoStackInstruction<TInstruction,TStack> where TInstruction:BaseJumpSubroutineInstruction<TInstruction,TStack> where TStack:BaseJumpSubroutineStack<TInstruction,TStack>,new()
	{
		//Technically pushes a value but we aren't counting it because it's popped outside of this subroutine.

		public Instruction Proc;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var jumpSubroutineAsmInstr = GetAsmInstruction<Disassembler.JumpSubroutineInstruction>();
			Proc = parser.GetProc(jumpSubroutineAsmInstr.Proc, this);
		}
	}
	public abstract class BaseJumpSubroutineStack<TInstruction,TStack>:SimpleNoStackStack<TInstruction,TStack> where TInstruction:BaseJumpSubroutineInstruction<TInstruction,TStack> where TStack:BaseJumpSubroutineStack<TInstruction,TStack>,new();

	public abstract class BaseMoveInstruction<TInstruction,TStack>:SimplePureConsumerInstruction<TInstruction,TStack> where TInstruction:BaseMoveInstruction<TInstruction,TStack> where TStack:BaseMoveStack<TInstruction,TStack>,new();
	public abstract class BaseMoveStack<TInstruction,TStack>:SimplePureConsumerStack<TInstruction,TStack> where TInstruction:BaseMoveInstruction<TInstruction,TStack> where TStack:BaseMoveStack<TInstruction,TStack>,new()
	{
		protected sealed override int PopCount => 1;

		public IStackProducer Amount => Operands[0];
	}

	public abstract class BasePrintInstruction<TInstruction,TStack>:SimpleNoStackInstruction<TInstruction,TStack> where TInstruction:BasePrintInstruction<TInstruction,TStack> where TStack:SimpleNoStackStack<TInstruction,TStack>,new()
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
					case InstructionOpcode.String:
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

	public abstract class BaseSpawnInstruction<TInstruction,TStack>:SimpleNoStackInstruction<TInstruction,TStack> where TInstruction:BaseSpawnInstruction<TInstruction,TStack> where TStack:BaseSpawnStack<TInstruction,TStack>,new()
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
	}
	public abstract class BaseSpawnStack<TInstruction,TStack>:SimpleNoStackStack<TInstruction,TStack> where TInstruction:BaseSpawnInstruction<TInstruction,TStack> where TStack:BaseSpawnStack<TInstruction,TStack>,new();

	public abstract class TriggerUpdateInstruction<TInstruction,TStack>:SimpleNoStackInstruction<TInstruction,TStack> where TInstruction:TriggerUpdateInstruction<TInstruction,TStack> where TStack:TriggerUpdateStack<TInstruction,TStack>,new()
	{
		public Instruction TriggerProc;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var triggerUpdateAsmInstr = GetAsmInstruction<Disassembler.TriggerUpdateInstruction>();
			TriggerProc = parser.GetTrigger(triggerUpdateAsmInstr.TriggerProc, this);
		}
	}
	public abstract class TriggerUpdateStack<TInstruction,TStack>:SimpleNoStackStack<TInstruction,TStack> where TInstruction:TriggerUpdateInstruction<TInstruction,TStack> where TStack:TriggerUpdateStack<TInstruction,TStack>,new();

	public abstract class VarInstruction:PureProducerInstruction<VarInstruction,VarInstruction.VarStack>
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

		public sealed class VarStack:PureProducerStack<VarInstruction,VarStack>
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
	public sealed class CommandErrorInstruction:NoStackInstruction<CommandErrorInstruction,CommandErrorStack,CommandErrorFlow>
	{
		public sealed override bool Terminal => true;
	}
	public sealed class CommandErrorStack:NoStackStack<CommandErrorInstruction,CommandErrorStack,CommandErrorFlow>
	{
		public override void WriteStatement(Writer writer)
		{
			writer.Write("COMMAND ERROR");
		}
	}

	public sealed class CommandErrorFlow:NoStackFlow<CommandErrorInstruction,CommandErrorStack,CommandErrorFlow>,IFlowTerminal;

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
	public sealed class PrintInstruction:BasePrintInstruction<PrintInstruction,PrintInstruction.PrintStack>
	{
		public sealed class PrintStack:SimpleNoStackStack<PrintInstruction,PrintStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("print ");
				writer.Write(Instruction.Data);
			}
		}
	}

	[Opcode(InstructionOpcode.Number)]
	public sealed class NumberInstruction:PureProducerInstruction<NumberInstruction,NumberInstruction.NumberStack>
	{
		public int Value;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var numberAsmInstr = GetAsmInstruction<Disassembler.NumberInstruction>();

			Value = numberAsmInstr.Value;
		}

		public sealed class NumberStack:PureProducerStack<NumberInstruction,NumberStack>
		{
			public sealed override bool Literal => true;

			//TODO: We will want to move away from this
			private static bool TryGetValue(int fixed12, out double value)
			{
				//Numbers were converted to ASL using the following format:
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
	public sealed class UMinusInstruction:UnaryOperationInstruction<UMinusInstruction,UMinusInstruction.UMinusStack>
	{
		public sealed class UMinusStack:UnaryOperationStack<UMinusInstruction,UMinusStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"-{Value.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Increase)]
	public sealed class IncreaseInstruction:SimplePureConsumerInstruction<IncreaseInstruction,IncreaseInstruction.IncreaseStack>
	{
		public sealed class IncreaseStack:SimplePureConsumerStack<IncreaseInstruction,IncreaseStack>
		{
			protected override int PopCount => 1;

			public VarInstruction.VarStack Address => (VarInstruction.VarStack)Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write(Address.ToExpressionString(ExpressionType.Dereference));
				writer.Write("++");
			}
		}
	}

	[Opcode(InstructionOpcode.Decrease)]
	public sealed class DecreaseInstruction:SimplePureConsumerInstruction<DecreaseInstruction,DecreaseInstruction.DecreaseStack>
	{
		public sealed class DecreaseStack:SimplePureConsumerStack<DecreaseInstruction,DecreaseStack>
		{
			protected override int PopCount => 1;

			public VarInstruction.VarStack Address => (VarInstruction.VarStack)Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write(Address.ToExpressionString(ExpressionType.Dereference));
				writer.Write("--");
			}
		}
	}

	[Opcode(InstructionOpcode.Add)]
	public sealed class AddInstruction:BinaryOperationInstruction<AddInstruction,AddInstruction.AddStack>
	{
		public sealed class AddStack:BinaryOperationStack<AddInstruction,AddStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} + {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Sub)]
	public sealed class SubInstruction:BinaryOperationInstruction<SubInstruction,SubInstruction.SubStack>
	{
		public sealed class SubStack:BinaryOperationStack<SubInstruction,SubStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} - {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Mul)]
	public sealed class MulInstruction:BinaryOperationInstruction<MulInstruction,MulInstruction.MulStack>
	{
		public sealed class MulStack:BinaryOperationStack<MulInstruction,MulStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} * {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Div)]
	public sealed class DivInstruction:BinaryOperationInstruction<DivInstruction,DivInstruction.DivStack>
	{
		public sealed class DivStack:BinaryOperationStack<DivInstruction,DivStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} / {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Equals)]
	public sealed class EqualsInstruction:SimplePureConsumerInstruction<EqualsInstruction,EqualsInstruction.EqualsStack>
	{
		public sealed class EqualsStack:SimplePureConsumerStack<EqualsInstruction,EqualsStack>
		{
			protected override int PopCount => 2;

			public VarInstruction.VarStack Address => (VarInstruction.VarStack)Operands[0];
			public IStackProducer Value => Operands[1];

			//TODO: Value isn't always fixed point, if it is a NumberInstruction, it generally will be.
			public override void WriteStatement(Writer writer)
			{
				writer.Write(Address.ToExpressionString(ExpressionType.Dereference));
				writer.Write(" = ");
				writer.Write(Value.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Compare)]
	public sealed class CompareInstruction:BinaryOperationInstruction<CompareInstruction,CompareInstruction.CompareStack>
	{
		public sealed class CompareStack:BinaryOperationStack<CompareInstruction,CompareStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} = {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} = {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} != {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.LessThan)]
	public sealed class LessThanInstruction:BinaryOperationInstruction<LessThanInstruction,LessThanInstruction.LessThanStack>
	{
		public sealed class LessThanStack:BinaryOperationStack<LessThanInstruction,LessThanStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} < {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} < {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} >= {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.GreaterThan)]
	public sealed class GreaterThanInstruction:BinaryOperationInstruction<GreaterThanInstruction,GreaterThanInstruction.GreaterThanStack>
	{
		public sealed class GreaterThanStack:BinaryOperationStack<GreaterThanInstruction,GreaterThanStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} > {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} > {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} <= {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.SetModel)]
	public sealed class SetModelInstruction:SimplePureConsumerInstruction<SetModelInstruction,SetModelInstruction.SetModelStack>
	{
		public sealed class SetModelStack:SimplePureConsumerStack<SetModelInstruction,SetModelStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Model => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("setmodel ");
				writer.Write(Model.ToExpressionString(ExpressionType.Reference));
			}
		}
	}

	[Opcode(InstructionOpcode.Scale)]
	public sealed class ScaleInstruction:SimplePureConsumerInstruction<ScaleInstruction,ScaleInstruction.ScaleStack>
	{
		public sealed class ScaleStack:SimplePureConsumerStack<ScaleInstruction,ScaleStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Scale => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("scale ");
				writer.Write(Scale.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.ScaleX)]
	public sealed class ScaleXInstruction:SimplePureConsumerInstruction<ScaleXInstruction,ScaleXInstruction.ScaleXStack>
	{
		public sealed class ScaleXStack:SimplePureConsumerStack<ScaleXInstruction,ScaleXStack>
		{
			protected override int PopCount => 1;

			public IStackProducer ScaleX => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("scalex ");
				writer.Write(ScaleX.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.ScaleY)]
	public sealed class ScaleYInstruction:SimplePureConsumerInstruction<ScaleYInstruction,ScaleYInstruction.ScaleYStack>
	{
		public sealed class ScaleYStack:SimplePureConsumerStack<ScaleYInstruction,ScaleYStack>
		{
			protected override int PopCount => 1;

			public IStackProducer ScaleY => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("scaley ");
				writer.Write(ScaleY.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.ScaleZ)]
	public sealed class ScaleZInstruction:SimplePureConsumerInstruction<ScaleZInstruction,ScaleZInstruction.ScaleZStack>
	{
		public sealed class ScaleZStack:SimplePureConsumerStack<ScaleZInstruction,ScaleZStack>
		{
			protected override int PopCount => 1;

			public IStackProducer ScaleZ => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("scalez ");
				writer.Write(ScaleZ.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Shadow)]
	public sealed class ShadowInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.ShadowSize)]
	public sealed class ShadowSizeInstruction:SimplePureConsumerInstruction<ShadowSizeInstruction,ShadowSizeInstruction.ShadowSizeStack>
	{
		public sealed class ShadowSizeStack:SimplePureConsumerStack<ShadowSizeInstruction,ShadowSizeStack>
		{
			protected override int PopCount => 1;

			public IStackProducer ShadowSize => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("ShadowSize ");
				writer.Write(ShadowSize.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.ShadowType)]
	public sealed class ShadowTypeInstruction:SimplePureConsumerInstruction<ShadowTypeInstruction,ShadowTypeInstruction.ShadowTypeStack>
	{
		public sealed class ShadowTypeStack:SimplePureConsumerStack<ShadowTypeInstruction,ShadowTypeStack>
		{
			protected override int PopCount => 1;

			public IStackProducer ShadowType => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("ShadowType ");
				writer.Write(ShadowType.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.Hide)]
	public sealed class HideInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Flash)]
	public sealed class FlashInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Trans)]
	public sealed class TransInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MoveUp)]
	public sealed class MoveUpInstruction:BaseMoveInstruction<MoveUpInstruction,MoveUpInstruction.MoveUpStack>
	{
		public sealed class MoveUpStack:BaseMoveStack<MoveUpInstruction,MoveUpStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveUp ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveDown)]
	public sealed class MoveDownInstruction:BaseMoveInstruction<MoveDownInstruction,MoveDownInstruction.MoveDownStack>
	{
		public sealed class MoveDownStack:BaseMoveStack<MoveDownInstruction,MoveDownStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveDown ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveForward)]
	public sealed class MoveForwardInstruction:BaseMoveInstruction<MoveForwardInstruction,MoveForwardInstruction.MoveForwardStack>
	{
		public sealed class MoveForwardStack:BaseMoveStack<MoveForwardInstruction,MoveForwardStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveForward ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveBackward)]
	public sealed class MoveBackwardInstruction:BaseMoveInstruction<MoveBackwardInstruction,MoveBackwardInstruction.MoveBackwardStack>
	{
		public sealed class MoveBackwardStack:BaseMoveStack<MoveBackwardInstruction,MoveBackwardStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveBackward ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveLeft)]
	public sealed class MoveLeftInstruction:BaseMoveInstruction<MoveLeftInstruction,MoveLeftInstruction.MoveLeftStack>
	{
		public sealed class MoveLeftStack:BaseMoveStack<MoveLeftInstruction,MoveLeftStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveLeft ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveRight)]
	public sealed class MoveRightInstruction:BaseMoveInstruction<MoveRightInstruction,MoveRightInstruction.MoveRightStack>
	{
		public sealed class MoveRightStack:BaseMoveStack<MoveRightInstruction,MoveRightStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveRight ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnRight)]
	public sealed class TurnRightInstruction:BaseMoveInstruction<TurnRightInstruction,TurnRightInstruction.TurnRightStack>
	{
		public sealed class TurnRightStack:BaseMoveStack<TurnRightInstruction,TurnRightStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnRight ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnLeft)]
	public sealed class TurnLeftInstruction:BaseMoveInstruction<TurnLeftInstruction,TurnLeftInstruction.TurnLeftStack>
	{
		public sealed class TurnLeftStack:BaseMoveStack<TurnLeftInstruction,TurnLeftStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnLeft ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TiltLeft)]
	public sealed class TiltLeftInstruction:BaseMoveInstruction<TiltLeftInstruction,TiltLeftInstruction.TiltLeftStack>
	{
		public sealed class TiltLeftStack:BaseMoveStack<TiltLeftInstruction,TiltLeftStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("TiltLeft ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TiltRight)]
	public sealed class TiltRightInstruction:BaseMoveInstruction<TiltRightInstruction,TiltRightInstruction.TiltRightStack>
	{
		public sealed class TiltRightStack:BaseMoveStack<TiltRightInstruction,TiltRightStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("TiltRight ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TiltForward)]
	public sealed class TiltForwardInstruction:BaseMoveInstruction<TiltForwardInstruction,TiltForwardInstruction.TiltForwardStack>
	{
		public sealed class TiltForwardStack:BaseMoveStack<TiltForwardInstruction,TiltForwardStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("TiltForward ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TiltBackward)]
	public sealed class TiltBackwardInstruction:BaseMoveInstruction<TiltBackwardInstruction,TiltBackwardInstruction.TiltBackwardStack>
	{
		public sealed class TiltBackwardStack:BaseMoveStack<TiltBackwardInstruction,TiltBackwardStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("TiltBackward ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnToPlayerX)]
	public sealed class TurnToPlayerXInstruction:SimplePureConsumerInstruction<TurnToPlayerXInstruction,TurnToPlayerXInstruction.TurnToPlayerXStack>
	{
		public sealed class TurnToPlayerXStack:SimplePureConsumerStack<TurnToPlayerXInstruction,TurnToPlayerXStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnToPlayerX ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnToPlayerY)]
	public sealed class TurnToPlayerYInstruction:SimplePureConsumerInstruction<TurnToPlayerYInstruction,TurnToPlayerYInstruction.TurnToPlayerYStack>
	{
		public sealed class TurnToPlayerYStack:SimplePureConsumerStack<TurnToPlayerYInstruction,TurnToPlayerYStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnToPlayerY ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnToPlayerXY)]
	public sealed class TurnToPlayerXYInstruction:SimplePureConsumerInstruction<TurnToPlayerXYInstruction,TurnToPlayerXYInstruction.TurnToPlayerXYStack>
	{
		public sealed class TurnToPlayerXYStack:SimplePureConsumerStack<TurnToPlayerXYInstruction,TurnToPlayerXYStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnToPlayerXY ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnToX)]
	public sealed class TurnToXInstruction:SimplePureConsumerInstruction<TurnToXInstruction,TurnToXInstruction.TurnToXStack>
	{
		public sealed class TurnToXStack:SimplePureConsumerStack<TurnToXInstruction,TurnToXStack>
		{
			protected override int PopCount => 4;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];
			public IStackProducer MaxTurnSpeed => Operands[3];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnToX ");
				writer.Write(X.ToFxString());
				writer.Write(", ");
				writer.Write(Y.ToFxString());
				writer.Write(", ");
				writer.Write(Z.ToFxString());
				writer.Write(", ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnToY)]
	public sealed class TurnToYInstruction:SimplePureConsumerInstruction<TurnToYInstruction,TurnToYInstruction.TurnToYStack>
	{
		public sealed class TurnToYStack:SimplePureConsumerStack<TurnToYInstruction,TurnToYStack>
		{
			protected override int PopCount => 4;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];
			public IStackProducer MaxTurnSpeed => Operands[3];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnToY ");
				writer.Write(X.ToFxString());
				writer.Write(", ");
				writer.Write(Y.ToFxString());
				writer.Write(", ");
				writer.Write(Z.ToFxString());
				writer.Write(", ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TurnToXY)]
	public sealed class TurnToXYInstruction:SimplePureConsumerInstruction<TurnToXYInstruction,TurnToXYInstruction.TurnToXYStack>
	{
		public sealed class TurnToXYStack:SimplePureConsumerStack<TurnToXYInstruction,TurnToXYStack>
		{
			protected override int PopCount => 4;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];
			public IStackProducer MaxTurnSpeed => Operands[3];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnToXY ");
				writer.Write(X.ToFxString());
				writer.Write(", ");
				writer.Write(Y.ToFxString());
				writer.Write(", ");
				writer.Write(Z.ToFxString());
				writer.Write(", ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Wobble)]
	public sealed class WobbleInstruction:SimplePureConsumerInstruction<WobbleInstruction,WobbleInstruction.WobbleStack>
	{
		public sealed class WobbleStack:SimplePureConsumerStack<WobbleInstruction,WobbleStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("Wobble ");
				writer.Write(Value.ToExpressionString());
			}
		}
	}

	[Opcode(InstructionOpcode.ReSetPos)]
	public sealed class ReSetPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetPos)]
	public sealed class SetPosInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Jump)]
	public sealed class JumpInstruction:BaseJumpInstruction<JumpInstruction,JumpInstruction.JumpStack,JumpInstruction.JumpFlow>
	{
		public sealed class JumpStack:BaseJumpStack<JumpInstruction,JumpStack,JumpFlow>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("goto ");
				writer.Write(Instruction.Destination.AsmLabel.GetLabel());
				writer.Write(" $ DONE");
			}
		}
		public sealed class JumpFlow:BaseJumpFlow<JumpInstruction,JumpStack,JumpFlow>;
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
	public sealed class WPTurnToXInstruction:SimplePureConsumerInstruction<WPTurnToXInstruction,WPTurnToXInstruction.WPTurnToXStack>
	{
		public sealed class WPTurnToXStack:SimplePureConsumerStack<WPTurnToXInstruction,WPTurnToXStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("WPTurnToX ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.WPTurnToY)]
	public sealed class WPTurnToYInstruction:SimplePureConsumerInstruction<WPTurnToYInstruction,WPTurnToYInstruction.WPTurnToYStack>
	{
		public sealed class WPTurnToYStack:SimplePureConsumerStack<WPTurnToYInstruction,WPTurnToYStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("WPTurnToY ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.WPTurnToXY)]
	public sealed class WPTurnToXYInstruction:SimplePureConsumerInstruction<WPTurnToXYInstruction,WPTurnToXYInstruction.WPTurnToXYStack>
	{
		public sealed class WPTurnToXYStack:SimplePureConsumerStack<WPTurnToXYInstruction,WPTurnToXYStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("WPTurnToXY ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.AnimPlay)]
	public sealed class AnimPlayInstruction:SimplePureConsumerInstruction<AnimPlayInstruction,AnimPlayInstruction.AnimPlayStack>
	{
		public sealed class AnimPlayStack:SimplePureConsumerStack<AnimPlayInstruction,AnimPlayStack>
		{
			protected override int PopCount => 1;

			public IStackProducer AnimationAddr => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("AnimPlay ");
				writer.Write(AnimationAddr.ToExpressionString(ExpressionType.Reference));
			}
		}
	}

	[Opcode(InstructionOpcode.AnimStop)]
	public sealed class AnimStopInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.AnimClear)]
	public sealed class AnimClearInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.AnimSetSpeed)]
	public sealed class AnimSetSpeedInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollisionType)]
	public sealed class CollisionTypeInstruction:SimplePureConsumerInstruction<CollisionTypeInstruction,CollisionTypeInstruction.CollisionTypeStack>
	{
		public sealed class CollisionTypeStack:SimplePureConsumerStack<CollisionTypeInstruction,CollisionTypeStack>
		{
			protected override int PopCount => 1;

			public IStackProducer CollisionType => Operands[0];

			//TODO: This may use custom values for it.
			public override void WriteStatement(Writer writer)
			{
				writer.Write("CollisionType ");
				writer.Write(CollisionType.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.CollRadius)]
	public sealed class CollisionRadiusInstruction:SimplePureConsumerInstruction<CollisionRadiusInstruction,CollisionRadiusInstruction.CollisionRadiusStack>
	{
		public sealed class CollisionRadiusStack:SimplePureConsumerStack<CollisionRadiusInstruction,CollisionRadiusStack>
		{
			protected override int PopCount => 1;

			public IStackProducer CollRadius => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CollisionRadius ");
				writer.Write(CollRadius.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.CollHeight)]
	public sealed class CollisionHeightInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollExtent)]
	public sealed class CollisionExtentInstruction:SimplePureConsumerInstruction<CollisionExtentInstruction,CollisionExtentInstruction.CollisionExtentStack>
	{
		public sealed class CollisionExtentStack:SimplePureConsumerStack<CollisionExtentInstruction,CollisionExtentStack>
		{
			protected override int PopCount => 1;

			public IStackProducer CollExtent => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CollisionExtent ");
				writer.Write(CollExtent.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.CollView)]
	public sealed class CollisionViewInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollPoints)]
	public sealed class CollisionPointsInstruction:SimplePureConsumerInstruction<CollisionPointsInstruction,CollisionPointsInstruction.CollisionPointsStack>
	{
		public sealed class CollisionPointsStack:SimplePureConsumerStack<CollisionPointsInstruction,CollisionPointsStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CollisionPoints ");
				writer.Write(Value.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.CollSetPoint)]
	public sealed class CollSetPointInstruction:SimplePureConsumerInstruction<CollSetPointInstruction,CollSetPointInstruction.CollSetPointStack>
	{
		public sealed class CollSetPointStack:SimplePureConsumerStack<CollSetPointInstruction,CollSetPointStack>
		{
			protected override int PopCount => 4;

			public IStackProducer PointIndex => Operands[0];
			public IStackProducer X => Operands[1];
			public IStackProducer Y => Operands[2];
			public IStackProducer Z => Operands[3];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CollisionSetPoint ");
				writer.Write(PointIndex.ToFxString());
				writer.Write(", ");
				writer.Write(X.ToFxString());
				writer.Write(", ");
				writer.Write(Y.ToFxString());
				writer.Write(", ");
				writer.Write(Z.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.CreateTrigger)]
	public sealed class CreateTriggerInstruction:SimpleNoStackInstruction<CreateTriggerInstruction,CreateTriggerInstruction.CreateTriggerStack>
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

		public sealed class CreateTriggerStack:SimpleNoStackStack<CreateTriggerInstruction,CreateTriggerStack>
		{
			public override void WriteStatement(Writer writer)
			{
				var ret = new StringBuilder();
				writer.Write("CreateTrigger ");
				writer.Write(Instruction.Type.ToString());
				writer.Write(' ');
				switch(Instruction.Type)
				{
					case TriggerType.Every or TriggerType.In://Frame Count
					case TriggerType.Anim://Animation Number
					case TriggerType.WhenNear or TriggerType.WhenFar://Distance
						writer.WriteInt(Instruction.Arg);
						writer.Write(' ');
						break;
				}
				writer.Write(Instruction.TriggerProc.AsmLabel.SubroutineName());
			}
		}
	}

	[Opcode(InstructionOpcode.KillTrigger)]
	public sealed class KillTriggerInstruction:TriggerUpdateInstruction<KillTriggerInstruction,KillTriggerInstruction.KillTriggerStack>
	{
		public sealed class KillTriggerStack:TriggerUpdateStack<KillTriggerInstruction,KillTriggerStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("KillTrigger ");
				writer.Write(Instruction.TriggerProc.AsmLabel.SubroutineName());
			}
		}
	}

	[Opcode(InstructionOpcode.HoldTriggers)]
	public sealed class HoldTriggersInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ReleaseTriggers)]
	public sealed class ReleaseTriggersInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.HoldTrigger)]
	public sealed class HoldTriggerInstruction:TriggerUpdateInstruction<HoldTriggerInstruction,HoldTriggerInstruction.HoldTriggerStack>
	{
		public sealed class HoldTriggerStack:TriggerUpdateStack<HoldTriggerInstruction,HoldTriggerStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("HoldTrigger ");
				writer.Write(Instruction.TriggerProc.AsmLabel.SubroutineName());
			}
		}
	}

	[Opcode(InstructionOpcode.ReleaseTrigger)]
	public sealed class ReleaseTriggerInstruction:TriggerUpdateInstruction<ReleaseTriggerInstruction,ReleaseTriggerInstruction.ReleaseTriggerStack>
	{
		public sealed class ReleaseTriggerStack:TriggerUpdateStack<ReleaseTriggerInstruction,ReleaseTriggerStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("ReleaseTrigger ");
				writer.Write(Instruction.TriggerProc.AsmLabel.SubroutineName());
			}
		}
	}

	[Opcode(InstructionOpcode.Wait)]
	public sealed class WaitInstruction:SimplePureConsumerInstruction<WaitInstruction,WaitInstruction.WaitStack>
	{
		public sealed class WaitStack:SimplePureConsumerStack<WaitInstruction,WaitStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("Wait ");
				writer.Write(Value.ToFxString());
			}
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
	public sealed class SpawnInstruction:BaseSpawnInstruction<SpawnInstruction,SpawnInstruction.SpawnStack>
	{
		public sealed class SpawnStack:BaseSpawnStack<SpawnInstruction,SpawnStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("Spawn ");
				writer.Write(Instruction.SpawnStratProc.AsmLabel.SubroutineName());
				writer.Write(", ");
				writer.WriteInt(Instruction.LocalVarsToPop);
				writer.Write(", ");
				writer.WriteInt(Instruction.LocalCount);
				writer.Write(", ");
				writer.WriteInt(Instruction.TriggerCount);
				writer.Write(", ");
				writer.WriteInt(Instruction.CollisionSize);
				writer.Write(", ");
				writer.WriteInt(Instruction.CollisionBoneCount);
			}
		}
	}

	[Opcode(InstructionOpcode.SpawnFrom)]
	public sealed class SpawnFromInstruction:BaseSpawnInstruction<SpawnFromInstruction,SpawnFromInstruction.SpawnFromStack>
	{
		public int BoneToSpawnFrom;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);

			var spawnFromAsmInstr = GetAsmInstruction<Disassembler.SpawnFromInstruction>();
			BoneToSpawnFrom = spawnFromAsmInstr.BoneToSpawnFrom;
		}

		public sealed class SpawnFromStack:BaseSpawnStack<SpawnFromInstruction,SpawnFromStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("SpawnFrom ");
				writer.Write(Instruction.SpawnStratProc.AsmLabel.SubroutineName());
				writer.Write(", ");
				writer.WriteInt(Instruction.LocalVarsToPop);
				writer.Write(", ");
				writer.WriteInt(Instruction.LocalCount);
				writer.Write(", ");
				writer.WriteInt(Instruction.TriggerCount);
				writer.Write(", ");
				writer.WriteInt(Instruction.CollisionSize);
				writer.Write(", ");
				writer.WriteInt(Instruction.CollisionBoneCount);
				writer.Write(", ");
				writer.WriteInt(Instruction.BoneToSpawnFrom);
			}
		}
	}

	[Opcode(InstructionOpcode.Link)]
	public sealed class LinkInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Unlink)]
	public sealed class UnlinkInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SoundShift)]
	public sealed class SoundShiftInstruction:SimplePureConsumerInstruction<SoundShiftInstruction,SoundShiftInstruction.SoundShiftStack>
	{
		public sealed class SoundShiftStack:SimplePureConsumerStack<SoundShiftInstruction,SoundShiftStack>
		{
			protected override int PopCount => 2;

			public IStackProducer Channel => Operands[0];
			public IStackProducer Pitch => Operands[1];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundShift ");
				writer.Write(Channel.ToIntStr());
				writer.Write(", ");
				writer.Write(Pitch.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.SoundStop)]
	public sealed class SoundStopInstruction:SimplePureConsumerInstruction<SoundStopInstruction,SoundStopInstruction.SoundStopStack>
	{
		public sealed class SoundStopStack:SimplePureConsumerStack<SoundStopInstruction,SoundStopStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Channel => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundStop ");
				writer.Write(Channel.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.CdPlay)]
	public sealed class CdPlayInstruction:SimplePureConsumerInstruction<CdPlayInstruction,CdPlayInstruction.CdPlayStack>
	{
		public sealed class CdPlayStack:SimplePureConsumerStack<CdPlayInstruction,CdPlayStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MusicId => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CdPlay ");
				writer.Write(MusicId.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MidiLoop)]
	public sealed class MidiLoopInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MidiVolume)]
	public sealed class MidiVolumeInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CdFade)]
	public sealed class CdFadeInstruction:SimplePureConsumerInstruction<CdFadeInstruction,CdFadeInstruction.CdFadeStack>
	{
		public sealed class CdFadeStack:SimplePureConsumerStack<CdFadeInstruction,CdFadeStack>
		{
			protected override int PopCount => 1;

			public IStackProducer TimeLength => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CdFade ");
				writer.Write(TimeLength.ToFxString());
			}
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

	public abstract class BaseCollisionInstruction:SimpleNoStackInstruction<BaseCollisionInstruction,BaseCollisionInstruction.BaseCollisionStack>
	{
		public readonly bool NewState;
		public uint CollisionFlag;

		public BaseCollisionInstruction(bool newState)
		{
			NewState = newState;
		}

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var collisionFlagAsmInstr = GetAsmInstruction<Disassembler.CollisionFlagInstruction>();
			CollisionFlag = collisionFlagAsmInstr.CollisionType;
		}

		public sealed class BaseCollisionStack:SimpleNoStackStack<BaseCollisionInstruction,BaseCollisionStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write(Instruction.NewState ? "collision on " : "collision off ");
				writer.WriteInt((int)Instruction.CollisionFlag);
			}
		}
	}

	[Opcode(InstructionOpcode.CollisionOn)]
	public sealed class CollisionOnInstruction():BaseCollisionInstruction(true);

	[Opcode(InstructionOpcode.CollisionOff)]
	public sealed class CollisionOffInstruction():BaseCollisionInstruction(false);

	[Opcode(InstructionOpcode.CollisionOffAll)]
	public sealed class CollisionOffAllInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SoundPlay3)]
	public sealed class SoundPlay3Instruction:SimplePureConsumerInstruction<SoundPlay3Instruction,SoundPlay3Instruction.SoundPlay3Stack>
	{
		public sealed class SoundPlay3Stack:SimplePureConsumerStack<SoundPlay3Instruction,SoundPlay3Stack>
		{
			protected override int PopCount => 3;

			public IStackProducer SoundIndex => Operands[0];
			public IStackProducer Volume => Operands[1];
			public IStackProducer Flags => Operands[2];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundPlay ");
				writer.Write(SoundIndex.ToIntStr());
				writer.Write(", ");
				writer.Write(Volume.ToFxString());
				writer.Write(", ");
				writer.Write(Flags.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.SoundPlay4)]
	public sealed class SoundPlay4Instruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SoundPlay3ASS)]
	public sealed class SoundPlay3AssignmentInstruction:BaseExpressionInstruction<SoundPlay3AssignmentInstruction,SoundPlay3AssignmentInstruction.SoundPlay3AssignmentStack>
	{
		public sealed class SoundPlay3AssignmentStack:BaseExpressionStack<SoundPlay3AssignmentInstruction,SoundPlay3AssignmentStack>
		{
			protected override int PopCount => 3;

			public IStackProducer SoundIndex => Operands[0];
			public IStackProducer Volume => Operands[1];
			public IStackProducer Flags => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToIntStr()}, {Volume.ToFxString()}, {Flags.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SoundPlay4ASS)]
	public sealed class SoundPlay4AssignmentInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Int)]
	public sealed class IntInstruction:UnaryOperationInstruction<IntInstruction,IntInstruction.IntStack>
	{
		public sealed class IntStack:UnaryOperationStack<IntInstruction,IntStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"int({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Sin)]
	public sealed class SinInstruction:UnaryOperationInstruction<SinInstruction,SinInstruction.SinStack>
	{
		public sealed class SinStack:UnaryOperationStack<SinInstruction,SinStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"sin({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Cos)]
	public sealed class CosInstruction:UnaryOperationInstruction<CosInstruction,CosInstruction.CosStack>
	{
		public sealed class CosStack:UnaryOperationStack<CosInstruction,CosStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"cos({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Not)]
	public sealed class NotInstruction:UnaryOperationInstruction<NotInstruction,NotInstruction.NotStack>
	{
		public sealed class NotStack:UnaryOperationStack<NotInstruction,NotStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"NOT {Value.ToExpressionString()}";
		}
	}

	[Opcode(InstructionOpcode.Pop)]
	public sealed class PopInstruction:SimplePureConsumerInstruction<PopInstruction,PopInstruction.PopStack>
	{
		public sealed class PopStack:SimplePureConsumerStack<PopInstruction,PopStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Discarded => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("Pop ");
				writer.Write(Discarded.ToExpressionString());
			}
		}
	}

	[Opcode(InstructionOpcode.Address)]
	public sealed class AddressInstruction:PureProducerInstruction<AddressInstruction,AddressInstruction.AddressStack>
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

		public sealed class AddressStack:PureProducerStack<AddressInstruction,AddressStack>
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
	public sealed class JsrInstruction:BaseJumpSubroutineInstruction<JsrInstruction,JsrInstruction.JsrStack>
	{
		public sealed class JsrStack:BaseJumpSubroutineStack<JsrInstruction,JsrStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("proc ");
				writer.Write(Instruction.Proc.AsmLabel.SubroutineName());
				writer.Write(" $ DONE");
			}
		}
	}

	[Opcode(InstructionOpcode.JsrImm)]
	public sealed class JsrImmInstruction:BaseJumpSubroutineInstruction<JsrImmInstruction,JsrImmInstruction.JsrImmStack>
	{
		public sealed class JsrImmStack:BaseJumpSubroutineStack<JsrImmInstruction,JsrImmStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("proc ");
				writer.Write(Instruction.Proc.AsmLabel.SubroutineName());
				writer.Write(" $ IMM");
			}
		}
	}

	[Opcode(InstructionOpcode.Return)]
	public sealed class ReturnInstruction:NoStackInstruction<ReturnInstruction,ReturnInstruction.ReturnStack,ReturnInstruction.ReturnFlow>
	{
		//Technically pops a value but we aren't counting it because it is pushed outisde the subroutine/trigger.

		public sealed override bool Terminal => true;

		public sealed class ReturnStack:NoStackStack<ReturnInstruction,ReturnStack,ReturnFlow>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("Return");
			}
		}

		public sealed class ReturnFlow:NoStackFlow<ReturnInstruction,ReturnStack,ReturnFlow>,IFlowTerminal;
	}

	[Opcode(InstructionOpcode.Beq)]
	public sealed class BeqInstruction:BranchInstruction<BeqInstruction,BeqInstruction.BeqStack>
	{
		public sealed class BeqStack:BranchStack<BeqInstruction,BeqStack>
		{
			//Branch if equal to zero
			public override void WriteStatement(Writer writer)
			{
				writer.Write("if ");
				writer.Write(Condition.ToConditionStr(false));
				writer.OpenLine(" then");
				writer.Write("goto ");
				writer.WriteLine(Instruction.ConditionalDest.AsmLabel.GetLabel());
				writer.CloseLine("endif $ DONE");
			}
		}
	}

	[Opcode(InstructionOpcode.Bne)]
	public sealed class BneInstruction:BranchInstruction<BneInstruction,BneInstruction.BneStack>
	{
		public sealed class BneStack:BranchStack<BneInstruction,BneStack>
		{
			//Branch if not equal to zero
			public override void WriteStatement(Writer writer)
			{
				writer.Write("if ");
				writer.Write(Condition.ToConditionStr(true));
				writer.OpenLine(" then");
				writer.Write("goto ");
				writer.WriteLine(Instruction.ConditionalDest.AsmLabel.GetLabel());
				writer.CloseLine("endif $ DONE");
			}
		}
	}

	[Opcode(InstructionOpcode.BeqImm)]
	public sealed class BeqImmInstruction:BranchInstruction<BeqImmInstruction,BeqImmInstruction.BeqImmStack>
	{
		public sealed class BeqImmStack:BranchStack<BeqImmInstruction,BeqImmStack>
		{
			//Branch if equal to zero
			public override void WriteStatement(Writer writer)
			{
				writer.Write("if ");
				writer.Write(Condition.ToConditionStr(false));
				writer.OpenLine(" then");
				writer.Write("goto ");
				writer.WriteLine(Instruction.ConditionalDest.AsmLabel.GetLabel());
				writer.CloseLine("endif $ IMM");
			}
		}
	}

	[Opcode(InstructionOpcode.BneImm)]
	public sealed class BneImmInstruction:BranchInstruction<BneImmInstruction,BneImmInstruction.BneImmStack>
	{
		public sealed class BneImmStack:BranchStack<BneImmInstruction,BneImmStack>
		{
			//Branch if not equal to zero
			public override void WriteStatement(Writer writer)
			{
				writer.Write("if ");
				writer.Write(Condition.ToConditionStr(true));
				writer.OpenLine(" then");
				writer.Write("goto ");
				writer.WriteLine(Instruction.ConditionalDest.AsmLabel.GetLabel());
				writer.CloseLine("endif $ IMM");
			}
		}
	}

	[Opcode(InstructionOpcode.JumpImm)]
	public sealed class JumpImmInstruction:BaseJumpInstruction<JumpImmInstruction,JumpImmInstruction.JumpImmStack,JumpImmInstruction.JumpImmFlow>
	{
		public sealed class JumpImmStack:BaseJumpStack<JumpImmInstruction,JumpImmStack,JumpImmFlow>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("goto ");
				writer.Write(Instruction.Destination.AsmLabel.GetLabel());
				writer.Write(" $ IMM");
			}
		}

		public sealed class JumpImmFlow:BaseJumpFlow<JumpImmInstruction,JumpImmStack,JumpImmFlow>;
	}

	[Opcode(InstructionOpcode.EndStrat)]
	public sealed class EndStratInstruction:NoStackInstruction<EndStratInstruction,EndStratInstruction.EndStratStack,EndStratInstruction.EndStratFlow>
	{
		public sealed override bool Terminal => true;

		public sealed class EndStratStack:NoStackStack<EndStratInstruction,EndStratStack,EndStratFlow>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("EndStrat");
			}
		}

		public sealed class EndStratFlow:NoStackFlow<EndStratInstruction,EndStratStack,EndStratFlow>,IFlowTerminal;
	}

	[Opcode(InstructionOpcode.IsPlayer)]
	public sealed class IsPlayerInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.And)]
	public sealed class AndInstruction:BinaryOperationInstruction<AndInstruction,AndInstruction.AndStack>
	{
		public sealed class AndStack:BinaryOperationStack<AndInstruction,AndStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToExpressionString()} AND {ValueB.ToExpressionString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToConditionStr(true)} AND {ValueB.ToConditionStr(true)})" : $"({ValueA.ToConditionStr(false)} OR {ValueB.ToConditionStr(false)})";
		}
	}

	[Opcode(InstructionOpcode.Or)]
	public sealed class OrInstruction:BinaryOperationInstruction<OrInstruction,OrInstruction.OrStack>
	{
		//TODO: This is bitwise |, not logical ||.

		public sealed class OrStack:BinaryOperationStack<OrInstruction,OrStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToExpressionString()} OR {ValueB.ToExpressionString()})";
		}
	}

	[Opcode(InstructionOpcode.Index_Jump)]
	public sealed class IndexJumpInstruction:PureConsumerInstruction<IndexJumpInstruction,IndexJumpInstruction.SwitchStack,IndexJumpInstruction.SwitchFlow>
	{
		public (int[] Comparands,Instruction Destination)[] Cases;

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

		public sealed class SwitchStack:PureConsumerStack<IndexJumpInstruction,SwitchStack,SwitchFlow>
		{
			protected override int PopCount => 1;

			public IStackProducer Value => Operands[0];

			public override void Analyze(StackAnalyzer stack)
			{
				base.Analyze(stack);

				foreach(var c in Instruction.Cases)
				{
					stack.AddDest(c.Destination);
				}
			}

			public override void WriteStatement(Writer writer)
			{
				//TODO: This was commented out. Might fail.

				writer.Write("switch ");
				writer.Write(Value.ToExpressionString());
				writer.OpenLine();
				foreach(var c in Instruction.Cases)
				{
					foreach(var comparand in c.Comparands)
					{
						writer.Write("case ");
						writer.WriteInt(comparand);
						writer.WriteLine();
					}
					writer.OpenLine();
					writer.Write("goto ");
					writer.WriteLine(c.Destination.AsmLabel.GetLabel());
					writer.CloseLine("endcase");
				}
				writer.CloseLine("endswitch");
			}
		}

		public sealed class SwitchFlow:PureConsumerFlow<IndexJumpInstruction,SwitchStack,SwitchFlow>,IFlowControl
		{
			public IStackStatement ControlStatement => Instruction.StackOperation;

			public IReadOnlyList<FlowStatement> FlowCaseDestinations => FlowDestinations;

			public void Analyze(FlowAnalyzer flow)
			{
				for(int i=0; i<Instruction.Cases.Length; i++)
				{
					flow.AddDest(this, Instruction.Cases[i].Destination);
				}
			}
		}
	}

	[Opcode(InstructionOpcode.BitwiseAnd)]
	public sealed class BitwiseAndInstruction:BinaryOperationInstruction<BitwiseAndInstruction,BitwiseAndInstruction.BitwiseAndStack>
	{
		public sealed class BitwiseAndStack:BinaryOperationStack<BitwiseAndInstruction,BitwiseAndStack>
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
	public sealed class ObjectJumpInstruction:SimplePureConsumerInstruction<ObjectJumpInstruction,ObjectJumpInstruction.ObjectJumpStack>
	{
		public sealed class ObjectJumpStack:SimplePureConsumerStack<ObjectJumpInstruction,ObjectJumpStack>
		{
			protected override int PopCount => 1;

			public IStackProducer VerticalVelocity => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("ObjectJump ");
				writer.Write(VerticalVelocity.ToFxString());
			}
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
	public sealed class NotEqualInstruction:BinaryOperationInstruction<NotEqualInstruction,NotEqualInstruction.NotEqualStack>
	{
		public sealed class NotEqualStack:BinaryOperationStack<NotEqualInstruction,NotEqualStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} != {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} != {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} = {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.ShiftLeft)]
	public sealed class ShiftLeftInstruction:BinaryOperationInstruction<ShiftLeftInstruction,ShiftLeftInstruction.ShiftLeftStack>
	{
		public sealed class ShiftLeftStack:BinaryOperationStack<ShiftLeftInstruction,ShiftLeftStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} << {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.ShiftRight)]
	public sealed class ShiftRightInstruction:BinaryOperationInstruction<ShiftRightInstruction,ShiftRightInstruction.ShiftRightStack>
	{
		public sealed class ShiftRightStack:BinaryOperationStack<ShiftRightInstruction,ShiftRightStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} >> {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.AnimAdvance)]
	public sealed class AnimAdvanceInstruction:SimplePureConsumerInstruction<AnimAdvanceInstruction,AnimAdvanceInstruction.AnimAdvanceStack>
	{
		public sealed class AnimAdvanceStack:SimplePureConsumerStack<AnimAdvanceInstruction,AnimAdvanceStack>
		{
			protected override int PopCount => 1;

			public IStackProducer DesiredIndex => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("AnimAdvance ");
				writer.Write(DesiredIndex.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.GreaterEqual)]
	public sealed class GreaterEqualInstruction:BinaryOperationInstruction<GreaterEqualInstruction,GreaterEqualInstruction.GreaterEqualStack>
	{
		public sealed class GreaterEqualStack:BinaryOperationStack<GreaterEqualInstruction,GreaterEqualStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} >= {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} >= {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} < {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.LessEqual)]
	public sealed class LessEqualInstruction:BinaryOperationInstruction<LessEqualInstruction,LessEqualInstruction.LessEqualStack>
	{
		public sealed class LessEqualStack:BinaryOperationStack<LessEqualInstruction,LessEqualStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} <= {ValueB.ToFxString()})";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? $"({ValueA.ToFxString()} <= {ValueB.ToFxString()})" : $"({ValueA.ToFxString()} > {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Rnd)]
	public sealed class RndInstruction:BaseExpressionInstruction<RndInstruction,RndInstruction.RndStack>
	{
		public sealed class RndStack:BaseExpressionStack<RndInstruction,RndStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxExclusive => Operands[0];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"rnd({MaxExclusive.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Blink)]
	public sealed class BlinkInstruction:SimplePureConsumerInstruction<BlinkInstruction,BlinkInstruction.BlinkStack>
	{
		public int Count;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var blinkAsmInstr = GetAsmInstruction<Disassembler.BlinkInstruction>();

			Count = blinkAsmInstr.Count;
		}

		public sealed class BlinkStack:SimplePureConsumerStack<BlinkInstruction,BlinkStack>
		{
			protected override int PopCount => Instruction.Count;

			//This is just the operands but in reverse
			//TODO: Cache this
			public IStackProducer[] Bones => Operands.Reverse().ToArray();

			public override void WriteStatement(Writer writer)
			{
				writer.Write("blink ");
				writer.Write(string.Join(", ", Bones.Select(b => b.ToFxString())));
			}
		}
	}

	[Opcode(InstructionOpcode.LoseHeart)]
	public sealed class LoseHeartInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ResetToCheckPoint)]
	public sealed class ResetToCheckPointInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ForceCollision)]
	public sealed class ForceCollisionInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.TurnFromPlayerY)]
	public sealed class TurnFromPlayerYInstruction:SimplePureConsumerInstruction<TurnFromPlayerYInstruction,TurnFromPlayerYInstruction.TurnFromPlayerYStack>
	{
		public sealed class TurnFromPlayerYStack:SimplePureConsumerStack<TurnFromPlayerYInstruction,TurnFromPlayerYStack>
		{
			protected override int PopCount => 1;

			public IStackProducer MaxTurnSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TurnFromPlayerY ");
				writer.Write(MaxTurnSpeed.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.PlayerAttack)]
	public sealed class PlayerAttackInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Rumble)]
	public sealed class RumbleInstruction:SimplePureConsumerInstruction<RumbleInstruction,RumbleInstruction.RumbleStack>
	{
		public sealed class RumbleStack:SimplePureConsumerStack<RumbleInstruction,RumbleStack>
		{
			protected override int PopCount => 2;

			public IStackProducer Rumble => Operands[0];
			public IStackProducer RumbleDecay => Operands[1];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("Rumble ");
				writer.Write(Rumble.ToFxString());
				writer.Write(", ");
				writer.Write(RumbleDecay.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Vibrate)]
	public sealed class VibrateInstruction:SimplePureConsumerInstruction<VibrateInstruction,VibrateInstruction.VibrateStack>
	{
		public sealed class VibrateStack:SimplePureConsumerStack<VibrateInstruction,VibrateStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Vibrate => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("Vibrate ");
				writer.Write(Vibrate.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.SuspendIfTooFar)]
	public sealed class SuspendIfTooFarInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.CollisionBone)]
	public sealed class CollisionBoneInstruction:SimplePureConsumerInstruction<CollisionBoneInstruction,CollisionBoneInstruction.CollisionBoneStack>
	{
		public sealed class CollisionBoneStack:SimplePureConsumerStack<CollisionBoneInstruction,CollisionBoneStack>
		{
			protected override int PopCount => 2;

			public IStackProducer Bone => Operands[0];
			public IStackProducer Radius => Operands[1];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CollisionBone ");
				writer.Write(Bone.ToFxString());
				writer.Write(", ");
				writer.Write(Radius.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.UseBone)]
	public sealed class UseBoneInstruction:SimplePureConsumerInstruction<UseBoneInstruction,UseBoneInstruction.UseBoneStack>
	{
		public sealed class UseBoneStack:SimplePureConsumerStack<UseBoneInstruction,UseBoneStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Frame => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("UseBone ");
				writer.Write(Frame.ToFxString());
			}
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
	public sealed class GainCrystalInstruction:SimplePureConsumerInstruction<GainCrystalInstruction,GainCrystalInstruction.GainCrystalStack>
	{
		public sealed class GainCrystalStack:SimplePureConsumerStack<GainCrystalInstruction,GainCrystalStack>
		{
			protected override int PopCount => 1;

			public IStackProducer CrystalType => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("GainCrystal ");
				writer.Write(CrystalType.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Cutscene)]
	public sealed class CutsceneInstruction:SimplePureConsumerInstruction<CutsceneInstruction,CutsceneInstruction.CutsceneStack>
	{
		public sealed class CutsceneStack:SimplePureConsumerStack<CutsceneInstruction,CutsceneStack>
		{
			protected override int PopCount => 1;

			public IStackProducer CutsceneAddr => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("Cutscene ");
				writer.Write(CutsceneAddr.ToExpressionString(ExpressionType.Reference));
			}
		}
	}

	[Opcode(InstructionOpcode.Inventory)]
	public sealed class InventoryInstruction:PureProducerInstruction<InventoryInstruction,InventoryInstruction.InventoryStack>
	{
		//Item count
		public int Item;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var inventoryAsmInstr = GetAsmInstruction<Disassembler.ItemCountInstruction>();

			Item = inventoryAsmInstr.Item;
		}

		public sealed class InventoryStack:PureProducerStack<InventoryInstruction,InventoryStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"inventory({Instruction.Item})";
		}
	}

	[Opcode(InstructionOpcode.DebugName)]
	public sealed class DebugNameInstruction:SimpleNoStackInstruction<DebugNameInstruction,DebugNameInstruction.DebugNameStack>
	{
		public string Name;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var debugNameAsmInstr = GetAsmInstruction<Disassembler.DebugNameInstruction>();

			Name = debugNameAsmInstr.Name;
		}

		public sealed class DebugNameStack:SimpleNoStackStack<DebugNameInstruction,DebugNameStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("DebugName \"");
				writer.Write(Instruction.Name);
				writer.Write('"');
			}
		}
	}

	[Opcode(InstructionOpcode.PlayerDistanceCheck)]
	public sealed class PlayerDistanceCheckInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.SoundPlay1)]
	public sealed class SoundPlay1Instruction:SimplePureConsumerInstruction<SoundPlay1Instruction,SoundPlay1Instruction.SoundPlay1Stack>
	{
		public sealed class SoundPlay1Stack:SimplePureConsumerStack<SoundPlay1Instruction,SoundPlay1Stack>
		{
			protected override int PopCount => 1;

			public IStackProducer SoundIndex => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundPlay ");
				writer.Write(SoundIndex.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.SoundPlay1ASS)]
	public sealed class SoundPlay1AssignmentInstruction:BaseExpressionInstruction<SoundPlay1AssignmentInstruction,SoundPlay1AssignmentInstruction.SoundPlay1AssignmentStack>
	{
		public sealed class SoundPlay1AssignmentStack:BaseExpressionStack<SoundPlay1AssignmentInstruction,SoundPlay1AssignmentStack>
		{
			protected override int PopCount => 1;

			public IStackProducer SoundIndex => Operands[0];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.SoundAddress)]
	public sealed class SoundAddressInstruction:PureProducerInstruction<SoundAddressInstruction,SoundAddressInstruction.SoundAddressStack>
	{
		public int Value;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var soundAddrAsmInstr = GetAsmInstruction<Disassembler.SoundAddressInstruction>();

			Value = soundAddrAsmInstr.Value;
		}

		public sealed class SoundAddressStack:PureProducerStack<SoundAddressInstruction,SoundAddressStack>
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
	public sealed class CollisionOffsetInstruction:SimplePureConsumerInstruction<CollisionOffsetInstruction,CollisionOffsetInstruction.CollisionOffsetStack>
	{
		public sealed class CollisionOffsetStack:SimplePureConsumerStack<CollisionOffsetInstruction,CollisionOffsetStack>
		{
			protected override int PopCount => 3;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CollisionOffset ");
				writer.Write(X.ToFxString());
				writer.Write(", ");
				writer.Write(Y.ToFxString());
				writer.Write(", ");
				writer.Write(Z.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Abs)]
	public sealed class AbsInstruction:UnaryOperationInstruction<AbsInstruction,AbsInstruction.AbsStack>
	{
		public sealed class AbsStack:UnaryOperationStack<AbsInstruction,AbsStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"abs({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Pickup)]
	public sealed class PickupInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Min)]
	public sealed class MinInstruction:BinaryOperationInstruction<MinInstruction,MinInstruction.MinStack>
	{
		public sealed class MinStack:BinaryOperationStack<MinInstruction,MinStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"min({ValueA.ToFxString()}, {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.Max)]
	public sealed class MaxInstruction:BinaryOperationInstruction<MaxInstruction,MaxInstruction.MaxStack>
	{
		public sealed class MaxStack:BinaryOperationStack<MaxInstruction,MaxStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"max({ValueA.ToFxString()}, {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.SpawnParticle)]
	public sealed class SpawnParticleInstruction:SimplePureConsumerInstruction<SpawnParticleInstruction,SpawnParticleInstruction.SpawnParticleStack>
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

		public sealed class SpawnParticleStack:SimplePureConsumerStack<SpawnParticleInstruction,SpawnParticleStack>
		{
			protected override int PopCount => 6;

			public IStackProducer Type => Operands[0];
			public IStackProducer Count => Operands[1];
			public IStackProducer X => Operands[2];
			public IStackProducer Y => Operands[3];
			public IStackProducer Z => Operands[4];
			public IStackProducer Arg6 => Operands[5];

			public override void WriteStatement(Writer writer)
			{
				
				writer.Write("SpawnParticle ");
				if(Type.OperationInstruction is NumberInstruction typeNum)
				{
					writer.Write(((ParticleType)typeNum.Value).ToString());
				}
				else
				{
					writer.Write(Type.ToIntStr());
				}
				writer.Write(", ");
				writer.Write(Count.ToFxString());
				writer.Write(", ");
				writer.Write(X.ToFxString());
				writer.Write(", ");
				writer.Write(Y.ToFxString());
				writer.Write(", ");
				writer.Write(Z.ToFxString());
				writer.Write(", ");
				writer.Write(Arg6.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.Sgn)]
	public sealed class SgnInstruction:UnaryOperationInstruction<SgnInstruction,SgnInstruction.SgnStack>
	{
		public sealed class SgnStack:UnaryOperationStack<SgnInstruction,SgnStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"sgn({Value.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.SpawnAfter)]
	public sealed class SpawnAfterInstruction:BaseSpawnInstruction<SpawnAfterInstruction,SpawnAfterInstruction.SpawnAfterStack>
	{
		public sealed class SpawnAfterStack:BaseSpawnStack<SpawnAfterInstruction,SpawnAfterStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("SpawnAfter ");
				writer.Write(Instruction.SpawnStratProc.AsmLabel.SubroutineName());
				writer.Write(", ");
				writer.WriteInt(Instruction.LocalVarsToPop);
				writer.Write(", ");
				writer.WriteInt(Instruction.LocalCount);
				writer.Write(", ");
				writer.WriteInt(Instruction.TriggerCount);
				writer.Write(", ");
				writer.WriteInt(Instruction.CollisionSize);
				writer.Write(", ");
				writer.WriteInt(Instruction.CollisionBoneCount);
			}
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
	public sealed class MoveForwardqInstruction:BaseMoveInstruction<MoveForwardqInstruction,MoveForwardqInstruction.MoveForwardqStack>
	{
		public sealed class MoveForwardqStack:BaseMoveStack<MoveForwardqInstruction,MoveForwardqStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveForwardQ ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveBackwardq)]
	public sealed class MoveBackwardqInstruction:BaseMoveInstruction<MoveBackwardqInstruction,MoveBackwardqInstruction.MoveBackwardqStack>
	{
		public sealed class MoveBackwardqStack:BaseMoveStack<MoveBackwardqInstruction,MoveBackwardqStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveBackwardQ ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.ScreenPrint)]
	public sealed class ScreenPrintInstruction:BasePrintInstruction<ScreenPrintInstruction,ScreenPrintInstruction.ScreenPrintStack>
	{
		public sealed class ScreenPrintStack:SimpleNoStackStack<ScreenPrintInstruction,ScreenPrintStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("screenprint ");
				writer.Write(Instruction.Data);
			}
		}
	}

	[Opcode(InstructionOpcode.SoundPlay2)]
	public sealed class SoundPlay2Instruction:SimplePureConsumerInstruction<SoundPlay2Instruction,SoundPlay2Instruction.SoundPlay2Stack>
	{
		public sealed class SoundPlay2Stack:SimplePureConsumerStack<SoundPlay2Instruction,SoundPlay2Stack>
		{
			protected override int PopCount => 2;

			public IStackProducer SoundIndex => Operands[0];
			public IStackProducer Volume => Operands[1];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundPlay ");
				writer.Write(SoundIndex.ToIntStr());
				writer.Write(", ");
				writer.Write(Volume.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.SoundPlay2ASS)]
	public sealed class SoundPlay2AssignmentInstruction:BaseExpressionInstruction<SoundPlay2AssignmentInstruction,SoundPlay2AssignmentInstruction.SoundPlay2AssignmentStack>
	{
		public sealed class SoundPlay2AssignmentStack:BaseExpressionStack<SoundPlay2AssignmentInstruction,SoundPlay2AssignmentStack>
		{
			protected override int PopCount => 2;

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
	public sealed class SoundVolumeInstruction:SimplePureConsumerInstruction<SoundVolumeInstruction,SoundVolumeInstruction.SoundVolumeStack>
	{
		public sealed class SoundVolumeStack:SimplePureConsumerStack<SoundVolumeInstruction,SoundVolumeStack>
		{
			protected override int PopCount => 2;

			public IStackProducer Channel => Operands[0];
			public IStackProducer Volume => Operands[1];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundVolume ");
				writer.Write(Channel.ToIntStr());
				writer.Write(", ");
				writer.Write(Volume.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Push)]
	public sealed class PushInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.String)]
	public sealed class StringInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SetBossHearts)]
	public sealed class SetBossHeartsInstruction:SimplePureConsumerInstruction<SetBossHeartsInstruction,SetBossHeartsInstruction.SetBossHeartsStack>
	{
		public sealed class SetBossHeartsStack:SimplePureConsumerStack<SetBossHeartsInstruction,SetBossHeartsStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Health => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SetBossHearts ");
				writer.Write(Health.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.LoseBossHeart)]
	public sealed class LoseBossHeartInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SoundShiftRelative)]
	public sealed class SoundShiftRelativeInstruction:SimplePureConsumerInstruction<SoundShiftRelativeInstruction,SoundShiftRelativeInstruction.SoundShiftRelativeStack>
	{
		public sealed class SoundShiftRelativeStack:SimplePureConsumerStack<SoundShiftRelativeInstruction,SoundShiftRelativeStack>
		{
			protected override int PopCount => 2;

			public IStackProducer Channel => Operands[0];
			public IStackProducer Pitch => Operands[1];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundShiftRelative ");
				writer.Write(Channel.ToIntStr());
				writer.Write(", ");
				writer.Write(Pitch.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.Smin)]
	public sealed class SminInstruction:BinaryOperationInstruction<SminInstruction,SminInstruction.SminStack>
	{
		public sealed class SminStack:BinaryOperationStack<SminInstruction,SminStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"smin({ValueA.ToFxString()}, {ValueB.ToFxString()})";
		}
	}

	[Opcode(InstructionOpcode.IsBoss)]
	public sealed class IsBossInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.TopSay)]
	public sealed class TopSayInstruction:DialogSayInstruction<TopSayInstruction,TopSayInstruction.TopSayStack>
	{
		public sealed class TopSayStack:DialogSayStack<TopSayInstruction,TopSayStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("TopSay \"");
				writer.Write(Instruction.EnglishString);
				writer.Write('"');
			}
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
	public sealed class ZeroInstruction:PureProducerInstruction<ZeroInstruction,ZeroInstruction.ZeroStack>
	{
		public sealed class ZeroStack:PureProducerStack<ZeroInstruction,ZeroStack>
		{
			public sealed override bool Literal => true;

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => "0";
			public override string ToConditionStr(bool checkTrue) => checkTrue ? "0" : "(NOT 0)";
		}
	}

	[Opcode(InstructionOpcode.TopHead)]
	public sealed class TopHeadInstruction:SimplePureConsumerInstruction<TopHeadInstruction,TopHeadInstruction.TopHeadStack>
	{
		public sealed class TopHeadStack:SimplePureConsumerStack<TopHeadInstruction,TopHeadStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Sprite => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("TopHead ");
				writer.Write(Sprite.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.TopDialog)]
	public sealed class TopDialogInstruction:DialogSetInstruction<TopDialogInstruction,TopDialogInstruction.TopDialogStack>
	{
		public sealed class TopDialogStack:DialogSetStack<TopDialogInstruction,TopDialogStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write(Instruction.State ? "TopDialog on" : "TopDialog off");
			}
		}
	}

	[Opcode(InstructionOpcode.BottomSay)]
	public sealed class BottomSayInstruction:DialogSayInstruction<BottomSayInstruction,BottomSayInstruction.BottomSayStack>
	{
		public sealed class BottomSayStack:DialogSayStack<BottomSayInstruction,BottomSayStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("BottomSay \"");
				writer.Write(Instruction.EnglishString);
				writer.Write('"');
			}
		}
	}

	[Opcode(InstructionOpcode.BottomHead)]
	public sealed class BottomHeadInstruction:SimplePureConsumerInstruction<BottomHeadInstruction,BottomHeadInstruction.BottomHeadStack>
	{
		public sealed class BottomHeadStack:SimplePureConsumerStack<BottomHeadInstruction,BottomHeadStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Sprite => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("BottomHead ");
				writer.Write(Sprite.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.BottomDialog)]
	public sealed class BottomDialogInstruction:DialogSetInstruction<BottomDialogInstruction,BottomDialogInstruction.BottomDialogStack>
	{
		public sealed class BottomDialogStack:DialogSetStack<BottomDialogInstruction,BottomDialogStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write(Instruction.State ? "BottomDialog on" : "BottomDialog off");
			}
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
	public sealed class FadeOutInstruction:SimplePureConsumerInstruction<FadeOutInstruction,FadeOutInstruction.FadeOutStack>
	{
		public sealed class FadeOutStack:SimplePureConsumerStack<FadeOutInstruction,FadeOutStack>
		{
			protected override int PopCount => 1;

			public IStackProducer FadeType => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("FadeOut ");
				writer.Write(FadeType.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.FadeIn)]
	public sealed class FadeInInstruction:SimplePureConsumerInstruction<FadeInInstruction,FadeInInstruction.FadeInStack>
	{
		public sealed class FadeInStack:SimplePureConsumerStack<FadeInInstruction,FadeInStack>
		{
			protected override int PopCount => 1;

			public IStackProducer FadeType => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("FadeIn ");
				writer.Write(FadeType.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveUpq)]
	public sealed class MoveUpqInstruction:BaseMoveInstruction<MoveUpqInstruction,MoveUpqInstruction.MoveUpqStack>
	{
		public sealed class MoveUpqStack:BaseMoveStack<MoveUpqInstruction,MoveUpqStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveUpQ ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveDownq)]
	public sealed class MoveDownqInstruction:BaseMoveInstruction<MoveDownqInstruction,MoveDownqInstruction.MoveDownqStack>
	{
		public sealed class MoveDownqStack:BaseMoveStack<MoveDownqInstruction,MoveDownqStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveDownQ ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.ForcePlayerDist)]
	public sealed class ForcePlayerDistInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ShadeType)]
	public sealed class ShadeTypeInstruction:SimplePureConsumerInstruction<ShadeTypeInstruction,ShadeTypeInstruction.ShadeTypeStack>
	{
		public sealed class ShadeTypeStack:SimplePureConsumerStack<ShadeTypeInstruction,ShadeTypeStack>
		{
			protected override int PopCount => 1;

			public IStackProducer ShadeType => Operands[0];

			//TODO: Custom int values?
			public override void WriteStatement(Writer writer)
			{
				writer.Write("ShadeType ");
				writer.Write(ShadeType.ToIntStr());
			}
		}
	}

	[Opcode(InstructionOpcode.NOP)]
	public sealed class NOPInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SetAnimSpeed)]
	public sealed class SetAnimSpeedInstruction:SimplePureConsumerInstruction<SetAnimSpeedInstruction,SetAnimSpeedInstruction.SetAnimSpeedStack>
	{
		public sealed class SetAnimSpeedStack:SimplePureConsumerStack<SetAnimSpeedInstruction,SetAnimSpeedStack>
		{
			protected override int PopCount => 1;

			public IStackProducer AnimSpeed => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SetAnimSpeed ");
				writer.Write(AnimSpeed.ToFxString());
			}
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
	public sealed class DistanceInstruction:BaseExpressionInstruction<DistanceInstruction,DistanceInstruction.DistanceStack>
	{
		public sealed class DistanceStack:BaseExpressionStack<DistanceInstruction,DistanceStack>
		{
			protected override int PopCount => 3;

			public IStackProducer X => Operands[0];
			public IStackProducer Y => Operands[1];
			public IStackProducer Z => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"Distance {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.Binocs)]
	public sealed class BinocsInstruction:SimpleNoStackInstruction<BinocsInstruction,BinocsInstruction.BinocsStack>
	{
		public bool State;

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

		public sealed class BinocsStack:SimpleNoStackStack<BinocsInstruction,BinocsStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write(Instruction.State ? "Binocs on" : "Binocs off");
			}
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
	public sealed class WorldVectorInstruction:SimplePureConsumerInstruction<WorldVectorInstruction,WorldVectorInstruction.WorldVectorStack>
	{
		public sealed class WorldVectorStack:SimplePureConsumerStack<WorldVectorInstruction,WorldVectorStack>
		{
			protected override int PopCount => 3;

			//This is the correct pop order
			public IStackProducer Z => Operands[0];
			public IStackProducer X => Operands[1];
			public IStackProducer Angle => Operands[2];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("WorldVector ");
				writer.Write(Z.ToFxString());
				writer.Write(", ");
				writer.Write(X.ToFxString());
				writer.Write(", ");
				writer.Write(Angle.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.ObjectFallVerySlow)]
	public sealed class ObjectFallVerySlowInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Slope2Controller)]
	public sealed class Slope2ControllerInstruction:SimplePureConsumerInstruction<Slope2ControllerInstruction,Slope2ControllerInstruction.Slope2ControllerStack>
	{
		public sealed class Slope2ControllerStack:SimplePureConsumerStack<Slope2ControllerInstruction,Slope2ControllerStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Unused => Operands[0];

			public override void WriteStatement(Writer writer)
			{ 
				if(Unused is not NumberInstruction.NumberStack num || num.Instruction.Value != 0)
				{
					throw new Exception("Slope2Controller has non-zero operand");
				}
				writer.Write("Slope2Controller ");
				writer.Write(Unused.ToIntStr());;
			}
		}
	}

	[Opcode(InstructionOpcode.LevelComplete)]
	public sealed class LevelCompleteInstruction:BaseExpressionInstruction<LevelCompleteInstruction,LevelCompleteInstruction.LevelCompleteStack>
	{
		public sealed class LevelCompleteStack:BaseExpressionStack<LevelCompleteInstruction,LevelCompleteStack>
		{
			protected override int PopCount => 3;

			public IStackProducer Tribe => Operands[0];
			public IStackProducer Level => Operands[1];
			public IStackProducer Type => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"LevelComplete {Tribe.ToFxString()}, {Level.ToFxString()}, {Type.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.SetLevelFlag)]
	public sealed class SetLevelFlagInstruction:SimplePureConsumerInstruction<SetLevelFlagInstruction,SetLevelFlagInstruction.SetLevelFlagStack>
	{
		public sealed class SetLevelFlagStack:SimplePureConsumerStack<SetLevelFlagInstruction,SetLevelFlagStack>
		{
			protected override int PopCount => 1;

			public IStackProducer LevelFlag => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SetLevelFlag ");
				writer.Write(LevelFlag.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.GetLevelFlag)]
	public sealed class GetLevelFlagInstruction:BaseExpressionInstruction<GetLevelFlagInstruction,GetLevelFlagInstruction.GetLevelFlagStack>
	{
		public sealed class GetLevelFlagStack:BaseExpressionStack<GetLevelFlagInstruction,GetLevelFlagStack>
		{
			protected override int PopCount => 3;

			public IStackProducer Tribe => Operands[0];
			public IStackProducer Level => Operands[1];
			public IStackProducer Type => Operands[2];

			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"GetLevelFlag {Tribe.ToFxString()}, {Level.ToFxString()}, {Type.ToFxString()}";
		}
	}

	[Opcode(InstructionOpcode.CalcCarTilt)]
	public sealed class CalcCarTiltInstruction:SimplePureConsumerInstruction<CalcCarTiltInstruction,CalcCarTiltInstruction.CalcCarTiltStack>
	{
		public sealed class CalcCarTiltStack:SimplePureConsumerStack<CalcCarTiltInstruction,CalcCarTiltStack>
		{
			protected override int PopCount => 4;

			//Notice the backwards numbering
			public IStackProducer Op4 => Operands[0];
			public IStackProducer Op3 => Operands[1];
			public IStackProducer Op2 => Operands[2];
			public IStackProducer Op1 => Operands[3];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("CalcCarTilt ");
				writer.Write(Op4.ToFxString());
				writer.Write(", ");
				writer.Write(Op3.ToFxString());
				writer.Write(", ");
				writer.Write(Op2.ToFxString());
				writer.Write(", ");
				writer.Write(Op1.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveLeftq)]
	public sealed class MoveLeftqInstruction:BaseMoveInstruction<MoveLeftqInstruction,MoveLeftqInstruction.MoveLeftqStack>
	{
		public sealed class MoveLeftqStack:BaseMoveStack<MoveLeftqInstruction,MoveLeftqStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveLeftQ ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.MoveRightq)]
	public sealed class MoveRightqInstruction:BaseMoveInstruction<MoveRightqInstruction,MoveRightqInstruction.MoveRightqStack>
	{
		public sealed class MoveRightqStack:BaseMoveStack<MoveRightqInstruction,MoveRightqStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("MoveRightQ ");
				writer.Write(Amount.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.BitwiseNot)]
	public sealed class BitwiseNotInstruction:UnaryOperationInstruction<BitwiseNotInstruction,BitwiseNotInstruction.BitwiseNotStack>
	{
		public sealed class BitwiseNotStack:UnaryOperationStack<BitwiseNotInstruction,BitwiseNotStack>
		{
			public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"~{Value.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.BordersOn)]
	public sealed class BordersOnInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.BordersOff)]
	public sealed class BordersOffInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.SoundAdsr)]
	public sealed class SoundAdsrInstruction:SimplePureConsumerInstruction<SoundAdsrInstruction,SoundAdsrInstruction.SoundAdsrStack>
	{
		public sealed class SoundAdsrStack:SimplePureConsumerStack<SoundAdsrInstruction,SoundAdsrStack>
		{
			protected override int PopCount => 5;

			public IStackProducer Channel => Operands[0];
			public IStackProducer A => Operands[1];
			public IStackProducer D => Operands[2];
			public IStackProducer S => Operands[3];
			public IStackProducer R => Operands[4];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundAdsr ");
				writer.Write(Channel.ToIntStr());
				writer.Write(", ");
				writer.Write(A.ToFxString());
				writer.Write(", ");
				writer.Write(D.ToFxString());
				writer.Write(", ");
				writer.Write(S.ToFxString());
				writer.Write(", ");
				writer.Write(R.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.SoundAdsrRelative)]
	public sealed class SoundAdsrRelativeInstruction:SimplePureConsumerInstruction<SoundAdsrRelativeInstruction,SoundAdsrRelativeInstruction.SoundAdsrRelativeStack>
	{
		public sealed class SoundAdsrRelativeStack:SimplePureConsumerStack<SoundAdsrRelativeInstruction,SoundAdsrRelativeStack>
		{
			protected override int PopCount => 5;

			public IStackProducer Channel => Operands[0];
			public IStackProducer A => Operands[1];
			public IStackProducer D => Operands[2];
			public IStackProducer S => Operands[3];
			public IStackProducer R => Operands[4];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SoundAdsrRelative ");
				writer.Write(Channel.ToIntStr());
				writer.Write(", ");
				writer.Write(A.ToFxString());
				writer.Write(", ");
				writer.Write(D.ToFxString());
				writer.Write(", ");
				writer.Write(S.ToFxString());
				writer.Write(", ");
				writer.Write(R.ToFxString());
			}
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
	public sealed class SampleStatusInstruction:BaseExpressionInstruction<SampleStatusInstruction,SampleStatusInstruction.SampleStatusStack>
	{
		public sealed class SampleStatusStack:BaseExpressionStack<SampleStatusInstruction,SampleStatusStack>
		{
			protected override int PopCount => 1;

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
	public sealed class SetItemInstruction:SimplePureConsumerInstruction<SetItemInstruction,SetItemInstruction.SetItemStack>
	{
		public sealed class SetItemStack:SimplePureConsumerStack<SetItemInstruction,SetItemStack>
		{
			protected override int PopCount => 1;

			public IStackProducer ExtraMax => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SetItem ");
				writer.Write(ExtraMax.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.SetTimer)]
	public sealed class SetTimerInstruction:SimplePureConsumerInstruction<SetTimerInstruction,SetTimerInstruction.SetTimerStack>
	{
		public sealed class SetTimerStack:SimplePureConsumerStack<SetTimerInstruction,SetTimerStack>
		{
			protected override int PopCount => 1;

			public IStackProducer Timer => Operands[0];

			public override void WriteStatement(Writer writer)
			{
				writer.Write("SetTimer ");
				writer.Write(Timer.ToFxString());
			}
		}
	}

	[Opcode(InstructionOpcode.TimerOff)]
	public sealed class TimerOffInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.DistanceNoY)]
	public sealed class DistanceNoYInstruction:BaseExpressionInstruction<DistanceNoYInstruction,DistanceNoYInstruction.DistanceNoYStack>
	{
		public sealed class DistanceNoYStack:BaseExpressionStack<DistanceNoYInstruction,DistanceNoYStack>
		{
			protected override int PopCount => 2;

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
	public sealed class CreditInstruction:SimpleNoStackInstruction<CreditInstruction,CreditInstruction.CreditStack>
	{
		public int Operand;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var creditAsmInstr = GetAsmInstruction<Disassembler.CreditInstruction>();

			Operand = creditAsmInstr.Operand;
		}

		public sealed class CreditStack:SimpleNoStackStack<CreditInstruction,CreditStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("Credit ");
				writer.WriteInt(Instruction.Operand);
			}
		}
	}

	[Opcode(InstructionOpcode.CloseCredits)]
	public sealed class CloseCreditsInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ShowRewardCard)]
	public sealed class ShowRewardCardInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.ShowHearts)]
	public sealed class ShowHeartsInstruction:SimpleNoOperandsNoStackInstruction;

	[Opcode(InstructionOpcode.Cwg)]
	public sealed class CwgInstruction:SimpleNoStackInstruction<CwgInstruction,CwgInstruction.CwgStack>
	{
		public int Value;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var cwgAsmInstr = GetAsmInstruction<Disassembler.CwgInstruction>();

			Value = cwgAsmInstr.Value;
		}

		public sealed class CwgStack:SimpleNoStackStack<CwgInstruction,CwgStack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("Cwg ");
				writer.WriteInt(Instruction.Value);
			}
		}
	}

	[Opcode(InstructionOpcode.FadeFunction_47E960)]
	public sealed class FadeFunction_47E960Instruction:SimpleNoStackInstruction<FadeFunction_47E960Instruction,FadeFunction_47E960Instruction.FadeFunction_47E960Stack>
	{
		public int Value;

		public sealed override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var fadeSetUnknownAsmInstr = GetAsmInstruction<Disassembler.FadeSetUnknownInstruction>();
			Value = fadeSetUnknownAsmInstr.Value;
		}

		public sealed class FadeFunction_47E960Stack:SimpleNoStackStack<FadeFunction_47E960Instruction,FadeFunction_47E960Stack>
		{
			public override void WriteStatement(Writer writer)
			{
				writer.Write("FadeFunction_47E960 ");
				writer.WriteInt(Instruction.Value);
			}
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