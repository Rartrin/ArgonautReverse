using System.Diagnostics.CodeAnalysis;

namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	//Breaks are only Jumps. They will still be surrounded by conditionals.


	//Jumps forward indicate:
	//-Else
	//-WhileBreak
	//-RepeatBreak
	//-BreakLoop
	//-CaseBreak
	//-ProcBreak
	//-EndCase
	//-Switch (default case)

	//Jumps backward indicate:
	//-EndWhile
	//-EndLoop

	//Branch backward indicates:
	//-Until

	//Branch forwards indicates:
	//-If
	//-Else
	//-While



	//Identifying things:


	//Nevermind?
	//This method won't work, use the old way with Backward Jumps instead



	// -While - Forward Branch to a statement where statement.Prev is a Backward Jump to While
	// -EndWhile - Backward Jump
	// -WhileBreak - Forward Jump to EndWhile.Next

	//-EndLoop - Backward Jump and isn't a While
	//-Loop - EndLoop goes to the start of Loop
	//-BreakLoop - Forward Jump to EndLoop.Next

	//-Until - Backward Branch
	//-Repeat - Until goes to the start of Repeat
	//-RepeatBreak - Forward Jump to Until.Next

	//-Switch - Declared
	//-Case - Declared
	//-EndCase - Following Case, Jump to either Switch.Next or anything after it
	//-CaseBreak - Following Case, Jump to either Switch.Next or anything after it
	//-Switch-Default - Switch.Next but only if the EndCases Jump to a value after Switch.Next

	//-ProcBreak - Forward Jump to Return
	//-If-Else - Forward branch to a statement where statement.Prev is a Forward Jump

	//Have different node types that can either be contained, or produce an external jump?





	//Self referencing loops are possible, like a Repeat or Loop without any other flow controls.


	//Analyze via sequence or linked nodes?


	//Trace through and find loops/connections, then reconstruct?


	public enum FlowStatementType
	{
		Unknown = 0,

		Simple,//No flow control
		
		If,
		Else,
		EndIf,

		While,
		WhileBreak,
		EndWhile,

		Loop,
		BreakLoop,
		EndLoop,

		Repeat,
		RepeatBreak,
		Until,

		Switch,
		EndSwitch,
		Case,
		CaseBreak,
		EndCase,
		DefaultCase,
		SwitchEndJump,
		
		StartSubroutine,//Proc, Trigger, Strat
		BreakSubroutine,
		EndSubroutine,//EndProc, TriggerEnd, EndStrat

		ProgramStart,
		ProgramEnd,
	}

	public abstract class FlowData
	{
		public BlockFlowData Scope{get;internal set;}

		public abstract FlowStatementType StartType{get;}
		public readonly FlowStatement Start;

		public FlowData(FlowStatement start)
		{
			if(LabelStart)
			{
				start.SetPreFlow(StartType, this);
			}
			else
			{
				start.SetFlow(StartType, this);
			}
			Start = start;
		}

		//Indicates that the start is based on a label and is not associated with the specifics of the instruction.
		public abstract bool LabelStart{get;}

		public bool IsStart(FlowStatement statement, FlowStatementType statementType)
		{
			return statement == Start && statementType == StartType;
		}

		//Returns true when new things have been identified. Returns false when there is nothing left to identify.
		public static bool TryIdentifyFlows(FlowStatement flow, BlockFlowData scope)
		{
			//Already identified FlowType
			if (flow.FlowType != FlowStatementType.Unknown)
			{
				return false;
			}

			if(SubroutineFlowData.TryIdentifySubroutine(flow, scope, out var subroutineFlowData))
			{
				return true;
			}

			//if (flow is ReturnInstruction or EndStratInstruction)
			//{
			//	flowData.Add(new EndSubroutineFlowData(flow));
			//	return true;
			//}
			//Unconditional Jump to a return is a ProcBreak
			//TODO: Should probably go after the switch but need to make sure it will still work
			//if(flow is IFlowGoto jump)
			//{
			//	if(jump.FlowDestination.StackStatement is ReturnInstruction)
			//	{
			//		//Gets catch up on Else and breaks without this
			//		if(flow.FlowType == FlowStatementType.Unknown)
			//		{
			//			flowData.Add(new ProcBreakFlowData(flow));
			//			return true;
			//		}
			//	}
			//}

			//An unidentified unconditional forward jump is a Break
			if(flow is IFlowJump jump && flow.StatementIndex<jump.FlowDestination.StatementIndex)
			{
				//TODO: Anything needed here?
			}

			if(SwitchFlowData.TryIdentifySwitchFlow(flow, scope, out var switchFlowData))
			{
				return true;
			}

			//Backward jumps and branches indicate while, repeat, and loop
			//TODO: Support multiple. In theory, there can be multiple when nesting loops/repeats
			var backwardFlow = flow.FlowSources.Where(s => flow.StatementIndex <= s.StatementIndex).SingleOrDefault();

			//If it exists and it has not already been identified.
			if(backwardFlow != null && backwardFlow.FlowType == FlowStatementType.Unknown)
			{
				//While, Repeat, Loop

				bool conditionalStart = flow is IFlowControl flowControl && flowControl.ControlStatement.FlowStatement is IFlowBranch conditionalStartBranch && conditionalStartBranch.FlowConditionalDest == backwardFlow.RawNextFlow;
				bool conditionalEnd = backwardFlow is IFlowControl backwardFlowControl && backwardFlowControl.ControlStatement.FlowStatement is IFlowBranch;

				BreakableBlockFlowData blockFlow;
				if(conditionalStart && !conditionalEnd)
				{
					blockFlow = new WhileFlowData(flow, backwardFlow);
				}
				else if(!conditionalStart && !conditionalEnd)
				{
					blockFlow = new LoopFlowData(flow, backwardFlow);
				}
				else if((!conditionalStart && conditionalEnd) || (flow==backwardFlow))
				{
					blockFlow = new RepeatFlowData(flow, backwardFlow);
					//It is possible for a loop to just be a single branch statement the loops on itself.
				}
				else
				{
					throw new Exception("Unknown block type");
				}

				//Breaks go to the statement after the end jump
				var breakDest = backwardFlow.RawNextFlow;

				foreach(var source in breakDest.FlowSources)
				{
					//A jump to the break destination that happens within the scope of the while is considered a break.
					if(flow.StatementIndex<=source.StatementIndex && source.StatementIndex<backwardFlow.StatementIndex)
					{
						blockFlow.AddBreak(source);
					}
				}
				scope.AddSubflow(blockFlow);
				return true;
			}
			if(flow is IFlowBranch branch)
			{
				//TODO: Better way to handle breaks. We should check if the jump happens within the current scope.
				if(branch.FlowConditionalDest.RawPrevFlow is IFlowJump jumpToEnd && jumpToEnd.FlowStatement.FlowType == FlowStatementType.Unknown)
				{
					//If Else
					if(jumpToEnd.FlowDestination.StatementIndex <= branch.FlowConditionalDest.StatementIndex)
					{
						throw new Exception("This is a loop");
					}

					var ifElseFlow = new IfElseFlowData(flow, jumpToEnd.FlowStatement, jumpToEnd.FlowDestination.RawPrevFlow);
					scope.AddSubflow(ifElseFlow);
				}
				else
				{
					//If
					var ifFlow = new IfFlowData(flow, branch.FlowConditionalDest.RawPrevFlow);
					scope.AddSubflow(ifFlow);
				}
				return true;
			}

			if(flow is IFlowControl)
			{
				if(flow.FlowType != FlowStatementType.Unknown)
				{
					return true;
				}
				throw new Exception();
			}

			scope.AddSubflow(new SimpleFlowData(flow));
			return true;
		}

		public static void UpdateScopePre(FlowStatement statement, ref BlockFlowData scope)
		{
			foreach(var preflow in statement.PreFlows)
			{
				if(preflow.Data is BlockFlowData blockPreflowData && blockPreflowData.IsStart(statement, preflow.Type))
				{
					if(scope == blockPreflowData.Scope)
					{
						scope = blockPreflowData;
					}
				}
			}
			if(statement.FlowData is BlockFlowData blockData && blockData.IsStart(statement, statement.FlowType))
			{
				if(scope != blockData.Scope){throw new Exception();}
				
				scope = blockData;
			}
		}
		public static void UpdateScopePost(FlowStatement statement, ref BlockFlowData scope)
		{
			if(statement.FlowData is BlockFlowData blockData && blockData.IsEnd(statement, statement.FlowType))
			{
				if(scope != blockData){throw new Exception();}

				scope = blockData.Scope;
			}
			foreach(var postflow in statement.PostFlows)
			{
				if(postflow.Data is BlockFlowData blockPostflowData && blockPostflowData.IsEnd(statement, postflow.Type))
				{
					if(scope == blockPostflowData)
					{
						scope = blockPostflowData.Scope;
					}
				}
			}
		}

		public virtual void Write(Writer writer, FlowStatement statement, FlowStatementType statementType)
		{
			//Always check type in cases where the statement can be both Start and End or other points
			if(statement == Start && statementType == StartType)
			{
				WriteStart(writer, statement, statementType);
			}
			else
			{
				throw new Exception();
			}
		}
		public abstract void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType);
	}

	public abstract class BlockFlowData:FlowData
	{
		public abstract FlowStatementType EndType{get;}
		public virtual FlowStatement End
		{
			get;
			init
			{
				if(LabelEnd)
				{
					value.SetPostFlow(EndType, this);
				}
				else
				{
					value.SetFlow(EndType, this);
				}
				field = value;
			}
		}

		public BlockFlowData(FlowStatement start):base(start)
		{

		}

		//Indicates that the end is based on a label and is not associated with the specifics of the instruction.
		public abstract bool LabelEnd{get;}

		private readonly List<FlowData> subflows = new List<FlowData>();
		public IReadOnlyList<FlowData> Subflows => subflows;

		public bool IsEnd(FlowStatement statement, FlowStatementType statementType)
		{
			return statement == End && statementType == EndType;
		}

		public void AddSubflow(FlowData flow)
		{
			if(flow.Scope != null)
			{
				throw new Exception("Scope already set");
			}
			flow.Scope = this;
			subflows.Add(flow);
		}

		public override void Write(Writer writer, FlowStatement statement, FlowStatementType statementType)
		{
			//Always check type in cases where the statement can be both Start and End or other points
			if(statement == End && statementType == EndType)
			{
				WriteEnd(writer, statement, statementType);
			}
			else
			{
				base.Write(writer, statement, statementType);
			}
		}
		public abstract void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType);
	}

	public sealed class IfFlowData:BlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.If;
		public override FlowStatementType EndType => FlowStatementType.EndIf;

		public override bool LabelStart => false;
		public override bool LabelEnd => true;

		public FlowStatement If => Start;

		public IfFlowData(FlowStatement start, FlowStatement end):base(start:start)
		{
			End = end;
		}

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			if(start is not IFlowBranch startBranch){throw new Exception();}
			(bool invert, IStackProducer condition) = startBranch.FlowStatement.StackStatement switch
			{
				BeqInstruction.BeqStack beq => (false, beq.Instruction.StackOperation.Condition),
				BeqImmInstruction.BeqImmStack beqImm => (false, beqImm.Instruction.StackOperation.Condition),
				BneInstruction.BneStack bne => (true, bne.Instruction.StackOperation.Condition),
				BneImmInstruction.BneImmStack bneImm => (true, bneImm.Instruction.StackOperation.Condition),
				_ => throw new Exception()
			};

			writer.Write("if ");
			//TODO: Use ToConditionStr(invert)
			if(invert)
			{
				writer.Write("not ");
			}
			writer.Write(condition.ToExpressionString());
			writer.WriteLine(" then");
			writer.OpenLine();
		}
		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			writer.CloseLine("endif");
		}
	}

	public sealed class IfElseFlowData:BlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.If;
		public override FlowStatementType EndType => FlowStatementType.EndIf;

		public override bool LabelStart => false;
		public override bool LabelEnd => true;

		public FlowStatement If => Start;
		/// <summary>The Else is considered the goto statement at the end of the first block</summary>
		public FlowStatement Else{get;}

		public IfElseFlowData(FlowStatement start, FlowStatement elseFlow, FlowStatement end):base(start:start)
		{
			End = end;
			if(Else != null)
			{
				throw new Exception();
			}
			elseFlow.SetFlow(FlowStatementType.Else, this);
			Else = elseFlow;
		}

		public override void Write(Writer writer, FlowStatement statement, FlowStatementType statementType)
		{
			//Always check type in cases where the statement can be both Start and End or other points
			if(statement == Else && statementType == FlowStatementType.Else)
			{
				WriteElse(writer, statement, statementType);
			}
			else
			{
				base.Write(writer, statement, statementType);
			}
		}

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			if(start is not IFlowBranch startBranch){throw new Exception();}
			(bool invert, IStackProducer condition) = startBranch.FlowStatement.StackStatement switch
			{
				BeqInstruction.BeqStack beq => (false, beq.Instruction.StackOperation.Condition),
				BeqImmInstruction.BeqImmStack beqImm => (false, beqImm.Instruction.StackOperation.Condition),
				BneInstruction.BneStack bne => (true, bne.Instruction.StackOperation.Condition),
				BneImmInstruction.BneImmStack bneImm => (true, bneImm.Instruction.StackOperation.Condition),
				_ => throw new Exception()
			};
			writer.Write("if ");
			//TODO: Use ToConditionStr(invert)
			if(invert)
			{
				writer.Write("not ");
			}
			writer.Write(condition.ToExpressionString());
			writer.WriteLine(" then");
			writer.OpenLine();
		}
		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			writer.CloseLine("endif");
		}
		public void WriteElse(Writer writer, FlowStatement elseStatement, FlowStatementType statementType)
		{
			if(elseStatement is not IFlowJump){throw new Exception();}

			writer.CloseLine("else");
			writer.OpenLine();
		}
	}

	public abstract class BreakableBlockFlowData:BlockFlowData
	{
		public abstract FlowStatementType BreakType{get;}

		private readonly List<FlowStatement> breaks = new();
		public IReadOnlyList<FlowStatement> Breaks => breaks;

		public BreakableBlockFlowData(FlowStatement start, FlowStatement end):base(start:start)
		{
			End = end;
		}

		public void AddBreak(FlowStatement breakStatement)
		{
			breakStatement.SetFlowBreak(BreakType, this);
			breaks.Add(breakStatement);
		}

		public override void Write(Writer writer, FlowStatement statement, FlowStatementType statementType)
		{
			foreach(var breakFlow in Breaks)
			{
				if(breakFlow == statement && statementType == BreakType)
				{
					WriteBreak(writer, statement, statementType);
					return;
				}
			}
			base.Write(writer, statement, statementType);
		}

		public abstract void WriteBreak(Writer writer, FlowStatement breakStatement, FlowStatementType statementType);
	}

	public sealed class WhileFlowData(FlowStatement start, FlowStatement end):BreakableBlockFlowData(start, end)
	{
		public override FlowStatementType StartType => FlowStatementType.While;
		public override FlowStatementType EndType => FlowStatementType.EndWhile;
		public override FlowStatementType BreakType => FlowStatementType.WhileBreak;

		public override bool LabelStart => false;
		public override bool LabelEnd => false;

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			if(start is not IFlowBranch startBranch){throw new Exception();}
			(bool invert, IStackProducer condition) = startBranch.FlowStatement.StackStatement switch
			{
				BeqInstruction.BeqStack beq => (false, beq.Instruction.StackOperation.Condition),
				BeqImmInstruction.BeqImmStack beqImm => (false, beqImm.Instruction.StackOperation.Condition),
				BneInstruction.BneStack bne => (true, bne.Instruction.StackOperation.Condition),
				BneImmInstruction.BneImmStack bneImm => (true, bneImm.Instruction.StackOperation.Condition),
				_ => throw new Exception()
			};
			writer.Write("while ");
			//TODO: Use ToConditionStr(invert)
			if(invert)
			{
				writer.Write("not ");
			}
			writer.WriteLine(condition.ToExpressionString());
			writer.OpenLine();
		}

		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			writer.CloseLine("endwhile");
		}

		public override void WriteBreak(Writer writer, FlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("whilebreak");
		}
	}

	public sealed class LoopFlowData(FlowStatement start, FlowStatement end):BreakableBlockFlowData(start, end)
	{
		public override FlowStatementType StartType => FlowStatementType.Loop;
		public override FlowStatementType EndType => FlowStatementType.EndLoop;
		public override FlowStatementType BreakType => FlowStatementType.BreakLoop;

		public override bool LabelStart => true;
		public override bool LabelEnd => false;

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			writer.OpenLine("loop");
		}

		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			writer.CloseLine("endloop");
		}

		public override void WriteBreak(Writer writer, FlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("breakloop");
		}
	}

	public sealed class RepeatFlowData(FlowStatement start, FlowStatement end):BreakableBlockFlowData(start, end)
	{
		public override FlowStatementType StartType => FlowStatementType.Repeat;
		public override FlowStatementType EndType => FlowStatementType.Until;
		public override FlowStatementType BreakType => FlowStatementType.RepeatBreak;

		public override bool LabelStart => true;
		public override bool LabelEnd => false;

		//public override void Write(Writer writer, IFlowStatement statement, FlowStatementType statementType)
		//{
		//	//Handles special case for Repeat where it branched to itself
		//	if(Start == End && statementType == FlowStatementType.Repeat)
		//	{
		//		//Since Start and End are the same, we need to force WriteStart to be called.
		//		//Otherwise, becasue of inheritance calling order, it checks if the statement is End before checking Start.
		//		WriteStart(writer, statement, statementType);
		//	}
		//	else
		//	{
		//		base.Write(writer, statement, statementType);
		//	}
		//}

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			writer.OpenLine("repeat");
		}

		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			if(end is not IFlowBranch endBranch){throw new Exception();}
			(bool invert, IStackProducer condition) = endBranch.FlowStatement.StackStatement switch
			{
				BeqInstruction.BeqStack beq => (false, beq.Instruction.StackOperation.Condition),
				BeqImmInstruction.BeqImmStack beqImm => (false, beqImm.Instruction.StackOperation.Condition),
				BneInstruction.BneStack bne => (true, bne.Instruction.StackOperation.Condition),
				BneImmInstruction.BneImmStack bneImm => (true, bneImm.Instruction.StackOperation.Condition),
				_ => throw new Exception()
			};
			writer.Unindent();
			writer.Write("until ");
			//TODO: Use ToConditionStr(invert)
			if(invert)
			{
				writer.Write("not ");
			}
			writer.WriteLine(condition.ToExpressionString());
		}

		public override void WriteBreak(Writer writer, FlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("repeatbreak");
		}
	}

	//public sealed class ForFlowData:BlockFlowData
	//{
	//	//TODO: For Next flow
	//	public override FlowStatementType StartType => throw new NotImplementedException();
	//	public override FlowStatementType EndType => throw new NotImplementedException();
	//}

	public class CaseFlowData:BlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.Case;
		public override FlowStatementType EndType => FlowStatementType.EndCase;

		public override bool LabelStart => true;
		public override bool LabelEnd => false;

		public override FlowStatement End
		{
			get;
			init
			{
				if(LabelEnd)
				{
					value.SetPostFlow(EndType, this);
				}
				else
				{
					//TODO: This is a bit of a hacky fix
					if(value.FlowType == FlowStatementType.CaseBreak)
					{
						value.FlowType = EndType;
						value.FlowData = this;
					}
					else
					{
						value.SetFlow(EndType, this);
					}
				}
				field = value;
			}
		}

		public IndexJumpInstruction SwitchStatement{get;}
		//Default case uses -1
		public int CaseIndex{get;}

		public CaseFlowData(FlowStatement start, FlowStatement end, FlowStatement switchStatement, int caseIndex):base(start:start)
		{
			End = end;
			SwitchStatement = ((IndexJumpInstruction.SwitchFlow)switchStatement).Instruction;
			CaseIndex = caseIndex;
		}

		//public override void Write(Writer writer, IFlowStatement statement, FlowStatementType statementType)
		//{
		//	//Handles single line cases (generally the last case in a switch) where the Start and End are the same
		//	base.Write(writer, statement, statementType);
		//}

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			var comparands = SwitchStatement.Cases[CaseIndex].Comparands;
			foreach(var comparand in comparands)
			{
				writer.Write("case ");
				writer.WriteInt(comparand);
				writer.WriteLine();
			}
			writer.OpenLine();
		}

		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			writer.CloseLine("endcase");
		}
	}

	public class LastCaseFlowData(FlowStatement start, FlowStatement end, FlowStatement switchStatement, int caseIndex):CaseFlowData(start, end, switchStatement, caseIndex)
	{
		public override bool LabelEnd => true;
	}

	public sealed class DefaultCaseFlowData(FlowStatement start, FlowStatement end, FlowStatement switchStatement):LastCaseFlowData(start, end, switchStatement, -1)
	{
		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			writer.OpenLine("default");
		}
	}

	public sealed class SwitchFlowData:BreakableBlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.Switch;
		public override FlowStatementType EndType => FlowStatementType.EndSwitch;
		public override FlowStatementType BreakType => FlowStatementType.CaseBreak;

		public override bool LabelStart => false;
		public override bool LabelEnd => true;

		public DefaultCaseFlowData DefaultCase{get;private set;}
		public IReadOnlyList<CaseFlowData> Cases{get;private set;}

		private SwitchFlowData(FlowStatement start, FlowStatement end):base(start, end){}

		public static bool TryIdentifySwitchFlow(FlowStatement flow, BlockFlowData scope, [MaybeNullWhen(false)]out SwitchFlowData switchFlowData)
		{
			if(flow is not IFlowControl flowControl || flowControl.ControlStatement.FlowStatement is not IndexJumpInstruction.SwitchFlow switchStart)
			{
				switchFlowData = null;
				return false;
			}
			
			//The Unknown type check is for ProcBreak
			//Can have a case break to an external switch
			if(switchStart.RawNextFlow is not IFlowJump switchDefaultJump /*|| switchDefaultJump.FlowType != FlowStatementType.Unknown*/)
			{
				throw new Exception("Invalid switch");
			}

			//Get cases

			//Switch should always be followed by a jump. If a default case exists, this will go to it. Otherwise this goes to the end of the main cases.
			SwitchEndJumpFlowData switchEndJumpFlow = new SwitchEndJumpFlowData(switchDefaultJump.FlowStatement);

			FlowStatement caseBreakDest;
			//If there is no default case, then the switch's default jump will go a location that directly leads into it.
			//If there is a jump there, then that is the goto for the prior case indicating we jumped into a default case.
			//TODO: This may not always be true. Consider if the prior case uses a procbreak.
			if(switchDefaultJump.FlowDestination.RawPrevFlow is IFlowJump flowGoto)
			{
				caseBreakDest = flowGoto.FlowDestination;
			}
			else
			{
				caseBreakDest = switchDefaultJump.FlowDestination;
			}
			FlowStatement endSwitch = caseBreakDest.RawPrevFlow;
			
			switchFlowData = new SwitchFlowData(switchStart, endSwitch);

			//Get breaks
			foreach(var breakSource in caseBreakDest.FlowSources)
			{
				//A jump to the break destination that happens within the scope of the while is considered a break.
				//The scope starts right after the default jump
				if(switchDefaultJump.FlowStatement.StatementIndex<breakSource.StatementIndex && breakSource.StatementIndex<=endSwitch.StatementIndex)
				{
					switchFlowData.AddBreak(breakSource);
				}
			}
			scope.AddSubflow(switchFlowData);

			//Cases must be handled after SwitchFlowData is created, otherwise endswitch and endcase statements will be in the incorrect order
			DefaultCaseFlowData defaultCase = null;
			if(caseBreakDest != switchDefaultJump.FlowDestination)
			{
				defaultCase = new DefaultCaseFlowData(switchDefaultJump.FlowDestination, endSwitch, switchStart);
			}

			var cases = new List<CaseFlowData>();
			for(int i=0; i<switchStart.FlowCaseDestinations.Count-1; i++)
			{
				var caseStart = switchStart.FlowCaseDestinations[i];
				var caseEnd = switchStart.FlowCaseDestinations[i+1].RawPrevFlow;
				if(caseEnd is not IFlowJump caseGoto || caseGoto.FlowDestination != caseBreakDest)
				{
					throw new Exception();
				}
				cases.Add(new CaseFlowData(caseStart, caseEnd, switchStart, i));
			}
			var lastCaseStart = switchStart.FlowCaseDestinations[^1];
			if(defaultCase != null)
			{
				var lastCaseEnd = defaultCase.Start.RawPrevFlow;
				cases.Add(new CaseFlowData(lastCaseStart, lastCaseEnd, switchStart, cases.Count));
			}
			else
			{
				var lastCaseEnd = caseBreakDest.RawPrevFlow;
				cases.Add(new LastCaseFlowData(lastCaseStart, lastCaseEnd, switchStart, cases.Count));
			}
			switchFlowData.DefaultCase = defaultCase;
			switchFlowData.Cases = cases;

			switchFlowData.AddSubflow(switchEndJumpFlow);
			foreach(var c in cases)
			{
				switchFlowData.AddSubflow(c);
			}
			if(defaultCase != null)
			{
				switchFlowData.AddSubflow(defaultCase);
			}

			return true;
		}

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			writer.Write("switch ");
			writer.WriteLine(((IndexJumpInstruction.SwitchFlow)start).Instruction.StackOperation.Value.ToExpressionString());
			writer.OpenLine();
		}

		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			writer.CloseLine("endswitch");
		}

		public override void WriteBreak(Writer writer, FlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("casebreak");
		}
	}

	public class SimpleFlowData(FlowStatement statement):FlowData(statement)
	{
		public override FlowStatementType StartType => FlowStatementType.Simple;

		public override bool LabelStart => false;

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			start.StackStatement.WriteStatement(writer);
			writer.WriteLine();
		}
	}

	public sealed class SubroutineFlowData:BreakableBlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.StartSubroutine;
		public override FlowStatementType BreakType => FlowStatementType.BreakSubroutine;
		public override FlowStatementType EndType => FlowStatementType.EndSubroutine;

		public override bool LabelStart => true;
		public override bool LabelEnd => throw new NotImplementedException();

		public override FlowStatement End
		{
			get;
			init
			{
				//If this is a return or endstrat, than the instruction itself is the end, otherwise, it may be something else, like the end of a loop.
				if(value is ReturnInstruction.ReturnFlow or EndStratInstruction.EndStratFlow)
				{
					value.SetFlow(EndType, this);
				}
				else
				{
					value.SetPostFlow(EndType, this);
				}
				field = value;
			}
		}

		private SubroutineFlowData(FlowStatement start, FlowStatement end):base(start, end){}

		public static bool TryIdentifySubroutine(FlowStatement flow, BlockFlowData scope, out SubroutineFlowData subroutineFlowData)
		{
			//If this is not a subroutine or if it has already been identified.
			if(!flow.StackStatement.StatementLabel.IsSubroutineEntry || flow.HasPreflow(FlowStatementType.StartSubroutine))
			{
				subroutineFlowData = null;
				return false;
			}
			
			var subroutineStart = flow;
			var subroutineEnd = flow;
			//Find either the end of the program or the start of the next subroutine. The end for this subroutine should be the instruction before it.
			while(subroutineEnd.RawNextFlow!=null && !subroutineEnd.RawNextFlow.StackStatement.StatementLabel.IsSubroutineEntry)
			{
				subroutineEnd = subroutineEnd.RawNextFlow;
			}
			subroutineFlowData = new SubroutineFlowData(subroutineStart, subroutineEnd);
			if(subroutineEnd is ReturnInstruction.ReturnFlow or EndStratInstruction.EndStratFlow && subroutineEnd.FlowSources.Count>0)
			{
				//var breakList = new List<IFlowStatement>();
				foreach(var source in subroutineEnd.FlowSources)
				{
					//Unconditional Jump to a return is a ProcBreak
					if(source is IFlowJump)
					{
						subroutineFlowData.AddBreak(source);
					}
				}
			}
			
			scope.AddSubflow(subroutineFlowData);
			return true;
		}
		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			writer.Write(start.StackStatement.FirstInstruction.SubroutineType switch
			{
				SubroutineType.Strat => "strat",
				SubroutineType.Trigger => "trigger",
				SubroutineType.Proc => "defproc",
				_ => throw new Exception("Unknown subroutine type")
			});
			writer.Write(' ');
			writer.Write(start.StackStatement.StatementLabel.SubroutineName());
			writer.OpenLine();
		}

		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType)
		{
			writer.CloseLine(Start.StackStatement.FirstInstruction.SubroutineType switch
			{
				SubroutineType.Strat => "endstrat",
				SubroutineType.Trigger => "endtrigger",
				SubroutineType.Proc => "endproc",
				_ => throw new Exception("Unknown subroutine type")
			});
			writer.WriteLine();

			//At this point, there should be no indents
			writer.AssertNoIndent();
		}

		public override void WriteBreak(Writer writer, FlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine(Start.StackStatement.FirstInstruction.SubroutineType switch
			{
				SubroutineType.Strat => throw new Exception("Strat does not support break"),
				SubroutineType.Trigger => "return # This is wrong",//throw new Exception("Trigger does not support break"),
				SubroutineType.Proc => "procbreak",
				_ => throw new Exception("Unknown subroutine type")
			});
		}
	}

	//public class ProcBreakFlowData(IFlowStatement flow):SimpleFlowData(flow)
	//{
	//	public override FlowStatementType StartType => FlowStatementType.ProcBreak;

	//	public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
	//	{
	//		writer.WriteLine("procbreak");
	//	}
	//}

	public sealed class SwitchEndJumpFlowData(FlowStatement flow):SimpleFlowData(flow)
	{
		public override FlowStatementType StartType => FlowStatementType.SwitchEndJump;

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType)
		{
			//Do nothing, this should be invisible
		}
	}

	public sealed class ProgramFlowData:BlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.ProgramStart;
		public override FlowStatementType EndType => FlowStatementType.ProgramEnd;

		public override bool LabelStart => true;
		public override bool LabelEnd => true;

		public ProgramFlowData(FlowStatement start, FlowStatement end):base(start:start)
		{
			End = end;
		}

		public override void WriteStart(Writer writer, FlowStatement start, FlowStatementType statementType){}
		public override void WriteEnd(Writer writer, FlowStatement end, FlowStatementType statementType){}
	}

	public abstract class FlowStatement
	{
		public FlowStatementType FlowType{get;set;}
		public FlowData FlowData{get;set;}

		public readonly Queue<(FlowStatementType Type, FlowData Data)> PreFlows = new();
		public readonly Stack<(FlowStatementType Type, FlowData Data)> PostFlows = new();

		public bool HasPreflow(FlowStatementType type) => PreFlows.Any(p => p.Type == type);
		public bool HasPostflow(FlowStatementType type) => PostFlows.Any(p => p.Type == type);

		public void SetPreFlow(FlowStatementType type, FlowData data)
		{
			PreFlows.Enqueue((type, data));
		}

		public void SetFlow(FlowStatementType type, FlowData data)
		{
			if(FlowType != FlowStatementType.Unknown)
			{
				//TODO: This shouldn't be hit
				throw new Exception("Flow already specified");
				//return;
			}
			FlowType = type;
			FlowData = data;
		}

		public void SetFlowBreak(FlowStatementType type, FlowData data)
		{
			//If flow is already set then it means it should be a break to the outermost flow (which ever started first)
			if(FlowType != FlowStatementType.Unknown && data.Start.StatementIndex <= FlowData.Start.StatementIndex)
			{
				return;
			}
			FlowType = type;
			FlowData = data;
		}

		public void SetPostFlow(FlowStatementType type, FlowData data)
		{
			PostFlows.Push((type, data));
		}

		public int StatementIndex => StackStatement.OperationInstruction.Index;
		public abstract IStackStatement StackStatement{get;}

		public FlowStatement NextFlow => StackStatement.NextStatement.FlowStatement;
		public FlowStatement PrevFlow => StackStatement.PrevStatement.FlowStatement;

		//Ignores terminal statements and just get the next/prev flow based on the instruction order.
		//Returns null if at the end
		public FlowStatement? RawNextFlow => StackStatement.OperationInstruction.RawAsmNext?.StackOperation!.Statement?.FlowStatement;
		public FlowStatement? RawPrevFlow => (StackStatement.FirstInstruction.RawAsmPrev?.StackOperation as IStackStatement)?.FlowStatement;

		//Jump sources
		public readonly List<FlowStatement> FlowSources = new();
		public readonly List<FlowStatement> FlowDestinations = new();
	}
	public abstract class FlowStatement<TInstruction>:FlowStatement where TInstruction:Instruction
	{
		public /*required*/ TInstruction Instruction{get;init;}
	}

	/// <summary>A flow with no flow change.</summary>
	//public interface IFlowSimple:IFlowStatement;

	/// <summary>A flow with no next flow.</summary>
	public interface IFlowTerminal;

	public interface IFlowControl
	{
		public abstract IStackStatement ControlStatement{get;}

		public abstract void Analyze(FlowAnalyzer flow);
	}
	public interface IFlowBranch
	{
		public abstract FlowStatement FlowStatement{get;}
		public abstract FlowStatement FlowConditionalDest{get;}
	}

	public interface IFlowJump
	{
		public abstract FlowStatement FlowStatement{get;}
		public abstract FlowStatement FlowDestination{get;}
	}

	public sealed class FlowAnalyzer
	{
		public readonly List<FlowStatement> Statements = new();

		//private readonly Dictionary<string,IFlowStatement> statementLabels = new Dictionary<string,IFlowStatement>();

		private readonly Queue<(FlowStatement instr,IStackStatement dest)> destinationsToAdd = new();

		private readonly HashSet<FlowStatement> analyzed = new();

		public void AddDest(FlowStatement instr, Instruction dest)
		{
			var destStatement = dest.StackOperation!.Statement;
			destinationsToAdd.Enqueue((instr, destStatement));

			var destFlowStatement = destStatement.FlowStatement;
			instr.FlowDestinations.Add(destFlowStatement);
			destFlowStatement.FlowSources.Add(instr);
		}

		private void AnalyzeBlock(List<FlowStatement> outputStatements, IStackStatement dest)
		{
			//var currentStatements = new List<IStackStatement>();
			var cur = dest;

			//IFlowStatement prevStatement = null;

			while(true)
			{
				var flowStatement = cur.FlowStatement;

				//Connect with an existing statement
				if(analyzed.Contains(flowStatement))
				{
					//if(prevStatement != null)
					//{
					//	prevStatement.NextFlow = flowStatement;
					//	flowStatement.PrevFlow = prevStatement;
					//}
					break;
				}

				if(flowStatement is IFlowControl flowControl)
				{
					flowControl.Analyze(this);
				}
				analyzed.Add(flowStatement);

				//bool nextIsLabelled = cur.NextStatement != null && cur.NextStatement.TryGetLabel(out _);
				
				outputStatements.Add(flowStatement);

				//if(flowStatement is IFlowControl or IFlowTerminal /*|| nextIsLabelled*/)
				//{
				//	//var label = flowStatement.Statements[0].StatementLabel;

				//	////Statements following a branch and at the start of a subroutines can both not have labels
				//	//if(label != null)
				//	//{
				//	//	statementLabels.Add(label, flowStatement);
				//	//}

				//	//if(prevStatement != null)
				//	//{
				//	//	prevStatement.NextFlow = flowStatement;
				//	//	flowStatement.PrevFlow = prevStatement;
				//	//}
				//	//prevStatement = flowStatement;
				//}
				if(flowStatement is IFlowTerminal)
				{
					break;
				}

				cur = cur.NextStatement;
			}
		}

		public void Analyze(IReadOnlyList<IStackStatement> subroutines)
		{
			foreach(var subroutine in subroutines)
			{
				Setup(subroutine);
			}
			Analyze();
		}

		public void Setup(IStackStatement startingStackStatement)
		{
			var statements = new List<FlowStatement>();

			AnalyzeBlock(statements, startingStackStatement);

			while(destinationsToAdd.TryDequeue(out var dest))
			{
				//var labelName = dest.dest.StatementLabel;
				if(!analyzed.Contains(dest.dest.FlowStatement))
				{
					AnalyzeBlock(statements, dest.dest);
				}

				//dest.instr.FlowDestinations.Add(statementLabels[labelName]);
			}

			Statements.AddRange(statements);
			Statements.Sort((a,b) => a.StatementIndex.CompareTo(b.StatementIndex));
		}

		public void Analyze()
		{
			var programFlowData = new ProgramFlowData(Statements[0], Statements[^1]);
			BlockFlowData? scope = null;
			//Setup initial scope around programFlowData.
			//foreach(var statement in Statements)
			for(int i = 0; i<Statements.Count; i++)
			{
				var statement = Statements[i];

				//TODO: For scope, nesting loops won't be an issue. Loops inside of a final switch case or at the end of a subroutine can have overlap.
				//We should be defaulting to the innermost loop (though it should work either way).
				//Otherwise, pay attention to the jump destination to determine the proper scope break.
				do
				{
					FlowData.UpdateScopePre(statement, ref scope);
				}
				while(FlowData.TryIdentifyFlows(statement, scope));

				FlowData.UpdateScopePost(statement, ref scope);
			}
		}

		public void Write(Writer writer)
		{
			//SubroutineType subroutineType = SubroutineType.None;
			for(int i=0; i<Statements.Count; i++)
			{
				var statement = Statements[i];

				//if(statement.StackStatement.TryGetSubroutine(out var subroutine))
				//{
				//	subroutineType = statement.StackStatement.FirstInstruction.SubroutineType;
				//	var defKeyword = subroutineType switch
				//	{
				//		SubroutineType.Strat => "strat",
				//		SubroutineType.Trigger => "trigger",
				//		SubroutineType.Proc => "defproc",
				//		_ => throw new Exception("Unknown subroutine type")
				//	};
				//	writer.WriteLine($"{defKeyword} {subroutine.SubroutineName()}");
				//}

				//if(statement.StackStatement.TryGetLabel(out var label))
				//{
				//	writer.WriteLine(label + ":");
				//}
				
				//if(statement.FlowType == FlowStatementType.EndSubroutine)
				//{
				//	var endKeyword = subroutineType switch
				//	{
				//		SubroutineType.Strat => "endstrat",
				//		SubroutineType.Trigger => "endtrigger",
				//		SubroutineType.Proc => "endproc",
				//		_ => throw new Exception("Unknown subroutine type")
				//	};
				//	writer.WriteLine(endKeyword);
				//	writer.WriteLine("");

				//	//At this point, there should be no indents
				//	writer.AssertNoIndent();
				//}
				//else
				
				foreach(var preflow in statement.PreFlows)
				{
					preflow.Data.Write(writer, statement, preflow.Type);
				}
				statement.FlowData.Write(writer, statement, statement.FlowType);
				foreach(var postflow in statement.PostFlows)
				{
					postflow.Data.Write(writer, statement, postflow.Type);
				}
			}
		}
	}
}