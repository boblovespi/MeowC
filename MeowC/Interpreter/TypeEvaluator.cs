using MeowC.Interpreter.Types;
using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Interpreter;

public class TypeEvaluator : IEvaluator<Type>
{
	// private IEvaluator<Type> Me => this;

	public Type Evaluate(Expression expression, Dictionary<IdValue, Type> bindings, Type? hint = null) =>
		expression switch
		{
			Expression.Application app => Apply(app, bindings, hint),
			Expression.BinaryOperator binOp => BinOp(binOp, bindings, hint),
			Expression.Case @case => Cases(@case, bindings, hint),
			Expression.Identifier id => Id(id, bindings),
			Expression.Number num => new Type.IntLiteral(num.Value),
			Expression.Prefix prefix => throw new NotImplementedException(),
			Expression.Procedure procedure => Procedure(procedure, bindings, hint),
			Expression.String => Type.ConstString,
			Expression.Unit => Type.Unit,
			Expression.Tuple tuple => Tuple(tuple, bindings, hint),
			_ => throw new NotImplementedException(
				$"We are missing type checking for expression {expression.GetType()}! {expression.Token.ErrorString}")
		};

	public Type Cases(Expression.Case cases, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		if (hint == null)
			throw new NotImplementedException($"Our cases hint is null! help! at {cases.Token.ErrorString}");
		foreach (var @case in cases.Cases)
		{
			switch (@case)
			{
				case Case.Bool boolCase:
					if (!(Evaluate(boolCase.Pattern, bindings, Type.Bool) & Type.Bool))
						throw new TokenException($"Case patterns is not a boolean", boolCase.Pattern.Token);
					if (!(Evaluate(boolCase.Value, bindings, hint) & hint))
						throw new TokenException($"Expected case to be {hint}", boolCase.Value.Token);
					break;

				case Case.Otherwise otherwiseCase:
					if (Evaluate(otherwiseCase.Value, bindings, hint) & hint) break;
					throw new TokenException($"Expected case to be {hint}", otherwiseCase.Value.Token);
			}
		}

		return hint;
	}

	public Type BinOp(Expression.BinaryOperator binOp, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		if (binOp.Type == TokenTypes.FuncType)
		{
			var l = Evaluate(binOp.Left, bindings, hint);
			if (l is Type.TypeIdentifier ll)
				l = ll.Type;
			var r = Evaluate(binOp.Right, bindings, hint);
			if (r is Type.TypeIdentifier rr)
				r = rr.Type;
			return new Type.TypeIdentifier(new Type.Function(l, r));
		}
		if (binOp.Type == TokenTypes.MapsTo)
			switch (hint)
			{
				case null:
					if (binOp.Left is not Expression.Identifier)
						throw new TokenException("Functions require bindings", binOp.Token);
					return Evaluate(binOp.Right, bindings);
				case Type.Function function:
					var left = binOp.Left;
					switch (function.From)
					{
						case Type.Enum { Value: 1 }:
							goto default;
						case Type.Product:
							{
								if (left is not Expression.Tuple tuple)
									throw new TokenException("Functions require bindings", binOp.Token);
								var newBindings = new Dictionary<IdValue, Type>(bindings);
								var right = function.From;
                                // if (function.From is not Type.Product right)
                                // 	throw new Exception($"Expected product type, got {function.From} at {binOp.Token.ErrorString}");
                                for (int i = 0; i < tuple.Values.Count - 1; i++)
								{
                                    var value = tuple.Values[i];
                                    if (right is not Type.Product rr)
										throw new TokenException($"Expected product type, got {function.From}", binOp.Token);
									right = rr.Right;
									var pleft = rr.Left;
									if (value is not Expression.Identifier id)
										throw new TokenException($"Expected identifier for function, got {value}", value.Token);
									newBindings[new IdValue(id.Name)] = pleft;
								}
								if (tuple.Values.Last() is not Expression.Identifier idL)
									throw new TokenException($"Expected identifier for function, got {tuple.Values.Last()}", tuple.Values.Last().Token);
								newBindings[new IdValue(idL.Name)] = right;
								return Evaluate(binOp.Right, newBindings, function.To);
							}
						default:
							{
								if (left is not Expression.Identifier bind)
									throw new TokenException($"Functions require bindings", binOp.Token);
								var newBindings = new Dictionary<IdValue, Type>(bindings)
								{
									[new IdValue(bind.Name)] = function.From
								};
								return Evaluate(binOp.Right, newBindings, function.To);
							}
					}
					throw new Exception("shouldn't be reached");
				default:
					throw new TokenException($"Expected a function type but got {hint} instead", binOp.Token);
			}

		if (binOp.Type == TokenTypes.Equals)
		{
			var l = Evaluate(binOp.Left, bindings);
			var r = Evaluate(binOp.Right, bindings);
			return (l & r) switch
			{
				true => Type.Bool,
				false => throw new TokenException($"Cannot compare values of different types {l}, {r}", binOp.Token)
			};
		}

		if (binOp.Type == TokenTypes.Times)
		{
			var l = Evaluate(binOp.Left, bindings);
			var r = Evaluate(binOp.Right, bindings);
			if (l is Type.TypeIdentifier ll && r is Type.TypeIdentifier rr)
				return new Type.TypeIdentifier(new Type.Product(ll.Type, rr.Type));
			return (IsNumeric(l) && IsNumeric(r), l & r) switch
			{
				(true, true) => l,
				(true, false) => throw new TokenException($"Cannot multiply values of different types {l}, {r}", binOp.Token),
				(false, _) => throw new TokenException($"Cannot multiply values of non-numeric types {l}, {r}", binOp.Token)
			};
		}
		if (binOp.Type == TokenTypes.Plus)
		{
			var l = Evaluate(binOp.Left, bindings);
			var r = Evaluate(binOp.Right, bindings);
			if (l is Type.TypeIdentifier ll && r is Type.TypeIdentifier rr)
				return new Type.TypeIdentifier(new Type.Sum(ll.Type, rr.Type));
			return (IsNumeric(l) && IsNumeric(r), l & r) switch
			{
				(true, true) => l,
				(true, false) => throw new TokenException($"Cannot add values of different types {l}, {r}", binOp.Token),
				(false, _) => throw new TokenException($"Cannot add values of non-numeric types {l}, {r}", binOp.Token)
			};
		}
		if (binOp.Type == TokenTypes.Slash || binOp.Type == TokenTypes.Minus)
		{
			var l = Evaluate(binOp.Left, bindings);
			var r = Evaluate(binOp.Right, bindings);
			return (IsNumeric(l) && IsNumeric(r), l & r) switch
			{
				(true, true) => l,
				(true, false) => throw new TokenException($"Cannot subtract/divide values of different types {l}, {r}", binOp.Token),
				(false, _) => throw new TokenException($"Cannot subtract/divide values of non-numeric types {l}, {r}", binOp.Token)
			};
		}

		throw new NotImplementedException($"Type checking not implemented for token at {binOp.Token.ErrorString}");
	}

	public Type Apply(Expression.Application app, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		var fun = Evaluate(app.Function, bindings);
		var arg = Evaluate(app.Argument, bindings);
		return fun switch
		{
			Type.Function f when f.From & arg => f.To,
			Type.Function f => throw new TokenException($"Type {f} takes a {f.From}, but got a {arg}", app.Token),
			_ => throw new TokenException($"Type {fun} is not a function", app.Token)
		};
	}

	private Type Tuple(Expression.Tuple tuple, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		var product = Evaluate(tuple.Values[0], bindings);
		for (var i = 1; i < tuple.Values.Count; i++)
			product = new Type.Product(product, Evaluate(tuple.Values[i], bindings));

		return product;
	}

	public Type Procedure(Expression.Procedure procedure, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		var oldBindings = bindings;
		bindings = new Dictionary<IdValue, Type>(bindings);
		foreach (var definition in procedure.Definitions) 
			bindings[new IdValue(definition.Id)] = TypeChecker.NormalizeTypes(Evaluate(definition.Type, oldBindings));
		Type? type = null;
		foreach (var statement in procedure.Statements)
		{
			switch (statement)
			{
				case Statement.Assignment assignment:
					Evaluate(assignment.Value, bindings, bindings[new IdValue(assignment.Variable)]);
					break;
				case Statement.Callable callable:
					if (callable.Routine == "print")
					{
						var value = Evaluate(callable.Argument, bindings);
					}
					break;
				case Statement.Return @return:
					if (type == null)
						type = Evaluate(@return.Argument, bindings, hint);
					else
					{
						var otherType = Evaluate(@return.Argument, bindings, hint);
						if (!(type & otherType))
							throw new TokenException($"Type {otherType} does not match {type}", @return.Argument.Token);
					}
					if (hint != null && !(type & hint))
						throw new TokenException($"Expected return of type {hint}, but got {type}", @return.Argument.Token);
					break;
				default:
					throw new NotImplementedException(nameof(statement));
			}
		}
		return type ?? Type.Unit;
	}

	private Type Id(Expression.Identifier identifier, Dictionary<IdValue, Type> bindings)
	{
		switch (identifier)
		{
			case { Name: "i32" }: return new Type.TypeIdentifier(new Type.Builtin(Builtins.I32));
			case { Name: "u8" }: return new Type.TypeIdentifier(new Type.Builtin(Builtins.U8));
			case { Name: "proc" }: return new Type.TypeIdentifier(new Type.Builtin(Builtins.Proc));
			default:
				var maybe = bindings.GetValueOrDefault(new IdValue(identifier.Name));
				if (maybe == null) throw new TokenException($"No identifier found for {identifier.Name}", identifier.Token);
				return maybe;
		}
	}

	private bool IsNumeric(Type type) => type & new Type.IntLiteral(0);
}