using LLVMSharp.Interop;
using MeowC.Interpreter;
using MeowC.Interpreter.Types;
using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Generators;

public class LLVMEvaluator(
	LLVMContextRef context,
	LLVMBuilderRef builder,
	LLVMModuleRef module,
	Dictionary<Expression, Type> typeTable,
	LLVMGen gen) : IEvaluator<LLVMValueRef>
{
	public LLVMContextRef Context { get; } = context;
	public LLVMBuilderRef Builder { get; } = builder;
	public LLVMModuleRef Module { get; } = module;
	public Dictionary<Expression, Type> TypeTable { get; } = typeTable;

	public LLVMValueRef Evaluate(Expression expression, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default) =>
		expression switch
		{
			Expression.Application app => Apply(app, bindings, hint),
			Expression.BinaryOperator binOp => BinOp(binOp, bindings, hint),
			Expression.Case @case => Cases(@case, bindings, hint),
			Expression.Identifier id => Id(id, bindings),
			Expression.Number num => Num(num, bindings),
			Expression.Prefix prefix => throw new NotImplementedException(),
			Expression.Procedure procedure => Procedure(procedure, bindings, hint),
			Expression.String @string => Builder.BuildGlobalString(@string.Value + "\0", @string.Value[..Math.Min(@string.Value.Length, 4)]),
			Expression.Unit => null,
			Expression.Tuple tuple => null,
			_ => throw new NotImplementedException(
				$"We are missing llvm emission for expression {expression.GetType()}! {expression.Token.ErrorString}")
		};

	private LLVMValueRef Id(Expression.Identifier id, Dictionary<IdValue, LLVMValueRef> bindings)
	{
		if (!bindings.TryGetValue(new IdValue(id.Name), out var value))
		{
			var newName = id.Name + "_" + TypeTable[id];
			var func = Module.GetNamedFunction(id.Name);
			if (func.Handle == IntPtr.Zero)
				throw new TokenException($"{id.Name} is undefined", id.Token);
			return func;
		}

		return value.IsAAllocaInst != null ? Builder.BuildLoad2(LLVMType(TypeTable[id]), value, "loadtmp") : value;
	}

	private LLVMValueRef Num(Expression.Number num, Dictionary<IdValue, LLVMValueRef> bindings)
	{
		return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)num.Value);
	}

	public LLVMValueRef Cases(Expression.Case cases, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		var func = Builder.InsertBlock.Parent;
		var finallyBB = func.AppendBasicBlock("finally");
		var phiVal = new List<LLVMValueRef>();
		var phiBlock = new List<LLVMBasicBlockRef>();
		foreach (var @case in cases.Cases)
		{
			LLVMValueRef result;
			switch (@case)
			{
				case Case.Bool(var expression, var pattern):
					var cond = Evaluate(pattern, bindings);
					var caseBB = func.AppendBasicBlock("ifcase");
					var nextBB = func.AppendBasicBlock("ifnot");
					Builder.BuildCondBr(cond, caseBB, nextBB);
					Builder.PositionAtEnd(caseBB);
					result = Evaluate(expression, bindings);
					Builder.BuildBr(finallyBB);
					phiVal.Add(result);
					phiBlock.Add(Builder.InsertBlock);
					Builder.PositionAtEnd(nextBB);
					break;
				case Case.Otherwise otherwise:
					var otherwiseBB = func.AppendBasicBlock("otherwise");
					Builder.BuildBr(otherwiseBB);
					Builder.PositionAtEnd(otherwiseBB);
					result = Evaluate(otherwise.Value, bindings);
					Builder.BuildBr(finallyBB);
					phiVal.Add(result);
					phiBlock.Add(Builder.InsertBlock);
					break;
				default:
					throw new Exception("unknown case type" + nameof(@case));
			}
		}

		Builder.PositionAtEnd(finallyBB);
		var phi = Builder.BuildPhi(LLVMType(TypeTable[cases]), "casetemp");
		phi.AddIncoming(phiVal.ToArray(), phiBlock.ToArray(), (uint)phiVal.Count);
		return phi;
	}

	public LLVMValueRef BinOp(Expression.BinaryOperator binOp, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		var left = Evaluate(binOp.Left, bindings, hint);
		var right = Evaluate(binOp.Right, bindings, hint);
		return binOp switch
		{
			_ when binOp.Type == TokenTypes.Plus => Builder.BuildAdd(left, right, "addtemp"),
			_ when binOp.Type == TokenTypes.Minus => Builder.BuildSub(left, right, "subtemp"),
			_ when binOp.Type == TokenTypes.Times => Builder.BuildMul(left, right, "multemp"),
			_ when binOp.Type == TokenTypes.Slash => Builder.BuildSDiv(left, right, "sdivtemp"),
			_ when binOp.Type == TokenTypes.Equals => Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eqtemp"),
			_ when binOp.Type == TokenTypes.Less => Builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lesstemp"),
			_ => throw new NotImplementedException()
		};
	}

	public LLVMValueRef Apply(Expression.Application app, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		var func = Evaluate(app.Function, bindings);
		var args = new LLVMValueRef[1];
		args[0] = Evaluate(app.Argument, bindings);
		var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, [LLVMTypeRef.Int32]);
		return Builder.BuildCall2(funcType, func, args, "calltemp");
	}

	public LLVMValueRef Procedure(Expression.Procedure procedure, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		var procType = LLVMType(TypeTable[procedure]);
		var oldBB = Builder.InsertBlock;
		// var procFuncType = LLVMTypeRef.CreateFunction(procType, []);
		// var function = Module.AddFunction("procwrapper", procFuncType);
		// var attribute = Context.CreateEnumAttribute((uint)AttributeKind.AlwaysInline, 0);
		// function.AddAttributeAtIndex(LLVMAttributeIndex.LLVMAttributeFunctionIndex, attribute);
		var function = Builder.InsertBlock.Parent;
		var entryBB = function.EntryBasicBlock;
		// var bb = function.AppendBasicBlock("proc");

		// Builder.BuildBr(bb);
		Builder.Position(entryBB, entryBB.FirstInstruction);

		foreach (var (varName, expression, val) in procedure.Definitions)
		{
			var type = LLVMType(TypeTable[expression]);
			var alloca = Builder.BuildAlloca(type, varName);
			if (val is not null)
				Builder.BuildStore(Evaluate(val, bindings), alloca);
			bindings[new IdValue(varName)] = alloca;
		}

		Builder.PositionAtEnd(oldBB);
		var builtRet = false;
		LLVMValueRef ret = default;

		foreach (var statement in procedure.Statements)
		{
			switch (statement)
			{
				case Statement.Callable(var routine, var expression):
					if (!bindings.TryGetValue(new IdValue(routine), out var func))
					{
						var typeStr = TypeTable[expression] switch
						{
							Type.CString => "str_t",
							Type.IntLiteral => "i32_t",
							Type.Builtin builtin => builtin.Value.ToString().ToLowerInvariant() + "_t",
							{ } a => throw new ArgumentOutOfRangeException(a.ToString())
						};
						func = Module.GetNamedFunction($"{routine}:3{typeStr}");
					}

					var args = new LLVMValueRef[1];
					args[0] = Evaluate(expression, bindings);
					var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [LLVMType(TypeTable[expression])]);
					var callTmp = Builder.BuildCall2(funcType, func, args);
					break;

				case Statement.Assignment(var variable, var expression):
					var val = Evaluate(expression, bindings);
					Builder.BuildStore(val, bindings[new IdValue(variable)]);
					break;
				case Statement.Return(var expression):
					builtRet = true;
					ret = Evaluate(expression, bindings);
					break;
			}
		}

		// if (!builtRet)
		// 	Builder.BuildRetVoid();
		// function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
		// var contBB = function.AppendBasicBlock("cont");
		// Builder.BuildBr(contBB);
		// Builder.PositionAtEnd(contBB);
		// var phi = Builder.BuildPhi(LLVMType(TypeTable[procedure]), "phi");
		// phi.AddIncoming([ret], [bb], 1);
		return builtRet ? ret : LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0);
	}

	public LLVMTypeRef LLVMType(Type type)
	{
		return type switch
		{
			Type.Builtin builtin => builtin.Value switch
			{
				Builtins.I8 => LLVMTypeRef.Int8,
				Builtins.I16 => LLVMTypeRef.Int16,
				Builtins.I32 => LLVMTypeRef.Int32,
				Builtins.I64 => LLVMTypeRef.Int64,
				Builtins.U8 => LLVMTypeRef.Int8,
				Builtins.U16 => LLVMTypeRef.Int16,
				Builtins.U32 => LLVMTypeRef.Int32,
				Builtins.U64 => LLVMTypeRef.Int64,
				Builtins.F32 => LLVMTypeRef.Float,
				Builtins.F64 => LLVMTypeRef.Double,
				Builtins.Proc => throw new NotImplementedException()
			},
			Type.CString cString => gen.PtrType,
			Type.Enum @enum => LLVMTypeRef.Int8,
			Type.TypeIdentifier typeId => LLVMType(typeId.Type),
			_ => throw new NotImplementedException(nameof(type))
		};
	}
}