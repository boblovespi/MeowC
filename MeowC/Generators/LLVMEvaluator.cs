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
			Expression.String @string => Builder.BuildGlobalString(@string.Value + "\0"),
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
		unsafe
		{
			return LLVM.ConstInt(Context.Int32Type, (ulong)num.Value, 0);
		}
	}

	public LLVMValueRef Cases(Expression.Case cases, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		var func = Builder.InsertBlock.Parent;
		var finallyBB = func.AppendBasicBlock("finally");
		var nextBB =  func.AppendBasicBlock("ifnot");
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
					Builder.BuildCondBr(cond, caseBB, nextBB);
					Builder.PositionAtEnd(caseBB);
					result = Evaluate(expression, bindings);
					Builder.BuildBr(finallyBB);
					phiVal.Add(result);
					phiBlock.Add(Builder.InsertBlock);
					Builder.PositionAtEnd(nextBB);
					nextBB =  func.AppendBasicBlock("ifnot");
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
					throw new ArgumentOutOfRangeException(nameof(@case));
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
		throw new NotImplementedException();
	}

	public LLVMTypeRef LLVMType(Type type)
	{
		return type switch
		{
			Type.Builtin builtin => builtin.Value switch
			{
				Builtins.I8 => Context.Int8Type,
				Builtins.I16 => Context.Int16Type,
				Builtins.I32 => Context.Int32Type,
				Builtins.I64 => Context.Int64Type,
				Builtins.U8 => Context.Int8Type,
				Builtins.U16 => Context.Int16Type,
				Builtins.U32 => Context.Int32Type,
				Builtins.U64 => Context.Int64Type,
				Builtins.F32 => Context.FloatType,
				Builtins.F64 => Context.DoubleType,
				Builtins.Proc => throw new NotImplementedException()
			},
			Type.CString cString => gen.PtrType,
			Type.Enum @enum => Context.Int8Type,
			Type.TypeIdentifier typeId => LLVMType(typeId.Type),
			_ => throw new NotImplementedException(nameof(type))
		};
	}
}