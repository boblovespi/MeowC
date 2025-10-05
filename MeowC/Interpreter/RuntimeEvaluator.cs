using MeowC.Interpreter.Types;
using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Interpreter;

public class RuntimeEvaluator(List<Definition> definitions, Dictionary<Expression, Type> typeTable) : IEvaluator<object>
{
	private List<Definition> Definitions { get; } = definitions;
	private Dictionary<Expression, Type> TypeTable { get; } = typeTable;
	private IEvaluator<object> Me => this;

	public object Evaluate(Expression expression, Dictionary<IdValue, object> bindings, object? hint = null) =>
		expression switch
		{
			// Expression.Number number when TypeTable[number] & new Type.Builtin(Builtins.U8) => (byte)number.Value,
			Expression.Number number => number.Value,
			Expression.String @string => @string.Value,
			Expression.Identifier id => Me.Unbind((IdValue)id.Name, bindings),
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
					var bindId = (IdValue)bind.Name;
					var type = TypeTable[binOp];
					if (type is not Type.Function fun)
						throw new Exception("wtf typechecking failed");
					var lType = fun.From;
					var rType = fun.To;
					return new Func<object, object>(x =>
					{
						var newBindings = new Dictionary<IdValue, object>(bindings)
						{
							// [bindId] = lType == new Type.Builtin(Builtins.U8) ? Convert.ToByte(x) : lType & new Type.IntLiteral(0) ? Convert.ToInt64(x) : x
							[bindId] = x
						};
						var o = Evaluate(binOp.Right, newBindings);
						// return rType == new Type.Builtin(Builtins.U8) ? Convert.ToByte(o) : rType & new Type.IntLiteral(0) ? Convert.ToByte(o) : o;
						return o;
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
							newBindings[idf.Name] = xs[i];
						}

						return Evaluate(binOp.Right, newBindings);
					});
				default:
					throw new Exception("Functions require bindings");
			}
			// if (left is not Expression.Identifier bind) throw new Exception("Functions require bindings");
		}

		if (binOp.Type == TokenTypes.DoubleMapsTo)
		{
			var left = binOp.Left;
			switch (left)
			{
				case Expression.Identifier bind:
					var bindId = (IdValue)bind.Name;
					return new Func<object, object>(x =>
					{
						// Program.Info("hi lol");
						return Evaluate(binOp.Right, bindings);
					});
			}
		}

		switch (binOp)
		{
			case var _ when binOp.Type == TokenTypes.MapsTo:
				break;
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
			return left switch
			{
				long ll when right is long rr => ll - rr,
				byte lb when right is byte rc => (long)lb - rc,
				long ll when right is byte rc2 => ll - rc2,
				byte lb when right is long rl => lb - rl,
				_ => throw new Exception("Type mismatch")
			};
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

		if (binOp.Type == TokenTypes.Less)
		{
			var left = Me.Unbind(Evaluate(binOp.Left, bindings), bindings);
			var right = Me.Unbind(Evaluate(binOp.Right, bindings), bindings);
			if (left is long ll && right is long rr) return ll < rr;
			throw new Exception("Type mismatch");
		}

		throw new NotImplementedException(binOp.Type.ToString());
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
			bindings[definition.Id] = new object();
		foreach (var statement in procedure.Statements)
		{
			switch (statement)
			{
				case Statement.Assignment(var variable, var expression):
					var var = variable;
					if (!bindings.ContainsKey(var)) throw new Exception($"Variable {variable} is not defined!");
					bindings[var] = Evaluate(expression, bindings);
					break;
				case Statement.Callable(var routine, var expression):
					if (routine == "print")
					{
						var value = Evaluate(expression, bindings);
						if (value is IdValue id)
							value = bindings[id];
						switch (value)
						{
							case long l and <= byte.MaxValue and >= byte.MinValue
								when TypeTable[expression] & new Type.Builtin(Builtins.U8):
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