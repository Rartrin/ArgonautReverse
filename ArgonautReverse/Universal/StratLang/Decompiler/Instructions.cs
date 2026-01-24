using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using ArgonautReverse.PSX.StratLang;

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

		public override bool TryGetSubroutine(out AsmInstruction subroutine) => throw new NotImplementedException();

		public override bool TryGetLabel(out AsmInstruction label) => throw new NotImplementedException();
	}

	public abstract class BaseOperandInstruction:Instruction,IStackOperation
	{
		public Instruction OperationInstruction => this;

		public abstract IStackStatement Statement{get;}

		public abstract void Analyze(StackAnalyzer stack);

		public abstract IEnumerable<IStackOperation> GetRootOperations();
	}

	public abstract class BaseConsumerInstruction:BaseOperandInstruction,IStackConsumer
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
				var producer = (Instruction)operand;
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
				var producer = (Instruction)operand;
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

	public abstract class PureConsumeInstruction:BaseConsumerInstruction,IStackStatement,IFlowStatement
	{
		public Instruction StatementInstruction => this;
		public IFlowStatement FlowStatement => this;

		public override IStackStatement Statement => this;
		public virtual IStackStatement ControlStatement => null;

		public AsmInstruction StatementLabel{get;private set;}
		public Instruction FirstInstruction{get;private set;}

		public abstract string ToStatement();

		public FlowStatementType FlowType{get;set;}
		public FlowData FlowData{get;set;}

		public Queue<(FlowStatementType Type, FlowData Data)> PreFlows{get;} = new Queue<(FlowStatementType Type, FlowData Data)>();
		public Stack<(FlowStatementType Type, FlowData Data)> PostFlows{get;} = new Stack<(FlowStatementType Type, FlowData Data)>();

		public IList<IFlowStatement> FlowSources{get;} = new List<IFlowStatement>();
		public IList<IFlowStatement> FlowDestinations{get;} = new List<IFlowStatement>();
		
		public IStackStatement StackStatement => this;

		public IStackStatement NextStatement{get;set;}
		public IStackStatement PrevStatement{get;set;}

		public override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);
			
			StatementLabel = stack.CurrentStatementFirstInstruction.AsmLabel;
			FirstInstruction = stack.CurrentStatementFirstInstruction;
		}

		public virtual void Analyze(FlowAnalyzer flow){}
	}

	public abstract class SimplePureConsumeInstruction:PureConsumeInstruction;

	public abstract class PureProducerInstruction:BaseOperandInstruction,IStackProducer
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

		public override bool TryGetSubroutine(out AsmInstruction subroutine)
		{
			subroutine = AsmLabel;
			return AsmLabel.IsSubroutineEntry;
		}

		public override bool TryGetLabel(out AsmInstruction label)
		{
			label = AsmLabel;
			return label.HasLabel;
		}
	}

	public abstract class BaseExpressionInstruction:BaseConsumerInstruction,IStackExpression
	{
		public IStackConsumer Consumer{get;set;}

		public sealed override IStackStatement Statement => Consumer.Statement;

		public sealed override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);
			stack.Push(this);
		}

		public abstract string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown);
	}

	/// <summary>Instructions that don't use the stack</summary>
	public abstract class NoStackInstruction:Instruction,IStackStatement,IFlowStatement
	{
		public Instruction OperationInstruction => this;

		public Instruction StatementInstruction => this;
		public IFlowStatement FlowStatement => this;

		public AsmInstruction StatementLabel => AsmLabel;
		public Instruction FirstInstruction => this;

		public IStackStatement Statement => this;
		public virtual IStackStatement ControlStatement => null;

		public IStackStatement NextStatement{get;set;}
		public IStackStatement PrevStatement{get;set;}

		public FlowStatementType FlowType{get;set;}
		public FlowData FlowData{get;set;}

		public Queue<(FlowStatementType Type, FlowData Data)> PreFlows{get;} = new Queue<(FlowStatementType Type, FlowData Data)>();
		public Stack<(FlowStatementType Type, FlowData Data)> PostFlows{get;} = new Stack<(FlowStatementType Type, FlowData Data)>();

		public IList<IFlowStatement> FlowSources{get;} = new List<IFlowStatement>();
		public IList<IFlowStatement> FlowDestinations{get;} = new List<IFlowStatement>();

		public IStackStatement StackStatement => this;

		public virtual void Analyze(StackAnalyzer stack){}

		public abstract string ToStatement();

		public virtual void Analyze(FlowAnalyzer flow){}

		public IEnumerable<IStackOperation> GetRootOperations() => [this];

		public override bool TryGetSubroutine([MaybeNullWhen(false)]out AsmInstruction subroutine)
		{
			if(AsmLabel.IsSubroutineEntry)
			{
				subroutine = AsmLabel;
				return true;
			}
			subroutine = null;
			return false;
		}

		public override bool TryGetLabel([MaybeNullWhen(false)]out AsmInstruction label)
		{
			if(AsmLabel.HasLabel)
			{
				label = AsmLabel;
				return true;
			}
			label = null;
			return false;
		}
	}
	public abstract class SimpleNoStackInstruction:NoStackInstruction
	{
		public override string ToStatement()
		{
			//TODO: Make this better
			string name = this.GetType().GetCustomAttribute<OpcodeAttribute>().Opcode.ToString();
			return name;
		}
	}

	public abstract class BinaryOperationInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 2;

		public IStackProducer ValueA => Operands[0];
		public IStackProducer ValueB => Operands[1];
	}

	public abstract class UnaryOperationInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Value => Operands[0];
	}

	public abstract class BranchInstruction:PureConsumeInstruction,IFlowBranch
	{
		public override int PopCount => 1;
		
		public Instruction ConditionalDest;

		public IStackProducer Condition => Operands[0];

		public override IStackStatement ControlStatement => this;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);

			var branchAsmInstr = GetAsmInstruction<PSX.StratLang.BranchInstruction>();
			ConditionalDest = parser.GetInstruction(branchAsmInstr.ConditionalDest, null, this);
		}

		public override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);
			
			stack.AddDest(ConditionalDest);
		}

		public override void Analyze(FlowAnalyzer flow)
		{
			base.Analyze(flow);

			flow.AddDest(this, ConditionalDest);
		}
	}

	public abstract class DialogSayInstruction:SimpleNoStackInstruction
	{
		public string EnglishString;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var dialogSayAsmInstr = GetAsmInstruction<PSX.StratLang.DialogSayInstruction>();

			EnglishString = dialogSayAsmInstr.EnglishString;
		}
	}

	public abstract class DialogSetInstruction:SimpleNoStackInstruction
	{
		public bool State;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var dialogSetAsmInstr = GetAsmInstruction<PSX.StratLang.DialogSetInstruction>();

			State = dialogSetAsmInstr.Operand switch
			{
				0 => false,
				1 => true,
				_ => throw new Exception("Unknown state")
			};
		}
	}

	public abstract class FlagInstruction:SimpleNoStackInstruction
	{
		public string Flag;

		//Operation
		//public int FlagGroup;
		//public int FlagChanged;

		//Operand
		public bool NewState;

		public FlagInstruction()
		{
			Flag = this.GetType().GetCustomAttribute<OpcodeAttribute>().Opcode.ToString();
		}

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var flagAsmInstr = GetAsmInstruction<PSX.StratLang.FlagInstruction>();

			NewState = flagAsmInstr.NewState;
		}

		public override string ToStatement() => NewState ? $"{Flag} on" : $"{Flag} off";
	}

	public abstract class ItemChangeInstruction:SimpleNoStackInstruction
	{
		//Give/take item from inventory

		//Operand
		public int Item;

		//Operation
		public abstract int Change{get;}

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var itemChangeAsmInstr = GetAsmInstruction<PSX.StratLang.ItemChangeInstruction>();

			Item = itemChangeAsmInstr.Item;
		}

		public override string ToStatement() => Change switch
		{
			-1 => $"DelInv {Item}",
			+1 => $"AddInv {Item}",
			_ => throw new Exception()
		};
	}

	public abstract class BaseJumpInstruction:SimpleNoStackInstruction,IFlowGoto
	{
		public override bool Terminal => true;

		public Instruction Destination;

		public override IStackStatement ControlStatement => this;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var jumpAsmInstr = GetAsmInstruction<PSX.StratLang.JumpInstruction>();
			Destination = parser.GetInstruction(jumpAsmInstr.Destination, null, this);
		}

		public override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);

			stack.AddDest(Destination);
		}

		public override void Analyze(FlowAnalyzer flow)
		{
			base.Analyze(flow);

			flow.AddDest(this, Destination);
		}
	}

	public abstract class BaseJumpSubroutineInstruction:SimpleNoStackInstruction
	{
		//Technically pushes a value but we aren't counting it because it's popped outside of this subroutine.

		public Instruction Proc;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var jumpSubroutineAsmInstr = GetAsmInstruction<PSX.StratLang.JumpSubroutineInstruction>();
			Proc = parser.GetProc(jumpSubroutineAsmInstr.Proc, this);
		}
	}

	public abstract class BaseMoveInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Amount => Operands[0];
	}

	public abstract class BasePrintInstruction:SimpleNoStackInstruction
	{
		public string Data;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var printAsmInstr = GetAsmInstruction<PSX.StratLang.PrintInstruction>();
			//TODO: Handle PrintInstruction string args with spaces
			Data = GetPrintString(printAsmInstr);
		}

		public static string GetPrintString(PSX.StratLang.PrintInstruction printAsmInstr)
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

			var spawnAsmInstr = GetAsmInstruction<PSX.StratLang.BaseSpawnInstruction>();
			
			SpawnStratProc = parser.GetStrat(spawnAsmInstr.SpawnStratProc, this);

			LocalVarsToPop = spawnAsmInstr.LocalVarsToPop;
			LocalCount = spawnAsmInstr.LocalCount;
			TriggerCount = spawnAsmInstr.TriggerCount;
			CollisionSize = spawnAsmInstr.CollisionSize;
			CollisionBoneCount = spawnAsmInstr.CollisionBoneCount;
		}
	}

	public abstract class TriggerUpdateInstruction:SimpleNoStackInstruction
	{
		public Instruction TriggerProc;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var triggerUpdateAsmInstr = GetAsmInstruction<PSX.StratLang.TriggerUpdateInstruction>();
			TriggerProc = parser.GetTrigger(triggerUpdateAsmInstr.TriggerProc, this);
		}
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

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var varAsmInstr = GetAsmInstruction<PSX.StratLang.VarInstruction>();
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

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown)
		{
			string varName;
			if(Source != SourceStrat.This)
			{
				varName = $"{Source}@{ID}";
			}
			else
			{
				varName = ID;
			}
			if((requestType==ExpressionType.Dereference) == GetAddress)
			{
				return varName;
			}
			else if((requestType!=ExpressionType.Dereference) && GetAddress)
			{
				return "&" + varName;
			}
			else//if (dereference && !GetAddress)
			{
				throw new Exception();
			}
		}
	}

	[Opcode(InstructionOpcode.CommandError)]
	public sealed class CommandErrorInstruction:SimpleNoStackInstruction,IFlowTerminal
	{
		public override bool Terminal => true;

		public override string ToStatement() => "COMMAND ERROR";
	}

	[Opcode(InstructionOpcode.Local)]
	public sealed class LocalInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.Local;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Global)]
	public sealed class GlobalInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.Global;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.WorldGlobal)]
	public sealed class WorldGlobalInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.WorldGlobal;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.AlienVar)]
	public sealed class AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.LocalAddress)]
	public sealed class LocalAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.Local;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.GlobalAddress)]
	public sealed class GlobalAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.Global;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.WorldGlobalAddress)]
	public sealed class WorldGlobalAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.WorldGlobal;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.AlienVarAddress)]
	public sealed class AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.This;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Print)]
	public sealed class PrintInstruction:BasePrintInstruction
	{
		public override string ToStatement() => $"print {Data}";
	}

	[Opcode(InstructionOpcode.Number)]
	public sealed class NumberInstruction:PureProducerInstruction
	{
		public override bool Literal => true;

		public int Value;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var numberAsmInstr = GetAsmInstruction<PSX.StratLang.NumberInstruction>();

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

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown)
		{
			if(requestType == ExpressionType.Integer)
			{
				return Value.ToString();
			}
			if(requestType == ExpressionType.FixedPoint)
			{
				if(TryGetValue(Value, out var converted))
				{
					return converted.ToString();
				}
				return (Value/4096.0).ToString();
			}
			return $"UnknownValue({Value})";
		}
	}

	[Opcode(InstructionOpcode.UMinus)]
	public sealed class UMinusInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"-{Value.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Increase)]
	public sealed class IncreaseInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public VarInstruction Address => (VarInstruction)Operands[0];

		public override string ToStatement() => $"{Address.ToExpressionString(ExpressionType.Dereference)}++";
	}

	[Opcode(InstructionOpcode.Decrease)]
	public sealed class DecreaseInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public VarInstruction Address => (VarInstruction)Operands[0];

		public override string ToStatement() => $"{Address.ToExpressionString(ExpressionType.Dereference)}--";
	}

	[Opcode(InstructionOpcode.Add)]
	public sealed class AddInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} + {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Sub)]
	public sealed class SubInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} - {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Mul)]
	public sealed class MulInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} * {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Div)]
	public sealed class DivInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} / {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Equals)]
	public sealed class EqualsInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 2;
		
		public VarInstruction Address => (VarInstruction)Operands[0];
		public IStackProducer Value => Operands[1];

		public override string ToStatement() => $"{Address.ToExpressionString(ExpressionType.Dereference)} = {Value.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Compare)]
	public sealed class CompareInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} = {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.LessThan)]
	public sealed class LessThanInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} < {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.GreaterThan)]
	public sealed class GreaterThanInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} > {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.SetModel)]
	public sealed class SetModelInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Model => Operands[0];

		public override string ToStatement() => $"setmodel {Model.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.Scale)]
	public sealed class ScaleInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Scale => Operands[0];

		public override string ToStatement() => $"scale {Scale.ToFxString()}";
	}

	[Opcode(InstructionOpcode.ScaleX)]
	public sealed class ScaleXInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ScaleX => Operands[0];

		public override string ToStatement() => $"scalex {ScaleX.ToFxString()}";
	}

	[Opcode(InstructionOpcode.ScaleY)]
	public sealed class ScaleYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ScaleY => Operands[0];

		public override string ToStatement() => $"scaley {ScaleY.ToFxString()}";
	}

	[Opcode(InstructionOpcode.ScaleZ)]
	public sealed class ScaleZInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ScaleZ => Operands[0];

		public override string ToStatement() => $"scalez {ScaleZ.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Shadow)]
	public sealed class ShadowInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.ShadowSize)]
	public sealed class ShadowSizeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ShadowSize => Operands[0];

		public override string ToStatement() => $"ShadowSize {ShadowSize.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.ShadowType)]
	public sealed class ShadowTypeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ShadowType => Operands[0];

		public override string ToStatement() => $"ShadowType {ShadowType.ToIntStr()}";
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
		public override string ToStatement() => $"MoveUp {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveDown)]
	public sealed class MoveDownInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveDown {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveForward)]
	public sealed class MoveForwardInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveForward {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveBackward)]
	public sealed class MoveBackwardInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveBackward {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveLeft)]
	public sealed class MoveLeftInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveLeft {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveRight)]
	public sealed class MoveRightInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveRight {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnRight)]
	public sealed class TurnRightInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"TurnRight {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnLeft)]
	public sealed class TurnLeftInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"TurnLeft {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TiltLeft)]
	public sealed class TiltLeftInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"TiltLeft {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TiltRight)]
	public sealed class TiltRightInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"TiltRight {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TiltForward)]
	public sealed class TiltForwardInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"TiltForward {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TiltBackward)]
	public sealed class TiltBackwardInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"TiltBackward {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnToPlayerX)]
	public sealed class TurnToPlayerXInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxTurnSpeed => Operands[0];

		public override string ToStatement() => $"TurnToPlayerX {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnToPlayerY)]
	public sealed class TurnToPlayerYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxTurnSpeed => Operands[0];

		public override string ToStatement() => $"TurnToPlayerY {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnToPlayerXY)]
	public sealed class TurnToPlayerXYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxTurnSpeed => Operands[0];

		public override string ToStatement() => $"TurnToPlayerXY {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnToX)]
	public sealed class TurnToXInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 4;
		
		public IStackProducer X => Operands[0];
		public IStackProducer Y => Operands[1];
		public IStackProducer Z => Operands[2];
		public IStackProducer MaxTurnSpeed => Operands[3];

		public override string ToStatement() => $"TurnToX {X.ToExpressionString()}, {Y.ToExpressionString()}, {Z.ToExpressionString()}, {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnToY)]
	public sealed class TurnToYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 4;
		
		public IStackProducer X => Operands[0];
		public IStackProducer Y => Operands[1];
		public IStackProducer Z => Operands[2];
		public IStackProducer MaxTurnSpeed => Operands[3];

		public override string ToStatement() => $"TurnToY {X.ToExpressionString()}, {Y.ToExpressionString()}, {Z.ToExpressionString()}, {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.TurnToXY)]
	public sealed class TurnToXYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 4;
		
		public IStackProducer X => Operands[0];
		public IStackProducer Y => Operands[1];
		public IStackProducer Z => Operands[2];
		public IStackProducer MaxTurnSpeed => Operands[3];

		public override string ToStatement() => $"TurnToXY {X.ToExpressionString()}, {Y.ToExpressionString()}, {Z.ToExpressionString()}, {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Wobble)]
	public sealed class WobbleInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Value => Operands[0];

		public override string ToStatement() => $"Wobble {Value.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.ReSetPos)]
	public sealed class ReSetPosInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SetPos)]
	public sealed class SetPosInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Jump)]
	public sealed class JumpInstruction:BaseJumpInstruction
	{
		public override string ToStatement() => $"goto {Destination.AsmLabel.GetLabel()} $ DONE";
	}

	[Opcode(InstructionOpcode.ObjectFall)]
	public sealed class ObjectFallInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Hang)]
	public sealed class HangInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.WPFirst)]
	public sealed class WPFirstInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WPLast)]
	public sealed class WPLastInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WPNext)]
	public sealed class WPNextInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WPPrev)]
	public sealed class WPPrevInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WPDel)]
	public sealed class WPDelInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.WPNew)]
	public sealed class WPNewInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.WPNearest)]
	public sealed class WPNearestInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WPFurthest)]
	public sealed class WPFurthestInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WPTurnToX)]
	public sealed class WPTurnToXInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxTurnSpeed => Operands[0];

		public override string ToStatement() => $"WPTurnToX {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.WPTurnToY)]
	public sealed class WPTurnToYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxTurnSpeed => Operands[0];

		public override string ToStatement() => $"WPTurnToY {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.WPTurnToXY)]
	public sealed class WPTurnToXYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxTurnSpeed => Operands[0];

		public override string ToStatement() => $"WPTurnToXY {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.AnimPlay)]
	public sealed class AnimPlayInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer AnimationAddr => Operands[0];

		public override string ToStatement() => $"AnimPlay {AnimationAddr.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.AnimStop)]
	public sealed class AnimStopInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.AnimClear)]
	public sealed class AnimClearInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.AnimSetSpeed)]
	public sealed class AnimSetSpeedInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollisionType)]
	public sealed class CollisionTypeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer CollisionType => Operands[0];

		public override string ToStatement() => $"CollisionType {CollisionType.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.CollRadius)]
	public sealed class CollisionRadiusInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer CollRadius => Operands[0];

		public override string ToStatement() => $"CollisionRadius {CollRadius.ToFxString()}";
	}

	[Opcode(InstructionOpcode.CollHeight)]
	public sealed class CollisionHeightInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollExtent)]
	public sealed class CollisionExtentInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer CollExtent => Operands[0];

		public override string ToStatement() => $"CollisionExtent {CollExtent.ToFxString()}";
	}

	[Opcode(InstructionOpcode.CollView)]
	public sealed class CollisionViewInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CollPoints)]
	public sealed class CollisionPointsInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Value => Operands[0];

		public override string ToStatement() => $"CollisionPoints {Value.ToFxString()}";
	}

	[Opcode(InstructionOpcode.CollSetPoint)]
	public sealed class CollSetPointInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 4;
		
		public IStackProducer PointIndex => Operands[0];
		public IStackProducer X => Operands[1];
		public IStackProducer Y => Operands[2];
		public IStackProducer Z => Operands[3];

		public override string ToStatement() => $"CollisionSetPoint {PointIndex.ToFxString()}, {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}";
	}

	[Opcode(InstructionOpcode.CreateTrigger)]
	public sealed class CreateTriggerInstruction:SimpleNoStackInstruction
	{
		[Flags]
		public enum TriggerType
		{
			Every		= 1<<1,
			WhenHit		= 1<<2,
			EndFall		= 1<<3,
			EndJump		= 1<<4,
			In			= 1<<5,
			Anim		= 1<<6,
			WhenNear	= 1<<7,
			WhenFar		= 1<<8,
			WhenHitWall	= 1<<9,
		};

		public TriggerType Type;
		public int Arg;
		public Instruction TriggerProc;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var triggerCreateAsmInstr = GetAsmInstruction<PSX.StratLang.TriggerCreateInstruction>();
			Type = triggerCreateAsmInstr.Type switch
			{
				PSX.TriggerTypePSX.None => 0,
				PSX.TriggerTypePSX.Every => TriggerType.Every,
				PSX.TriggerTypePSX.WhenHit => TriggerType.WhenHit,
				PSX.TriggerTypePSX.EndFall => TriggerType.EndFall,
				PSX.TriggerTypePSX.EndJump => TriggerType.EndJump,
				PSX.TriggerTypePSX.In => TriggerType.In,
				PSX.TriggerTypePSX.Anim => TriggerType.Anim,
				PSX.TriggerTypePSX.WhenNear => TriggerType.WhenNear,
				PSX.TriggerTypePSX.WhenFar => TriggerType.WhenFar,
				PSX.TriggerTypePSX.WhenHitWall => TriggerType.WhenHitWall,
				_ => throw new Exception()
			};
			Arg = triggerCreateAsmInstr.Arg;
			TriggerProc = parser.GetTrigger(triggerCreateAsmInstr.Stream, this);
		}

		public override string ToStatement()
		{
			var ret = new StringBuilder();
			ret.Append($"CreateTrigger {Type} ");
			switch(Type)
			{
				case TriggerType.Every or TriggerType.In://Frame Count
				case TriggerType.Anim://Animation Number
				case TriggerType.WhenNear or TriggerType.WhenFar://Distance
					ret.Append(Arg + " ");
					break;
			}
			ret.Append(TriggerProc.AsmLabel.SubroutineName());
			return ret.ToString();
		}
	}

	[Opcode(InstructionOpcode.KillTrigger)]
	public sealed class KillTriggerInstruction:TriggerUpdateInstruction
	{
		public override string ToStatement() => $"KillTrigger {TriggerProc.AsmLabel.SubroutineName()}";
	}

	[Opcode(InstructionOpcode.HoldTriggers)]
	public sealed class HoldTriggersInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ReleaseTriggers)]
	public sealed class ReleaseTriggersInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.HoldTrigger)]
	public sealed class HoldTriggerInstruction:TriggerUpdateInstruction
	{
		public override string ToStatement() => $"HoldTrigger {TriggerProc.AsmLabel.SubroutineName()}";
	}

	[Opcode(InstructionOpcode.ReleaseTrigger)]
	public sealed class ReleaseTriggerInstruction:TriggerUpdateInstruction
	{
		public override string ToStatement() => $"ReleaseTrigger {TriggerProc.AsmLabel.SubroutineName()}";
	}

	[Opcode(InstructionOpcode.Wait)]
	public sealed class WaitInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Value => Operands[0];

		public override string ToStatement() => $"Wait {Value.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Hold)]
	public sealed class HoldInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Release)]
	public sealed class ReleaseInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Remove)]
	public sealed class RemoveInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.MapRemove)]
	public sealed class MapRemoveInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.MapAdd)]
	public sealed class MapAddInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MapReplace)]
	public sealed class MapReplaceInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Activated)]
	public sealed class ActivatedInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Collected)]
	public sealed class CollectedInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Spawn)]
	public sealed class SpawnInstruction:BaseSpawnInstruction
	{
		public override string ToStatement() => $"Spawn {SpawnStratProc.AsmLabel.SubroutineName()}, {LocalVarsToPop}, {LocalCount}, {TriggerCount}, {CollisionSize}, {CollisionBoneCount}";
	}

	[Opcode(InstructionOpcode.SpawnFrom)]
	public sealed class SpawnFromInstruction:BaseSpawnInstruction
	{
		public int BoneToSpawnFrom;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);

			var spawnFromAsmInstr = GetAsmInstruction<PSX.StratLang.SpawnFromInstruction>();
			BoneToSpawnFrom = spawnFromAsmInstr.BoneToSpawnFrom;
		}

		public override string ToStatement() => $"SpawnFrom {SpawnStratProc.AsmLabel.SubroutineName()}, {LocalVarsToPop}, {LocalCount}, {TriggerCount}, {CollisionSize}, {CollisionBoneCount}, {BoneToSpawnFrom}";
	}

	[Opcode(InstructionOpcode.Link)]
	public sealed class LinkInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Unlink)]
	public sealed class UnlinkInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SoundShift)]
	public sealed class SoundShiftInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 2;
		
		public IStackProducer Channel => Operands[0];
		public IStackProducer Pitch => Operands[1];

		public override string ToStatement() => $"SoundShift {Channel.ToExpressionString()}, {Pitch.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.SoundStop)]
	public sealed class SoundStopInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Channel => Operands[0];

		public override string ToStatement() => $"SoundStop {Channel.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.CdPlay)]
	public sealed class CdPlayInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MusicId => Operands[0];

		public override string ToStatement() => $"CdPlay {MusicId.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.MidiLoop)]
	public sealed class MidiLoopInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.MidiVolume)]
	public sealed class MidiVolumeInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.CdFade)]
	public sealed class CdFadeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer TimeLength => Operands[0];

		public override string ToStatement() => $"CdFade {TimeLength.ToExpressionString()}";
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

	public abstract class BaseCollisionInstruction(bool newState):SimpleNoStackInstruction
	{
		public uint CollisionFlag;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var collisionFlagAsmInstr = GetAsmInstruction<PSX.StratLang.CollisionFlagInstruction>();
			CollisionFlag = collisionFlagAsmInstr.CollisionType;
		}

		public override string ToStatement() => $"collision {(newState ? "on" : "off")} {CollisionFlag}";
	}

	[Opcode(InstructionOpcode.CollisionOn)]
	public sealed class CollisionOnInstruction():BaseCollisionInstruction(true);

	[Opcode(InstructionOpcode.CollisionOff)]
	public sealed class CollisionOffInstruction():BaseCollisionInstruction(false);

	[Opcode(InstructionOpcode.CollisionOffAll)]
	public sealed class CollisionOffAllInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SoundPlay3)]
	public sealed class SoundPlay3Instruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 3;

		public IStackProducer SoundIndex => Operands[0];
		public IStackProducer Volume => Operands[1];
		public IStackProducer Flags => Operands[2];

		public override string ToStatement() => $"SoundPlay {SoundIndex.ToExpressionString()}, {Volume.ToExpressionString()}, {Flags.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.SoundPlay4)]
	public sealed class SoundPlay4Instruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SoundPlay3ASS)]
	public sealed class SoundPlay3AssignmentInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 3;

		public IStackProducer SoundIndex => Operands[0];
		public IStackProducer Volume => Operands[1];
		public IStackProducer Flags => Operands[2];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToExpressionString()}, {Volume.ToExpressionString()}, {Flags.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.SoundPlay4ASS)]
	public sealed class SoundPlay4AssignmentInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.Int)]
	public sealed class IntInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"int({Value.ToExpressionString()})";
	}

	[Opcode(InstructionOpcode.Sin)]
	public sealed class SinInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"sin({Value.ToExpressionString()})";
	}

	[Opcode(InstructionOpcode.Cos)]
	public sealed class CosInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"cos({Value.ToExpressionString()})";
	}

	[Opcode(InstructionOpcode.Not)]
	public sealed class NotInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"NOT {Value.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.Pop)]
	public sealed class PopInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Discarded => Operands[0];

		public override string ToStatement() => $"Pop {Discarded.ToExpressionString()}";
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

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var addressAsmInstr = GetAsmInstruction<PSX.StratLang.AddressInstruction>();

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

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown)
		{
			if(IsAnimLoad)
			{
				return $"animload Anim_{AnimationIndex}";
			}
			else if(IsDataLoad)
			{
				return $"Address Offset {DataOffset}";
			}
			else
			{
				throw new Exception();
			}
		}
	}

	[Opcode(InstructionOpcode.Jsr)]
	public sealed class JsrInstruction:BaseJumpSubroutineInstruction
	{
		public override string ToStatement() => $"proc {Proc.AsmLabel.SubroutineName()} $ DONE";
	}

	[Opcode(InstructionOpcode.JsrImm)]
	public sealed class JsrImmInstruction:BaseJumpSubroutineInstruction
	{
		public override string ToStatement() => $"proc {Proc.AsmLabel.SubroutineName()} $ IMM";
	}

	[Opcode(InstructionOpcode.Return)]
	public sealed class ReturnInstruction:SimpleNoStackInstruction,IFlowTerminal
	{
		//Technically pop a values but we aren't counting it because it is pushed outisde the subroutine/trigger.

		public override bool Terminal => true;
	}

	[Opcode(InstructionOpcode.Beq)]
	public sealed class BeqInstruction:BranchInstruction
	{
		public override string ToStatement()
		{
			return $"if {Condition.ToExpressionString()} == 0 then goto {ConditionalDest.AsmLabel.GetLabel()} endif $ DONE";
		}
	}

	[Opcode(InstructionOpcode.Bne)]
	public sealed class BneInstruction:BranchInstruction
	{
		public override string ToStatement()
		{
			return $"if {Condition.ToExpressionString()} != 0 then goto {ConditionalDest.AsmLabel.GetLabel()} endif $ DONE";
		}
	}

	[Opcode(InstructionOpcode.BeqImm)]
	public sealed class BeqImmInstruction:BranchInstruction
	{
		public override string ToStatement()
		{
			return $"if {Condition.ToExpressionString()} == 0 then goto {ConditionalDest.AsmLabel.GetLabel()} endif $ IMM";
		}
	}

	[Opcode(InstructionOpcode.BneImm)]
	public sealed class BneImmInstruction:BranchInstruction
	{
		public override string ToStatement()
		{
			return $"if {Condition.ToExpressionString()} != 0 then goto {ConditionalDest.AsmLabel.GetLabel()} endif $ IMM";
		}
	}

	[Opcode(InstructionOpcode.JumpImm)]
	public sealed class JumpImmInstruction:BaseJumpInstruction
	{
		public override string ToStatement() => $"goto {Destination.AsmLabel.GetLabel()} $ IMM";
	}

	[Opcode(InstructionOpcode.EndStrat)]
	public sealed class EndStratInstruction:SimpleNoStackInstruction,IFlowTerminal
	{
		public override bool Terminal => true;
	}

	[Opcode(InstructionOpcode.IsPlayer)]
	public sealed class IsPlayerInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.And)]
	public sealed class AndInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToExpressionString()} AND {ValueB.ToExpressionString()})";
	}

	[Opcode(InstructionOpcode.Or)]
	public sealed class OrInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToExpressionString()} OR {ValueB.ToExpressionString()})";
	}

	[Opcode(InstructionOpcode.Index_Jump)]
	public sealed class Index_JumpInstruction:PureConsumeInstruction,IFlowSwitch
	{
		public override int PopCount => 1;

		public (int[] Comparands,Instruction Destination)[] Cases;

		public IStackProducer Value => Operands[0];

		public override IStackStatement ControlStatement => this;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var indexJumpAsmInstr = GetAsmInstruction<PSX.StratLang.IndexJumpInstruction>();

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
					cases.Add((new List<int> {comparand}, destination));
				}
			}
			Cases = cases.Select(c => (c.comparands.ToArray(), c.destination)).ToArray();

			if(Cases.Length == 0)
			{
				throw new Exception("Empty Switches are not supported and will break the FlowAnalyzer");
			}
		}

		public override void Analyze(StackAnalyzer stack)
		{
			base.Analyze(stack);

			foreach(var c in Cases)
			{
				stack.AddDest(c.Destination);
			}
		}

		public override void Analyze(FlowAnalyzer flow)
		{
			base.Analyze(flow);

			for(int i=0; i<Cases.Length; i++)
			{
				flow.AddDest(this, Cases[i].Destination);
			}
		}

		//public override string ToStatement() => throw new NotSupportedException();

		public override string ToStatement()
		{
			//TODO: This was commented out. Might fail.

			var ret = new StringBuilder();
			ret.AppendLine($"switch {Value.ToExpressionString()}");
			foreach(var c in Cases)
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

	[Opcode(InstructionOpcode.BitwiseAnd)]
	public sealed class BitwiseAndInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToIntStr()} & {ValueB.ToIntStr()})";
	}

	[Opcode(InstructionOpcode.Ext_Local)]
	public sealed class Ext_LocalInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Parent;
		public override VarType Type => VarType.Local;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Ext_LocalAddress)]
	public sealed class Ext_LocalAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Parent;
		public override VarType Type => VarType.Local;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Ext_Global)]
	public sealed class Ext_GlobalInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.Ext_GlobalAddress)]
	public sealed class Ext_GlobalAddressInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.ObjectJump)]
	public sealed class ObjectJumpInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer VerticalVelocity => Operands[0];

		public override string ToStatement() => $"ObjectJump {VerticalVelocity.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Ext_AlienVar)]
	public sealed class Ext_AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Parent;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Ext_AlienVarAddress)]
	public sealed class Ext_AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Parent;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.NotEqual)]
	public sealed class NotEqualInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} != {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.ShiftLeft)]
	public sealed class ShiftLeftInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} << {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.ShiftRight)]
	public sealed class ShiftRightInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} >> {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.AnimAdvance)]
	public sealed class AnimAdvanceInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer DesiredIndex => Operands[0];

		public override string ToStatement() => $"AnimAdvance {DesiredIndex.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.GreaterEqual)]
	public sealed class GreaterEqualInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} >= {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.LessEqual)]
	public sealed class LessEqualInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"({ValueA.ToFxString()} <= {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Rnd)]
	public sealed class RndInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxExclusive => Operands[0];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"rnd({MaxExclusive.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Blink)]
	public sealed class BlinkInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => Count;
		
		public int Count;

		//This is just the operands but in reverse
		public IStackProducer[] Bones => Operands.Reverse().ToArray();

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var blinkAsmInstr = GetAsmInstruction<PSX.StratLang.BlinkInstruction>();

			Count = blinkAsmInstr.Count;
		}

		public override string ToStatement() => $"blink {string.Join(", ", Bones.Select(b => b.ToFxString()))}";
	}

	[Opcode(InstructionOpcode.LoseHeart)]
	public sealed class LoseHeartInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ResetToCheckPoint)]
	public sealed class ResetToCheckPointInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ForceCollision)]
	public sealed class ForceCollisionInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.TurnFromPlayerY)]
	public sealed class TurnFromPlayerYInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer MaxTurnSpeed => Operands[0];

		public override string ToStatement() => $"TurnFromPlayerY {MaxTurnSpeed.ToFxString()}";
	}

	[Opcode(InstructionOpcode.PlayerAttack)]
	public sealed class PlayerAttackInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Rumble)]
	public sealed class RumbleInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 2;

		public IStackProducer Rumble => Operands[0];
		public IStackProducer RumbleDecay => Operands[1];

		public override string ToStatement() => $"Rumble {Rumble.ToFxString()}, {RumbleDecay.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Vibrate)]
	public sealed class VibrateInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Vibrate => Operands[0];

		public override string ToStatement() => $"Vibrate {Vibrate.ToFxString()}";
	}

	[Opcode(InstructionOpcode.SuspendIfTooFar)]
	public sealed class SuspendIfTooFarInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.CollisionBone)]
	public sealed class CollisionBoneInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 2;

		public IStackProducer Bone => Operands[0];
		public IStackProducer Radius => Operands[1];

		public override string ToStatement() => $"CollisionBone {Bone.ToFxString()}, {Radius.ToFxString()}";
	}

	[Opcode(InstructionOpcode.UseBone)]
	public sealed class UseBoneInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Frame => Operands[0];

		public override string ToStatement() => $"UseBone {Frame.ToFxString()}";
	}

	[Opcode(InstructionOpcode.IsCamera)]
	public sealed class IsCameraInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.LookAtMe)]
	public sealed class LookAtMeInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.LookAtMe2)]
	public sealed class LookAtMe2Instruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.PushCamera)]
	public sealed class PushCameraInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.PopCamera)]
	public sealed class PopCameraInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ResetCameraPos)]
	public sealed class ResetCameraPosInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GainHeart)]
	public sealed class GainHeartInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GainHeartPot)]
	public sealed class GainHeartPotInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.AddInv)]
	public sealed class AddInvInstruction:ItemChangeInstruction
	{
		public override int Change => +1;
	}

	[Opcode(InstructionOpcode.GainCrystal)]
	public sealed class GainCrystalInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer CrystalType => Operands[0];

		public override string ToStatement() => $"GainCrystal {CrystalType.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.Cutscene)]
	public sealed class CutsceneInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer CutsceneAddr => Operands[0];

		public override string ToStatement() => $"Cutscene {CutsceneAddr.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.Inventory)]
	public sealed class InventoryInstruction:PureProducerInstruction
	{
		//Item count
		public int Item;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var inventoryAsmInstr = GetAsmInstruction<PSX.StratLang.ItemCountInstruction>();

			Item = inventoryAsmInstr.Item;
		}

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"inventory({Item})";
	}

	[Opcode(InstructionOpcode.DebugName)]
	public sealed class DebugNameInstruction:SimpleNoStackInstruction
	{
		public string Name;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var debugNameAsmInstr = GetAsmInstruction<PSX.StratLang.DebugNameInstruction>();

			Name = debugNameAsmInstr.Name;
		}

		public override string ToStatement() => $"DebugName \"{Name}\"";
	}

	[Opcode(InstructionOpcode.PlayerDistanceCheck)]
	public sealed class PlayerDistanceCheckInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.SoundPlay1)]
	public sealed class SoundPlay1Instruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer SoundIndex => Operands[0];

		public override string ToStatement() => $"SoundPlay {SoundIndex.ToIntStr()}";
	}

	[Opcode(InstructionOpcode.SoundPlay1ASS)]
	public sealed class SoundPlay1AssignmentInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 1;

		public IStackProducer SoundIndex => Operands[0];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToIntStr()}";
	}

	[Opcode(InstructionOpcode.SoundAddress)]
	public sealed class SoundAddressInstruction:PureProducerInstruction
	{
		public int Value;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var soundAddrAsmInstr = GetAsmInstruction<PSX.StratLang.SoundAddressInstruction>();

			Value = soundAddrAsmInstr.Value;
		}

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundAddress {Value}";
	}

	[Opcode(InstructionOpcode.OnGround)]
	public sealed class OnGroundInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.ObjectFallSlow)]
	public sealed class ObjectFallSlowInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Player_AlienVar)]
	public sealed class Player_AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Player;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Player_AlienVarAddress)]
	public sealed class Player_AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Player;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.CollisionOffset)]
	public sealed class CollisionOffsetInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 3;

		public IStackProducer X => Operands[0];
		public IStackProducer Y => Operands[1];
		public IStackProducer Z => Operands[2];

		public override string ToStatement() => $"CollisionOffset {X.ToFxString()}, {Y.ToFxString()}, {Z.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Abs)]
	public sealed class AbsInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"abs({Value.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Pickup)]
	public sealed class PickupInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Min)]
	public sealed class MinInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"min({ValueA.ToFxString()}, {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.Max)]
	public sealed class MaxInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"max({ValueA.ToFxString()}, {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.SpawnParticle)]
	public sealed class SpawnParticleInstruction:SimplePureConsumeInstruction
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
			if(Type is NumberInstruction typeNum)
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

	[Opcode(InstructionOpcode.Sgn)]
	public sealed class SgnInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"sgn({Value.ToFxString()})";
	}

	[Opcode(InstructionOpcode.SpawnAfter)]
	public sealed class SpawnAfterInstruction:BaseSpawnInstruction
	{
		public override string ToStatement() => $"SpawnAfter {SpawnStratProc.AsmLabel.SubroutineName()}, {LocalVarsToPop}, {LocalCount}, {TriggerCount}, {CollisionSize}, {CollisionBoneCount}";
	}

	[Opcode(InstructionOpcode.Camera_AlienVar)]
	public sealed class Camera_AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Camera;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Camera_AlienVarAddress)]
	public sealed class Camera_AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Camera;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Target_AlienVar)]
	public sealed class Target_AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Target;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Target_AlienVarAddress)]
	public sealed class Target_AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Target;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.Collide_AlienVar)]
	public sealed class Collide_AlienVarInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.Collide_AlienVarAddress)]
	public sealed class Collide_AlienVarAddressInstruction:UnimplementedInstruction;//VarInstruction

	[Opcode(InstructionOpcode.Target2_AlienVar)]
	public sealed class Target2_AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Target2;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Target2_AlienVarAddress)]
	public sealed class Target2_AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Target2;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.DontLookAtMe)]
	public sealed class DontLookAtMeInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.RunAt60)]
	public sealed class RunAt60Instruction():UnimplementedInstruction(fail:false);

	[Opcode(InstructionOpcode.MoveForwardq)]
	public sealed class MoveForwardqInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveForwardQ {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveBackwardq)]
	public sealed class MoveBackwardqInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveBackwardQ {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.ScreenPrint)]
	public sealed class ScreenPrintInstruction:BasePrintInstruction
	{
		public override string ToStatement() => $"screenprint {Data}";
	}

	[Opcode(InstructionOpcode.SoundPlay2)]
	public sealed class SoundPlay2Instruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 2;

		public IStackProducer SoundIndex => Operands[0];
		public IStackProducer Volume => Operands[1];

		public override string ToStatement() => $"SoundPlay {SoundIndex.ToIntStr()}, {Volume.ToFxString()}";
	}

	[Opcode(InstructionOpcode.SoundPlay2ASS)]
	public sealed class SoundPlay2AssignmentInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 2;

		public IStackProducer SoundIndex => Operands[0];
		public IStackProducer Volume => Operands[1];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SoundPlay {SoundIndex.ToIntStr()}, {Volume.ToFxString()}";
	}

	[Opcode(InstructionOpcode.SetWP)]
	public sealed class SetWPInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ResetWP)]
	public sealed class ResetWPInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SoundVolume)]
	public sealed class SoundVolumeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 2;

		public IStackProducer Channel => Operands[0];
		public IStackProducer Volume => Operands[1];

		public override string ToStatement() => $"SoundVolume {Channel.ToIntStr()}, {Volume.ToFxString()}";
	}

	[Opcode(InstructionOpcode.Push)]
	public sealed class PushInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.String)]
	public sealed class StringInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.SetBossHearts)]
	public sealed class SetBossHeartsInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Health => Operands[0];

		public override string ToStatement() => $"SetBossHearts {Health.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.LoseBossHeart)]
	public sealed class LoseBossHeartInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SoundShiftRelative)]
	public sealed class SoundShiftRelativeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 2;

		public IStackProducer Channel => Operands[0];
		public IStackProducer Pitch => Operands[1];

		public override string ToStatement() => $"SoundShiftRelative {Channel.ToExpressionString()}, {Pitch.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.Smin)]
	public sealed class SminInstruction:BinaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"smin({ValueA.ToFxString()}, {ValueB.ToFxString()})";
	}

	[Opcode(InstructionOpcode.IsBoss)]
	public sealed class IsBossInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.TopSay)]
	public sealed class TopSayInstruction:DialogSayInstruction
	{
		public override string ToStatement() => $"TopSay \"{EnglishString}\"";
	}

	[Opcode(InstructionOpcode.Boss_AlienVar)]
	public sealed class Boss_AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Boss;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Boss_AlienVarAddress)]
	public sealed class Boss_AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Boss;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.GetParentPos)]
	public sealed class GetParentPosInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.AfterBoss)]
	public sealed class AfterBossInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.AfterPlayer)]
	public sealed class AfterPlayerInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.BeforePlayer)]
	public sealed class BeforePlayerInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.BeforeBoss)]
	public sealed class BeforeBossInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.NoHang)]
	public sealed class NoHangInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Zero)]
	public sealed class ZeroInstruction:PureProducerInstruction
	{
		public override bool Literal => true;

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => "0";
	}

	[Opcode(InstructionOpcode.TopHead)]
	public sealed class TopHeadInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Sprite => Operands[0];

		public override string ToStatement() => $"TopHead {Sprite.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.TopDialog)]
	public sealed class TopDialogInstruction:DialogSetInstruction
	{
		public override string ToStatement() => State ? "TopDialog on" : "TopDialog off";
	}

	[Opcode(InstructionOpcode.BottomSay)]
	public sealed class BottomSayInstruction:DialogSayInstruction
	{
		public override string ToStatement() => $"BottomSay \"{EnglishString}\"";
	}

	[Opcode(InstructionOpcode.BottomHead)]
	public sealed class BottomHeadInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Sprite => Operands[0];

		public override string ToStatement() => $"BottomHead {Sprite.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.BottomDialog)]
	public sealed class BottomDialogInstruction:DialogSetInstruction
	{
		public override string ToStatement() => State ? "BottomDialog on" : "BottomDialog off";
	}

	[Opcode(InstructionOpcode.GetPlayerPos)]
	public sealed class GetPlayerPosInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GetWPpos)]
	public sealed class GetWPposInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GetBossPos)]
	public sealed class GetBossPosInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GetDoorPos)]
	public sealed class GetDoorPosInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.FadeOut)]
	public sealed class FadeOutInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer FadeType => Operands[0];

		public override string ToStatement() => $"FadeOut {FadeType.ToFxString()}";
	}

	[Opcode(InstructionOpcode.FadeIn)]
	public sealed class FadeInInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer FadeType => Operands[0];

		public override string ToStatement() => $"FadeIn {FadeType.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveUpq)]
	public sealed class MoveUpqInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveUpQ {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveDownq)]
	public sealed class MoveDownqInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveDownQ {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.ForcePlayerDist)]
	public sealed class ForcePlayerDistInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ShadeType)]
	public sealed class ShadeTypeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ShadeType => Operands[0];

		public override string ToStatement() => $"ShadeType {ShadeType.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.NOP)]
	public sealed class NOPInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SetAnimSpeed)]
	public sealed class SetAnimSpeedInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer AnimSpeed => Operands[0];

		public override string ToStatement() => $"SetAnimSpeed {AnimSpeed.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.CheckLevelDoor)]
	public sealed class CheckLevelDoorInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.BottomHeadLeft)]
	public sealed class BottomHeadLeftInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.TopHeadLeft)]
	public sealed class TopHeadLeftInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GainJigsaw)]
	public sealed class GainJigsawInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GainGoldenGobbo)]
	public sealed class GainGoldenGobboInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Gain100Crystal)]
	public sealed class Gain100CrystalInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ResetSpline)]
	public sealed class ResetSplineInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.CheckPoint)]
	public sealed class CheckPointInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WaterTest)]
	public sealed class WaterTestInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.IsMainCamera)]
	public sealed class IsMainCameraInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ResetDialog)]
	public sealed class ResetDialogInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.EndLevel)]
	public sealed class EndLevelInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Dialog_AlienVar)]
	public sealed class Dialog_AlienVarInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Dialog;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => false;
	}

	[Opcode(InstructionOpcode.Dialog_AlienVarAddress)]
	public sealed class Dialog_AlienVarAddressInstruction:VarInstruction
	{
		public override SourceStrat Source => SourceStrat.Dialog;
		public override VarType Type => VarType.Alien;
		public override bool GetAddress => true;
	}

	[Opcode(InstructionOpcode.IsDialog)]
	public sealed class IsDialogInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Distance)]
	public sealed class DistanceInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 3;

		public IStackProducer X => Operands[0];
		public IStackProducer Y => Operands[1];
		public IStackProducer Z => Operands[2];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"Distance {X.ToExpressionString()}, {Y.ToExpressionString()}, {Z.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.Binocs)]
	public sealed class BinocsInstruction:SimpleNoStackInstruction
	{
		public bool State;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var binocsAsmInstr = GetAsmInstruction<PSX.StratLang.BinocsInstruction>();

			State = binocsAsmInstr.State switch
			{
				1 => true,
				0 => false,
				_ => throw new Exception("Invalid binoc state")
			};
		}

		public override string ToStatement() =>  State ? "Binocs on" : "Binocs off";
	}

	[Opcode(InstructionOpcode.TopCloseDialog)]
	public sealed class TopCloseDialogInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.BottomCloseDialog)]
	public sealed class BottomCloseDialogInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.NextInventory)]
	public sealed class NextInventoryInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.PrevInventory)]
	public sealed class PrevInventoryInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.OtherPiece)]
	public sealed class OtherPieceInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.NormalPiece)]
	public sealed class NormalPieceInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Climb)]
	public sealed class ClimbInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.DelInv)]
	public sealed class DelInvInstruction:ItemChangeInstruction
	{
		public override int Change => -1;
	}

	[Opcode(InstructionOpcode.GainReward)]
	public sealed class GainRewardInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.WorldVector)]
	public sealed class WorldVectorInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 3;
		
		//This is the correct pop order
		public IStackProducer Z => Operands[0];
		public IStackProducer X => Operands[1];
		public IStackProducer Angle => Operands[2];

		public override string ToStatement() => $"WorldVector {Z.ToFxString()}, {X.ToFxString()}, {Angle.ToFxString()}";
	}

	[Opcode(InstructionOpcode.ObjectFallVerySlow)]
	public sealed class ObjectFallVerySlowInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Slope2Controller)]
	public sealed class Slope2ControllerInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Unused => Operands[0];

		public override string ToStatement()
		{ 
			if(Unused is not NumberInstruction num || num.Value != 0)
			{
				throw new Exception("Slope2Controller has non-zero operand");
			}
			return $"Slope2Controller {Unused.ToIntStr()}";
		}
	}

	[Opcode(InstructionOpcode.LevelComplete)]
	public sealed class LevelCompleteInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 3;
		
		public IStackProducer Tribe => Operands[0];
		public IStackProducer Level => Operands[1];
		public IStackProducer Type => Operands[2];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"LevelComplete {Tribe.ToExpressionString()}, {Level.ToExpressionString()}, {Type.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.SetLevelFlag)]
	public sealed class SetLevelFlagInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer LevelFlag => Operands[0];

		public override string ToStatement() => $"SetLevelFlag {LevelFlag.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.GetLevelFlag)]
	public sealed class GetLevelFlagInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 3;
		
		public IStackProducer Tribe => Operands[0];
		public IStackProducer Level => Operands[1];
		public IStackProducer Type => Operands[2];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"GetLevelFlag {Tribe.ToExpressionString()}, {Level.ToExpressionString()}, {Type.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.CalcCarTilt)]
	public sealed class CalcCarTiltInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 4;
		
		//Notice the backwards numbering
		public IStackProducer Op4 => Operands[0];
		public IStackProducer Op3 => Operands[1];
		public IStackProducer Op2 => Operands[2];
		public IStackProducer Op1 => Operands[3];

		public override string ToStatement() => $"CalcCarTilt {Op4.ToExpressionString()}, {Op3.ToExpressionString()}, {Op2.ToExpressionString()}, {Op1.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.MoveLeftq)]
	public sealed class MoveLeftqInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveLeftQ {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.MoveRightq)]
	public sealed class MoveRightqInstruction:BaseMoveInstruction
	{
		public override string ToStatement() => $"MoveRightQ {Amount.ToFxString()}";
	}

	[Opcode(InstructionOpcode.BitwiseNot)]
	public sealed class BitwiseNotInstruction:UnaryOperationInstruction
	{
		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"~{Value.ToIntStr()}";
	}

	[Opcode(InstructionOpcode.BordersOn)]
	public sealed class BordersOnInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.BordersOff)]
	public sealed class BordersOffInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SoundAdsr)]
	public sealed class SoundAdsrInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 5;

		public IStackProducer Channel => Operands[0];
		public IStackProducer A => Operands[1];
		public IStackProducer D => Operands[2];
		public IStackProducer S => Operands[3];
		public IStackProducer R => Operands[4];

		public override string ToStatement() => $"SoundAdsr {Channel.ToExpressionString()}, {A.ToExpressionString()}, {D.ToExpressionString()}, {S.ToExpressionString()}, {R.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.SoundAdsrRelative)]
	public sealed class SoundAdsrRelativeInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 5;

		public IStackProducer Channel => Operands[0];
		public IStackProducer A => Operands[1];
		public IStackProducer D => Operands[2];
		public IStackProducer S => Operands[3];
		public IStackProducer R => Operands[4];

		public override string ToStatement() => $"SoundAdsrRelative {Channel.ToExpressionString()}, {A.ToExpressionString()}, {D.ToExpressionString()}, {S.ToExpressionString()}, {R.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.RotatePiece)]
	public sealed class RotatePieceInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SetAmbient)]
	public sealed class SetAmbientInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.ResetAmbient)]
	public sealed class ResetAmbientInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.InvActive)]
	public sealed class InvActiveInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.InvInactive)]
	public sealed class InvInactiveInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SampleStatus)]
	public sealed class SampleStatusInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ID => Operands[0];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"SampleStatus {ID.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.ResetToCheckPointnlh)]
	public sealed class ResetToCheckPointnlhInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ResetDoor)]
	public sealed class ResetDoorInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.StoreDoor)]
	public sealed class StoreDoorInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Camera_modified)]
	public sealed class Camera_modifiedInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.PushPlayer)]
	public sealed class PushPlayerInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.PopPlayer)]
	public sealed class PopPlayerInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ReSetPostrn)]
	public sealed class ReSetPostrnInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.GainItem)]
	public sealed class GainItemInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SetItem)]
	public sealed class SetItemInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer ExtraMax => Operands[0];

		public override string ToStatement() => $"SetItem {ExtraMax.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.SetTimer)]
	public sealed class SetTimerInstruction:SimplePureConsumeInstruction
	{
		public override int PopCount => 1;

		public IStackProducer Timer => Operands[0];

		public override string ToStatement() => $"SetTimer {Timer.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.TimerOff)]
	public sealed class TimerOffInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.DistanceNoY)]
	public sealed class DistanceNoYInstruction:BaseExpressionInstruction
	{
		public override int PopCount => 2;

		public IStackProducer X => Operands[0];
		public IStackProducer Z => Operands[1];

		public override string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown) => $"DistanceNoY {X.ToExpressionString()}, {Z.ToExpressionString()}";
	}

	[Opcode(InstructionOpcode.Swim)]
	public sealed class SwimInstruction:FlagInstruction;

	[Opcode(InstructionOpcode.Lose100Crystals)]
	public sealed class Lose100CrystalsInstruction:UnimplementedInstruction;

	[Opcode(InstructionOpcode.LoseReward)]
	public sealed class LoseRewardInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.LoseGoldenGobbo)]
	public sealed class LoseGoldenGobboInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.NextTribe)]
	public sealed class NextTribeInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.PrevTribe)]
	public sealed class PrevTribeInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SetTimerClock)]
	public sealed class SetTimerClockInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.SetTimerBomb)]
	public sealed class SetTimerBombInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.InitBurpingGame)]
	public sealed class InitBurpingGameInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.CloseBurpingGame)]
	public sealed class CloseBurpingGameInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Credit)]
	public sealed class CreditInstruction:SimpleNoStackInstruction
	{
		public int Operand;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var creditAsmInstr = GetAsmInstruction<PSX.StratLang.CreditInstruction>();

			Operand = creditAsmInstr.Operand;
		}

		public override string ToStatement() => $"Credit {Operand}";
	}

	[Opcode(InstructionOpcode.CloseCredits)]
	public sealed class CloseCreditsInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ShowRewardCard)]
	public sealed class ShowRewardCardInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.ShowHearts)]
	public sealed class ShowHeartsInstruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.Cwg)]
	public sealed class CwgInstruction:SimpleNoStackInstruction
	{
		public int Value;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var cwgAsmInstr = GetAsmInstruction<PSX.StratLang.CwgInstruction>();

			Value = cwgAsmInstr.Value;
		}

		public override string ToStatement() => $"Cwg {Value}";
	}

	[Opcode(InstructionOpcode.FadeFunction_47E960)]
	public sealed class FadeFunction_47E960Instruction:SimpleNoStackInstruction
	{
		public int Value;

		public override void Setup(AsmParser parser)
		{
			base.Setup(parser);
			var fadeSetUnknownAsmInstr = GetAsmInstruction<PSX.StratLang.FadeSetUnknownInstruction>();
			Value = fadeSetUnknownAsmInstr.Value;
		}

		public override string ToStatement() => $"FadeFunction_47E960 {Value}";
	}

	[Opcode(InstructionOpcode.CameraFunction_47F1E0)]
	public sealed class CameraFunction_47F1E0Instruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.CameraFunction_47F040)]
	public sealed class CameraFunction_47F040Instruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.CameraFunction_47F0C0)]
	public sealed class CameraFunction_47F0C0Instruction:SimpleNoStackInstruction;

	[Opcode(InstructionOpcode.CameraFunction_47F490)]
	public sealed class CameraFunction_47F490Instruction:SimpleNoStackInstruction;
}