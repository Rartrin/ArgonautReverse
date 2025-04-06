using System.Diagnostics.CodeAnalysis;
using ArgonautReverse.PSX.StratLang;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	//Terms:
	//Consumers pop values off the start
	//Producers push values on the stack
	//Pure Consumers/Producers only consume/produce

	//Conjectures:
	//The stack should be clear after a pure consumer is finished (a consumer that does not push).
	//The stack should be clear before a NoStack operation (an operation that doesn't effect the stack).
	//The stack should be clear before a jump/branch/switch/call (which the exception of call return address).
	//The stack should be clear when returning (which the exception of call return address).
	//Producers should have no side-effects. Exception for SoundPlayAssignments but the results MUST be used in assignment.
	//Labels should only be on non-consumers.

	//Only non-producers will appear as a statement, all other operations get condenced into these.

	public enum ExpressionType
	{
		Unknown = 0,
		Integer,
		FixedPoint,
		Dereference,
	}

	//This is not always a operand but can also indicate a statement that effects the analyzer
	public interface IStackOperation
	{
		public abstract Instruction OperationInstruction{get;}

		public abstract IStackStatement Statement{get;}

		public abstract void Analyze(StackAnalyzer stack);

		public abstract IEnumerable<IStackOperation> GetRootOperations();
	}
	public interface IStackProducer:IStackOperation
	{
		public abstract IStackConsumer Consumer{get;set;}

		public virtual bool Literal => false;
		
		//deref = false means get the address
		//deref = true means use the value
		//getFixedPoint = false means numbers will be treated as integers and not be converted
		//getFixedPoint = true means numbers will be treated as fixed point and converted into decimal notation
		public abstract string ToExpressionString(ExpressionType requestType = ExpressionType.Unknown);

		public string ToIntStr() => ToExpressionString(ExpressionType.Integer);
		public string ToFxString() => ToExpressionString(ExpressionType.FixedPoint);
		public string ToDerefStr() => ToExpressionString(ExpressionType.Dereference);
	}
	public interface IStackConsumer:IStackOperation
	{
		public int PopCount{get;}

		public IStackProducer[] Operands{get;}
	}

	public interface IStackExpression:IStackProducer,IStackConsumer{}

	//This represents a statement which can be a pure consumer or a NoStack operation
	//In other words, a non-producer
	public interface IStackStatement:IStackOperation
	{
		public abstract AsmInstruction StatementLabel{get;}
		public abstract Instruction FirstInstruction{get;}

		//The specific instruction ending this expression chain.
		public abstract Instruction StatementInstruction{get;}

		public abstract IFlowStatement FlowStatement{get;}

		public abstract IStackStatement NextStatement{get;set;}
		public abstract IStackStatement PrevStatement{get;set;}

		public abstract string ToStatement();

		public abstract bool TryGetSubroutine([MaybeNullWhen(false)]out AsmInstruction subroutine);
		public abstract bool TryGetLabel([MaybeNullWhen(false)]out AsmInstruction label);
	}

	public sealed class StackAnalyzer
	{
		private readonly Stack<IStackProducer> stack = new Stack<IStackProducer>();

		private readonly HashSet<int> analyzed = new HashSet<int>();

		private readonly Queue<Instruction> pending = new Queue<Instruction>();

		private readonly List<IStackStatement> statements = new List<IStackStatement>();

		public readonly List<IStackStatement> Subroutines = new List<IStackStatement>();

		public Instruction CurrentStatementFirstInstruction{get;private set;}

		public void Push(IStackProducer operand)
		{
			stack.Push(operand);
		}

		public IStackProducer Pop()
		{
			return stack.Pop();
		}

		public void AssertStackEmpty()
		{
			if(stack.Count!=0)
			{
				throw new Exception("Stack not empty");
			}
		}

		public void AddDest(Instruction dest)
		{
			AssertStackEmpty();

			pending.Enqueue(dest);
		}

		private void AnalyzeDestination(Instruction dest)
		{
			IStackStatement? prevStatement = null;
			var cur = dest;

			CurrentStatementFirstInstruction = cur;
			while(true)
			{
				bool alreadyAnalyzed = analyzed.Contains(cur.Index);

				if(alreadyAnalyzed)
				{
					var stackStatement = ((IStackOperation)cur).Statement;
					if(prevStatement != null)
					{
						stackStatement.PrevStatement = prevStatement;
						prevStatement.NextStatement = stackStatement;
					}
					break;
				}

				if (cur is IStackOperation operation)
				{
					operation.Analyze(this);
					analyzed.Add(cur.Index);
				}
				if(cur is IStackStatement statement)
				{
					AssertStackEmpty();
					statements.Add(statement);

					if(prevStatement != null)
					{
						statement.PrevStatement = prevStatement;
						prevStatement.NextStatement = statement;
					}
					prevStatement = statement;

					if(!cur.Terminal)
					{
						CurrentStatementFirstInstruction = cur.AsmNext;
					}
				}
				if(cur.Terminal)
				{
					break;
				}
				cur = cur.AsmNext;
			}
		}

		public void Analyze(IReadOnlyList<Instruction> subroutines)
		{
			foreach(var subroutine in subroutines)
			{
				Analyze(subroutine);
			}
		}
		public void Analyze(Instruction subroutine)
		{
			AddDest(subroutine);

			while(pending.TryDequeue(out var dest))
			{
				AnalyzeDestination(dest);
			}
			Subroutines.Add(((IStackOperation)subroutine).Statement);
		}

		public void Write(List<string> lines)
		{
			var sortedStatements = statements.OrderBy(s => s.StatementInstruction.Index);

			foreach(var statement in sortedStatements)
			{
				if(statement.TryGetSubroutine(out var subroutine))
				{
					lines.Add(subroutine.SubroutineName() + ":");
					if(statement.StatementLabel != subroutine)
					{
						throw new Exception();
					}
				}
				else if(statement.StatementLabel.IsSubroutineEntry)
				{
					throw new Exception();
				}

				if(statement.TryGetLabel(out var label))
				{
					lines.Add(label.GetLabel() + ":");
					if(statement.StatementLabel != label)
					{
						throw new Exception();
					}
				}
				else if(statement.StatementLabel.HasLabel)
				{
					throw new Exception();
				}
				
				lines.Add("\t" + statement.ToStatement());
			}
		}
	}
}