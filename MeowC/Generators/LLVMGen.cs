using LLVMSharp;
using LLVMSharp.Interop;
using MeowC.Interpreter;
using MeowC.Parser.Matches;

namespace MeowC.Generators;

public class LLVMGen : IEvaluator<LLVMValueRef>
{
	public LLVMContext Context { get; }
	public LLVMBuilderRef Builder { get; }
	public Module Module { get; }
	public Dictionary<string, LLVMValueRef> NamedValues { get; } = new();

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
			Expression.String => null,
			Expression.Unit => null,
			Expression.Tuple tuple => null,
			_ => throw new NotImplementedException(
				$"We are missing type checking for expression {expression.GetType()}! {expression.Token.ErrorString}")
		};

	private LLVMValueRef Id(Expression.Identifier id, Dictionary<IdValue, LLVMValueRef> bindings)
	{
		if (!bindings.TryGetValue(new IdValue(id.Name), out var value))
			throw new TokenException($"{id.Name} is undefined", id.Token);
		return value;
	}

	private LLVMValueRef Num(Expression.Number num, Dictionary<IdValue, LLVMValueRef> bindings)
	{
		unsafe
		{
			return LLVM.ConstInt(LLVM.Int64Type(), (ulong)num.Value, 0);
		}
	}

	public LLVMValueRef Cases(Expression.Case cases, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		throw new NotImplementedException();
	}

	public LLVMValueRef BinOp(Expression.BinaryOperator binOp, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		var left = Evaluate(binOp.Left, bindings, hint);
		var right = Evaluate(binOp.Right, bindings, hint);
		return binOp switch
		{
			not null when binOp.Type == TokenTypes.Plus => Builder.BuildAdd(left, right, "addtemp"),
			_ => throw new NotImplementedException()
		};
	}

	public LLVMValueRef Apply(Expression.Application app, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		throw new NotImplementedException();
	}

	public LLVMValueRef Procedure(Expression.Procedure procedure, Dictionary<IdValue, LLVMValueRef> bindings, LLVMValueRef hint = default)
	{
		throw new NotImplementedException();
	}
}