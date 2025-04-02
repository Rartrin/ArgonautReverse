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
		private readonly IFlowStatement _start;
		public IFlowStatement Start
		{
			get => _start;
			init
			{
				if(LabelStart)
				{
					value.SetPreFlow(StartType, this);
				}
				else
				{
					value.SetFlow(StartType, this);
				}
				_start = value;
			}
		}

		//Indicates that the start is based on a label and is not associated with the specifics of the instruction.
		public abstract bool LabelStart{get;}

		public bool IsStart(IFlowStatement statement, FlowStatementType statementType)
		{
			return statement == Start && statementType == StartType;
		}

		//Returns true when new things have been identified. Returns false when there is nothing left to identify.
		public static bool TryIdentifyFlows(IFlowStatement flow, BlockFlowData scope)
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
			if(flow is IFlowGoto jump && flow.StatementIndex<jump.FlowDestination.StatementIndex)
			{

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

				bool conditionalStart = flow.ControlStatement is IFlowBranch conditionalStartBranch && conditionalStartBranch.FlowConditionalDest == backwardFlow.RawNextFlow;
				bool conditionalEnd = backwardFlow.ControlStatement is IFlowBranch;

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
				//TODO: Better way to handle breaks. We should check if the jump happeneds within the current scope.
				if(branch.FlowConditionalDest.RawPrevFlow is IFlowGoto jumpToEnd && jumpToEnd.FlowType == FlowStatementType.Unknown)
				{
					//If Else
					if(jumpToEnd.FlowDestination.StatementIndex <= branch.FlowConditionalDest.StatementIndex)
					{
						throw new Exception("This is a loop");
					}

					var ifElseFlow = new IfElseFlowData(flow, jumpToEnd, jumpToEnd.FlowDestination.RawPrevFlow);
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


		public static void UpdateScopePre(IFlowStatement statement, ref BlockFlowData scope)
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
		public static void UpdateScopePost(IFlowStatement statement, ref BlockFlowData scope)
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

		public virtual void Write(Writer writer, IFlowStatement statement, FlowStatementType statementType)
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
		public abstract void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType);
	}

	public abstract class BlockFlowData:FlowData
	{
		public abstract FlowStatementType EndType{get;}
		protected IFlowStatement _end;
		public virtual IFlowStatement End
		{
			get => _end;
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
				_end = value;
			}
		}
		//Indicates that the end is based on a label and is not associated with the specifics of the instruction.
		public abstract bool LabelEnd{get;}

		private readonly List<FlowData> subflows = new List<FlowData>();
		public IReadOnlyList<FlowData> Subflows => subflows;

		public bool IsEnd(IFlowStatement statement, FlowStatementType statementType)
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

		public override void Write(Writer writer, IFlowStatement statement, FlowStatementType statementType)
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
		public abstract void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType);
	}

	public class IfFlowData:BlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.If;
		public override FlowStatementType EndType => FlowStatementType.EndIf;

		public override bool LabelStart => false;
		public override bool LabelEnd => true;

		public IFlowStatement If => Start;

		public IfFlowData(IFlowStatement start, IFlowStatement end)
		{
			Start = start;
			End = end;
		}

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			bool invert = start switch
			{
				BeqInstruction or BeqImmInstruction => false,
				BneInstruction or BneImmInstruction => true,
				_ => throw new Exception()
			};
			var branch = (BranchInstruction)start;
			var condition = branch.Condition.ToExpressionString();
			if(invert)
			{
				condition = "not " + condition;
			}

			writer.WriteLine($"if {condition} then");
			writer.Indent();
		}
		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			writer.Unindent();
			writer.WriteLine("endif");
		}
	}

	public class IfElseFlowData:BlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.If;
		public override FlowStatementType EndType => FlowStatementType.EndIf;

		public override bool LabelStart => false;
		public override bool LabelEnd => true;

		public IFlowStatement If => Start;
		/// <summary>The Else is considered the goto statement at the end of the first block</summary>
		public IFlowStatement Else{get;}

		public IfElseFlowData(IFlowStatement start, IFlowStatement elseFlow, IFlowStatement end)
		{
			Start = start;
			End = end;
			if(Else != null)
			{
				throw new Exception();
			}
			elseFlow.SetFlow(FlowStatementType.Else, this);
			Else = elseFlow;
		}

		public override void Write(Writer writer, IFlowStatement statement, FlowStatementType statementType)
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

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			bool invert = start switch
			{
				BeqInstruction or BeqImmInstruction => false,
				BneInstruction or BneImmInstruction => true,
				_ => throw new Exception()
			};
			var branch = (BranchInstruction)start;
			var condition = branch.Condition.ToExpressionString();
			if(invert)
			{
				condition = "not " + condition;
			}

			writer.WriteLine($"if {condition} then");
			writer.Indent();
		}
		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			writer.Unindent();
			writer.WriteLine("endif");
		}
		public void WriteElse(Writer writer, IFlowStatement elseStatement, FlowStatementType statementType)
		{
			if(elseStatement is not IFlowGoto){throw new Exception();}

			writer.Unindent();
			writer.WriteLine("else");
			writer.Indent();
		}
	}

	public abstract class BreakableBlockFlowData:BlockFlowData
	{
		public abstract FlowStatementType BreakType{get;}

		private readonly List<IFlowStatement> breaks = new List<IFlowStatement>();
		public IReadOnlyList<IFlowStatement> Breaks => breaks;

		public BreakableBlockFlowData(IFlowStatement start, IFlowStatement end)
		{
			Start = start;
			End = end;
		}

		public void AddBreak(IFlowStatement breakStatement)
		{
			breakStatement.SetFlowBreak(BreakType, this);
			breaks.Add(breakStatement);
		}

		public override void Write(Writer writer, IFlowStatement statement, FlowStatementType statementType)
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

		public abstract void WriteBreak(Writer writer, IFlowStatement breakStatement, FlowStatementType statementType);
	}

	public sealed class WhileFlowData(IFlowStatement start, IFlowStatement end):BreakableBlockFlowData(start, end)
	{
		public override FlowStatementType StartType => FlowStatementType.While;
		public override FlowStatementType EndType => FlowStatementType.EndWhile;
		public override FlowStatementType BreakType => FlowStatementType.WhileBreak;

		public override bool LabelStart => false;
		public override bool LabelEnd => false;

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			bool invert = start switch
			{
				BeqInstruction or BeqImmInstruction => false,
				BneInstruction or BneImmInstruction => true,
				_ => throw new Exception()
			};
			var branch = (BranchInstruction)start;
			var condition = branch.Condition.ToExpressionString();
			if(invert)
			{
				condition = "not " + condition;
			}
			writer.WriteLine($"while {condition}");
			writer.Indent();
		}

		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			writer.Unindent();
			writer.WriteLine("endwhile");
		}

		public override void WriteBreak(Writer writer, IFlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("whilebreak");
		}
	}

	public sealed class LoopFlowData(IFlowStatement start, IFlowStatement end):BreakableBlockFlowData(start, end)
	{
		public override FlowStatementType StartType => FlowStatementType.Loop;
		public override FlowStatementType EndType => FlowStatementType.EndLoop;
		public override FlowStatementType BreakType => FlowStatementType.BreakLoop;

		public override bool LabelStart => true;
		public override bool LabelEnd => false;

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			writer.WriteLine("loop");
			writer.Indent();
		}

		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			writer.Unindent();
			writer.WriteLine("endloop");
		}

		public override void WriteBreak(Writer writer, IFlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("breakloop");
		}
	}

	public sealed class RepeatFlowData(IFlowStatement start, IFlowStatement end):BreakableBlockFlowData(start, end)
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

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			writer.WriteLine("repeat");
			writer.Indent();
		}

		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			bool invert = end switch
			{
				BeqInstruction or BeqImmInstruction => false,
				BneInstruction or BneImmInstruction => true,
				_ => throw new Exception()
			};
			var branch = (BranchInstruction)end;
			var condition = branch.Condition.ToExpressionString();
			if(invert)
			{
				condition = "not " + condition;
			}

			writer.Unindent();
			writer.WriteLine($"until {condition}");
		}

		public override void WriteBreak(Writer writer, IFlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("repeatbreak");
		}
	}

	//These were deprecated in the Aladdin ASL
	//public sealed class ForFlorData:BlockFlowData
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

		public override IFlowStatement End
		{
			get => _end;
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
				_end = value;
			}
		}

		public Index_JumpInstruction SwitchStatement{get;}
		//Default case uses -1
		public int CaseIndex{get;}

		public CaseFlowData(IFlowStatement start, IFlowStatement end, IFlowStatement switchStatement, int caseIndex)
		{
			Start = start;
			End = end;
			SwitchStatement = (Index_JumpInstruction)switchStatement;
			CaseIndex = caseIndex;
		}

		//public override void Write(Writer writer, IFlowStatement statement, FlowStatementType statementType)
		//{
		//	//Handles single line cases (generally the last case in a switch) where the Start and End are the same
		//	base.Write(writer, statement, statementType);
		//}

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			var comparands = SwitchStatement.Cases[CaseIndex].Comparands;
			foreach(var comparand in comparands)
			{
				writer.WriteLine($"case {comparand}");
			}
			writer.Indent();
		}

		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			writer.Unindent();
			writer.WriteLine("endcase");
		}
	}

	public class LastCaseFlowData:CaseFlowData
	{
		public override bool LabelEnd => true;

		public LastCaseFlowData(IFlowStatement start, IFlowStatement end, IFlowStatement switchStatement, int caseIndex) : base(start, end, switchStatement, caseIndex){}
	}

	public sealed class DefaultCaseFlowData:LastCaseFlowData
	{
		public DefaultCaseFlowData(IFlowStatement start, IFlowStatement end, IFlowStatement switchStatement) : base(start, end, switchStatement, -1){}

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			writer.WriteLine("default");
			writer.Indent();
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

		private SwitchFlowData(IFlowStatement start, IFlowStatement end):base(start, end){}

		public static bool TryIdentifySwitchFlow(IFlowStatement flow, BlockFlowData scope, out SwitchFlowData switchFlowData)
		{
			if(flow.ControlStatement is not IFlowSwitch switchStart)
			{
				switchFlowData = null;
				return false;
			}
			
			//The Unknown type check is for ProcBreak
			//Can have a case break to an external switch
			if(switchStart.RawNextFlow is not IFlowGoto switchDefaultJump /*|| switchDefaultJump.FlowType != FlowStatementType.Unknown*/)
			{
				throw new Exception("Invalid switch");
			}

			//Get cases

			//Switch should always be followed by a jump. If a default case exists, this will go to it. Otherwise this goes to the end of the main cases.
			SwitchEndJumpFlowData switchEndJumpFlow = new SwitchEndJumpFlowData(switchDefaultJump);

			//If there is no default case, then the switch's default jump will go a location that directly leads into it.
			//If there is a jump there, then that is the goto for the prior case indicating we jumped into a default case.
			//TODO: This may not always be true. Consider if the prior case uses a procbreak.
			bool hasDefault = switchDefaultJump.FlowDestination.RawPrevFlow is IFlowGoto;
			IFlowStatement caseBreakDest = hasDefault ? ((IFlowGoto)switchDefaultJump.FlowDestination.RawPrevFlow).FlowDestination : switchDefaultJump.FlowDestination;
			IFlowStatement endSwitch = caseBreakDest.RawPrevFlow;
			
			switchFlowData = new SwitchFlowData(switchStart, endSwitch);

			//Get breaks
			foreach(var breakSource in caseBreakDest.FlowSources)
			{
				//A jump to the break destination that happens within the scope of the while is considered a break.
				//The scope starts right after the default jump
				if(switchDefaultJump.StatementIndex<breakSource.StatementIndex && breakSource.StatementIndex<=endSwitch.StatementIndex)
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
				if(caseEnd is not IFlowGoto caseGoto || caseGoto.FlowDestination != caseBreakDest)
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

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			writer.WriteLine($"switch {((Index_JumpInstruction)start).Value.ToExpressionString()}");
			writer.Indent();
		}

		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			writer.Unindent();
			writer.WriteLine("endswitch");
		}

		public override void WriteBreak(Writer writer, IFlowStatement breakStatement, FlowStatementType statementType)
		{
			writer.WriteLine("casebreak");
		}
	}

	public class SimpleFlowData:FlowData
	{
		public override FlowStatementType StartType => FlowStatementType.Simple;

		public override bool LabelStart => false;

		public SimpleFlowData(IFlowStatement statement)
		{
			Start = statement;
		}

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			writer.WriteLine(((IStackStatement)start).ToStatement());
		}
	}

	public sealed class SubroutineFlowData:BreakableBlockFlowData
	{
		public override FlowStatementType StartType => FlowStatementType.StartSubroutine;
		public override FlowStatementType BreakType => FlowStatementType.BreakSubroutine;
		public override FlowStatementType EndType => FlowStatementType.EndSubroutine;

		public override bool LabelStart => true;
		public override bool LabelEnd => throw new NotImplementedException();

		public override IFlowStatement End
		{
			get => _end;
			init
			{
				//If this is a return or endstrat, than the instruction itself is the end, otherwise, it may be something else, like the end of a loop.
				if(value is ReturnInstruction or EndStratInstruction)
				{
					value.SetFlow(EndType, this);
				}
				else
				{
					value.SetPostFlow(EndType, this);
				}
				_end = value;
			}
		}

		private SubroutineFlowData(IFlowStatement start, IFlowStatement end):base(start, end){}

		public static bool TryIdentifySubroutine(IFlowStatement flow, BlockFlowData scope, out SubroutineFlowData subroutineFlowData)
		{
			//If this is not a subroutine or if it has already been identified.
			if(flow.StackStatement.StatementSubroutineName == null || flow.HasPreflow(FlowStatementType.StartSubroutine))
			{
				subroutineFlowData = null;
				return false;
			}
			
			var subroutineStart = flow;
			var subroutineEnd = flow;
			//Find either the end of the program or the start of the next subroutine. The end for this subroutine should be the instruction before it.
			while(subroutineEnd.RawNextFlow!=null && subroutineEnd.RawNextFlow.StackStatement.StatementSubroutineName == null)
			{
				subroutineEnd = subroutineEnd.RawNextFlow;
			}
			subroutineFlowData = new SubroutineFlowData(subroutineStart, subroutineEnd);
			if(subroutineEnd is ReturnInstruction or EndStratInstruction && subroutineEnd.FlowSources.Count>0)
			{
				//var breakList = new List<IFlowStatement>();
				foreach(var source in subroutineEnd.FlowSources)
				{
					//Unconditional Jump to a return is a ProcBreak
					if(source is IFlowGoto)
					{
						subroutineFlowData.AddBreak(source);
					}
				}
					
			}
			
			scope.AddSubflow(subroutineFlowData);
			return true;
		}
		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
		{
			var subroutineName = start.StackStatement.StatementSubroutineName;
			var subroutineType = start.StackStatement.FirstInstruction.SubroutineType;
			var defKeyword = subroutineType switch
			{
				SubroutineType.Strat => "strat",
				SubroutineType.Trigger => "trigger",
				SubroutineType.Proc => "defproc",
				_ => throw new Exception("Unknown subroutine type")
			};
			writer.WriteLine($"{defKeyword} {subroutineName}");
			writer.Indent();
		}

		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType)
		{
			var subroutineType = Start.StackStatement.FirstInstruction.SubroutineType;
			var endKeyword = subroutineType switch
			{
				SubroutineType.Strat => "endstrat",
				SubroutineType.Trigger => "endtrigger",
				SubroutineType.Proc => "endproc",
				_ => throw new Exception("Unknown subroutine type")
			};
			writer.Unindent();
			writer.WriteLine(endKeyword);
			writer.WriteLine("");

			//At this point, there should be no indents
			writer.AssertNoIndent();
		}

		public override void WriteBreak(Writer writer, IFlowStatement breakStatement, FlowStatementType statementType)
		{
			var subroutineType = Start.StackStatement.FirstInstruction.SubroutineType;
			var breakKeyword = subroutineType switch
			{
				SubroutineType.Strat => throw new NotSupportedException("Strat does not support break"),
				SubroutineType.Trigger => "return # This is wrong",//throw new NotSupportedException("Trigger does not support break"),
				SubroutineType.Proc => "procbreak",
				_ => throw new Exception("Unknown subroutine type")
			};
			writer.WriteLine(breakKeyword);
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

	public class SwitchEndJumpFlowData(IFlowStatement flow):SimpleFlowData(flow)
	{
		public override FlowStatementType StartType => FlowStatementType.SwitchEndJump;

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType)
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

		public ProgramFlowData(IFlowStatement start, IFlowStatement end)
		{
			Start = start;
			End = end;
		}

		public override void WriteStart(Writer writer, IFlowStatement start, FlowStatementType statementType){}
		public override void WriteEnd(Writer writer, IFlowStatement end, FlowStatementType statementType){}
	}

	public interface IFlowStatement
	{
		public FlowStatementType FlowType{get;set;}
		public FlowData FlowData{get;set;}

		public Queue<(FlowStatementType Type, FlowData Data)> PreFlows{get;}
		public Stack<(FlowStatementType Type, FlowData Data)> PostFlows{get;}

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

		public abstract IStackStatement ControlStatement{get;}

		public int StatementIndex => StackStatement.OperationInstruction.Index;
		public abstract IStackStatement StackStatement{get;}

		public IFlowStatement NextFlow => StackStatement.NextStatement.FlowStatement;
		public IFlowStatement PrevFlow => StackStatement.PrevStatement.FlowStatement;

		//Ignores terminal statements and just get the next/prev flow based on the instruction order.
		//Returns null if at the end
		public IFlowStatement? RawNextFlow => (IFlowStatement?)((IStackOperation)StackStatement.OperationInstruction.RawAsmNext)?.Statement;
		public IFlowStatement? RawPrevFlow => (IFlowStatement?)StackStatement.FirstInstruction.RawAsmPrev;

		//Jump sources
		public abstract IList<IFlowStatement> FlowSources{get;}

		public abstract IList<IFlowStatement> FlowDestinations{get;}

		public abstract void Analyze(FlowAnalyzer flow);
	}

	/// <summary>A flow with no flow change.</summary>
	public interface IFlowSimple:IFlowStatement;

	/// <summary>A flow with no next flow.</summary>
	public interface IFlowTerminal:IFlowStatement;

	public interface IFlowControl:IFlowStatement;

	public interface IFlowGoto:IFlowTerminal,IFlowControl
	{
		public IFlowStatement FlowDestination => FlowDestinations[0];
	}

	public interface IFlowBranch:IFlowStatement,IFlowControl
	{
		public IFlowStatement FlowConditionalDest => FlowDestinations[0];
	}

	public interface IFlowSwitch:IFlowStatement,IFlowControl
	{
		public IReadOnlyList<IFlowStatement> FlowCaseDestinations => (IReadOnlyList<IFlowStatement>)FlowDestinations;
	}

	public sealed class FlowAnalyzer
	{
		public readonly List<IFlowStatement> Statements = new List<IFlowStatement>();

		//private readonly Dictionary<string,IFlowStatement> statementLabels = new Dictionary<string,IFlowStatement>();

		private readonly Queue<(IFlowStatement instr,IStackStatement dest)> destinationsToAdd = new Queue<(IFlowStatement instr,IStackStatement dest)>();

		private readonly HashSet<IFlowStatement> analyzed = new HashSet<IFlowStatement>();

		public void AddDest(IFlowStatement instr, Instruction dest)
		{
			var destStatement = ((IStackOperation)dest).Statement;
			destinationsToAdd.Enqueue((instr, destStatement));

			instr.FlowDestinations.Add((IFlowStatement)destStatement);
			((IFlowStatement)destStatement).FlowSources.Add(instr);
		}

		private void AnalyzeBlock(List<IFlowStatement> outputStatements, IStackStatement dest)
		{
			//var currentStatements = new List<IStackStatement>();
			var cur = dest;

			//IFlowStatement prevStatement = null;

			while(true)
			{
				var flowStatement = (IFlowStatement)cur;

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

				flowStatement.Analyze(this);
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
			var statements = new List<IFlowStatement>();

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
			BlockFlowData scope = null;
			//Setup initial scope around programFlowData.
			foreach(var statement in Statements)
			{
				//TODO: For scope, nesting loops won't be an issue. Loops inside of a final switch case or at the end of a subroutine can have overlap.
				//We should defaulting to the innermost loop (though it should work either way).
				//Otherwise, pay attention to the jump destination to determine the probper scope break.
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

				//if(statement.StackStatement.TryGetSubroutineName(out var subroutineName))
				//{
				//	subroutineType = statement.StackStatement.FirstInstruction.SubroutineType;
				//	var defKeyword = subroutineType switch
				//	{
				//		SubroutineType.Strat => "strat",
				//		SubroutineType.Trigger => "trigger",
				//		SubroutineType.Proc => "defproc",
				//		_ => throw new Exception("Unknown subroutine type")
				//	};
				//	writer.WriteLine($"{defKeyword} {subroutineName}");
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