using MeowC.Parser.Matches;

namespace MeowC.Interpreter;

public class Interpreter(List<Definition> definitions)
{
	private List<Definition> Definitions { get; } = definitions;

	public void Run()
	{
		foreach (var definition in Definitions)
			if (definition is { Id: "main", Val: Expression.Procedure procedure })
				RunProcedure(procedure);
	}

	private void RunProcedure(Expression.Procedure procedure)
	{
		foreach (var statement in procedure.Statements) RunStatement(statement);
	}

	private void RunStatement(Statement statement)
	{
		switch (statement)
		{
			case Statement.Callable callable:
				if (callable.Routine == "print")
				{
					var value = Evaluate(callable.Argument);
					switch (value)
					{
						case long l and <= byte.MaxValue and >= byte.MinValue:
							Console.Write((char)l);
							break;
						case long l:
							Console.Write(l);
							break;
						default:
							Console.Write(value.ToString());
							break;
					}
				}

				break;
		}
	}

	private Func<object, object>? FindFunction(IdValue identifier)
	{
		var definition = Definitions.Find(d => d.Id == identifier.Name);
		if (definition == null) return null;
		if (Evaluate(definition.Val) is Func<object, object> func) return func;
		return null;
	}

	private object Evaluate(Expression expression, Dictionary<IdValue, object>? bindings = null) =>
		expression switch
		{
			Expression.Number number => number.Value,
			Expression.String @string => @string.Value,
			Expression.Identifier id => new IdValue(id.Name),
			Expression.Application app => Apply(app, bindings),
			Expression.BinaryOperator binOp => BinOp(binOp, bindings),
			Expression.Case @case => Cases(@case, bindings),
			_ => ""
		};

	private object Cases(Expression.Case cases, Dictionary<IdValue,object>? bindings)
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

	private object BinOp(Expression.BinaryOperator binOp, Dictionary<IdValue, object>? bindings)
	{
		if (binOp.Type == TokenTypes.MapsTo)
		{
			var left = binOp.Left;
			if (left is not Expression.Identifier bind) throw new Exception("Functions require bindings");
			var bindId = new IdValue(bind.Name);
			return new Func<object, object>(x =>
			{
				var newBindings = bindings ?? new Dictionary<IdValue, object>();
				newBindings[bindId] = x;
				return Evaluate(binOp.Right, newBindings);
			});
		}

		if (binOp.Type == TokenTypes.Times)
		{
			var left = Evaluate(binOp.Left, bindings);
			var right = Evaluate(binOp.Right, bindings);
			if (left is IdValue id && bindings != null && bindings.TryGetValue(id, out var bound)) left = bound;
			if (right is IdValue id2 && bindings != null && bindings.TryGetValue(id2, out var bound2)) right = bound2;
			if (left is long ll && right is long rr) return ll * rr;
			throw new Exception("Type mismatch");
		}

		if (binOp.Type == TokenTypes.Minus)
		{
			var left = Evaluate(binOp.Left, bindings);
			var right = Evaluate(binOp.Right, bindings);
			if (left is IdValue id && bindings != null && bindings.TryGetValue(id, out var bound)) left = bound;
			if (right is IdValue id2 && bindings != null && bindings.TryGetValue(id2, out var bound2)) right = bound2;
			if (left is long ll && right is long rr) return ll - rr;
			throw new Exception("Type mismatch");
		}
		if (binOp.Type == TokenTypes.Equals)
		{
			var left = Evaluate(binOp.Left, bindings);
			var right = Evaluate(binOp.Right, bindings);
			if (left is IdValue id && bindings != null && bindings.TryGetValue(id, out var bound)) left = bound;
			if (right is IdValue id2 && bindings != null && bindings.TryGetValue(id2, out var bound2)) right = bound2;
			return left.Equals(right);
		}
		throw new NotImplementedException();
	}

	private object Apply(Expression.Application app, Dictionary<IdValue, object>? bindings)
	{
		var func = Evaluate(app.Function);
		if (func is IdValue id) func = FindFunction(id);
		if (func is Func<object, object> f) return f(Evaluate(app.Argument, bindings));
		throw new Exception("Not a function: " + app.Function);
	}
}