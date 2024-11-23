using MeowC.Parser.Matches;

namespace MeowC.Interpreter;

public interface IEvaluator<T>
{
	internal T Evaluate(Expression expression, Dictionary<IdValue, T> bindings, T? hint = default);
	internal T Cases(Expression.Case cases, Dictionary<IdValue, T> bindings, T? hint = default);
	internal T BinOp(Expression.BinaryOperator binOp, Dictionary<IdValue, T> bindings, T? hint = default);
	internal T Apply(Expression.Application app, Dictionary<IdValue, T> bindings, T? hint = default);
	internal T Procedure(Expression.Procedure procedure, Dictionary<IdValue, T> bindings, T? hint = default);

	internal T Unbind(object maybe, Dictionary<IdValue, T> bindings)
	{
		return maybe is IdValue id2 && bindings.TryGetValue(id2, out var bound2) ? bound2 : (T)maybe;
	}
}