using MeowC.Parser.Matches;

namespace MeowC.Interpreter;

public interface IEvaluator<T>
{
	internal T Evaluate(Expression expression, Dictionary<IdValue, object> bindings);
	internal T Cases(Expression.Case cases, Dictionary<IdValue, T> bindings);
	internal T BinOp(Expression.BinaryOperator binaryOperator, Dictionary<IdValue, T> bindings);
	internal T Apply(Expression.Application expression, Dictionary<IdValue, T> bindings);

	internal T Unbind(object maybe, Dictionary<IdValue, T> bindings)
	{
		return maybe is IdValue id2 && bindings.TryGetValue(id2, out var bound2) ? bound2 : (T)maybe;
	}
}