using MeowC.Parser.Matches;

namespace MeowC.Interpreter;

public class RuntimeEvaluator(List<Definition> definitions) : IEvaluator<object>
{
	private List<Definition> Definitions { get; } = definitions;
	private IEvaluator<object> Me => this;

	public object Evaluate(Expression expression, Dictionary<IdValue, object> bindings, object? hint = null) =>
		expression switch
		{
			Expression.Number number => number.Value,
			Expression.String @string => @string.Value,
			Expression.Identifier id => new IdValue(id.Name),
			Expression.Application app => Apply(app, bindings),
			Expression.BinaryOperator binOp => BinOp(binOp, bindings),
			Expression.Case @case => Cases(@case, bindings),
			Expression.Procedure procedure => Procedure(procedure, bindings),
			_ => ""
		};

	public object Cases(Expression.Case cases, Dictionary<IdValue, object> bindings, object? hint = null)
	{
		foreach (var @case in cases.Cases)
		{
			switch (@case)
			{
				case Case.Bool boolCase:
					if (Evaluate(boolCase.Pattern, bindings) is true) return Evaluate(boolCase.Value, bindings);
					break;
				case Case.Otherwise otherwise:
					return Evaluate(otherwise.Value, bindings);
			}
		}

		throw new Exception("Not all cases are defined!");
	}

	public object BinOp(Expression.BinaryOperator binOp, Dictionary<IdValue, object> bindings, object? hint = null)
	{
		if (binOp.Type == TokenTypes.MapsTo)
		{
			var left = binOp.Left;
			if (left is not Expression.Identifier bind) throw new Exception("Functions require bindings");
			var bindId = new IdValue(bind.Name);
			return new Func<object, object>(x =>
			{
				var newBindings = new Dictionary<IdValue, object>(bindings)
				{
					[bindId] = x
				};
				return Evaluate(binOp.Right, newBindings);
			});
		}

		if (binOp.Type == TokenTypes.Times)
		{
			var left = Me.Unbind(Evaluate(binOp.Left, bindings), bindings);
			var right = Me.Unbind(Evaluate(binOp.Right, bindings), bindings);
			if (left is long ll && right is long rr) return ll * rr;
			throw new Exception("Type mismatch");
		}

		if (binOp.Type == TokenTypes.Minus)
		{
			var left = Me.Unbind(Evaluate(binOp.Left, bindings), bindings);
			var right = Me.Unbind(Evaluate(binOp.Right, bindings), bindings);
			if (left is long ll && right is long rr) return ll - rr;
			throw new Exception("Type mismatch");
		}

		if (binOp.Type == TokenTypes.Equals)
		{
			var left = Me.Unbind(Evaluate(binOp.Left, bindings), bindings);
			var right = Me.Unbind(Evaluate(binOp.Right, bindings), bindings);
			return left.Equals(right);
		}

		throw new NotImplementedException();
	}

	public object Apply(Expression.Application app, Dictionary<IdValue, object> bindings, object? hint = null)
	{
		var func = Evaluate(app.Function, bindings);
		if (func is IdValue id) func = FindFunction(id, bindings);
		if (func is Func<object, object> f) return f(Evaluate(app.Argument, bindings));
		throw new Exception("Not a function: " + app.Function);
	}

	public object Procedure(Expression.Procedure procedure, Dictionary<IdValue, object> bindings, object? hint = null)
	{
		throw new NotImplementedException();
	}

	private Func<object, object>? FindFunction(IdValue identifier, Dictionary<IdValue, object> bindings)
	{
		var definition = Definitions.Find(d => d.Id == identifier.Name);
		if (definition == null) return null;
		if (Evaluate(definition.Val, bindings) is Func<object, object> func) return func;
		return null;
	}
}