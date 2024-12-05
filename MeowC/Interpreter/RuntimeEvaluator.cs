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
			Expression.Tuple tuple => Tuple(tuple, bindings),
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
			switch (left)
			{
				case Expression.Identifier bind:
					var bindId = new IdValue(bind.Name);
					return new Func<object, object>(x =>
					{
						var newBindings = new Dictionary<IdValue, object>(bindings)
						{
							[bindId] = x
						};
						return Evaluate(binOp.Right, newBindings);
					});
				case Expression.Tuple tuple:
					return new Func<object, object>(x =>
					{
						if (x is not List<object> xs)
							throw new Exception("Tuple takes a tuple");
						var newBindings = new Dictionary<IdValue, object>(bindings);
						for (int i = 0; i < tuple.Values.Count; i++)
						{
							var id = tuple.Values[i];
							if (id is not Expression.Identifier idf)
								throw new Exception("Functions require bindings!");
							newBindings[new IdValue(idf.Name)] = xs[i];
						}

						return Evaluate(binOp.Right, newBindings);
					});
				default:
					throw new Exception("Functions require bindings");
			}
			// if (left is not Expression.Identifier bind) throw new Exception("Functions require bindings");
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

		if (binOp.Type == TokenTypes.Plus)
		{
			var left = Me.Unbind(Evaluate(binOp.Left, bindings), bindings);
			var right = Me.Unbind(Evaluate(binOp.Right, bindings), bindings);
			if (left is long ll && right is long rr) return ll + rr;
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
		if (func is not Func<object, object> f) throw new Exception("Not a function: " + app.Function);
		var arg = Evaluate(app.Argument, bindings);
		if (arg is IdValue id2) arg = bindings[id2];
		return f(arg);
	}

	public object Procedure(Expression.Procedure procedure, Dictionary<IdValue, object> bindings, object? hint = null)
	{
		var oldBindings = bindings;
		bindings = new Dictionary<IdValue, object>(bindings);
		foreach (var definition in procedure.Definitions)
			bindings[new IdValue(definition.Id)] = new object();
		foreach (var statement in procedure.Statements)
		{
			switch (statement)
			{
				case Statement.Assignment(var variable, var expression):
					var var = new IdValue(variable);
					if (!bindings.ContainsKey(var)) throw new Exception($"Variable {variable} is not defined!");
					bindings[var] = Evaluate(expression, bindings);
					break;
				case Statement.Callable(var routine, var expression):
					if (routine == "print")
					{
						var value = Evaluate(expression, bindings);
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
				case Statement.Return @return:
					return Evaluate(@return.Argument, bindings);
				default:
					throw new NotImplementedException(nameof(statement));
			}
		}

		return null;
	}

	private object Tuple(Expression.Tuple tuple, Dictionary<IdValue, object> bindings) =>
		tuple.Values.ConvertAll(x => Evaluate(x, bindings));

	private Func<object, object>? FindFunction(IdValue identifier, Dictionary<IdValue, object> bindings)
	{
		var definition = Definitions.Find(d => d.Id == identifier.Name);
		if (definition == null) return null;
		if (Evaluate(definition.Val, bindings) is Func<object, object> func) return func;
		return null;
	}
}