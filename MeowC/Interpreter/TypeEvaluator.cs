﻿using System.Runtime.InteropServices.ComTypes;
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
			Expression.Procedure => new Type.Builtin(Builtins.Proc),
			Expression.String => Type.ConstString,
			_ => throw new ArgumentOutOfRangeException(nameof(expression))
		};

	public Type Cases(Expression.Case cases, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		if (hint == null)
			throw new Exception($"Our cases hint is null! help! at {cases.Token.ErrorString}");
		foreach (var @case in cases.Cases)
		{
			switch (@case)
			{
				case Case.Bool boolCase:
					if (!(Evaluate(boolCase.Pattern, bindings, Type.Bool) & Type.Bool))
						throw new Exception($"Case patterns is not a boolean at {boolCase.Pattern.Token.ErrorString}");
					if (!(Evaluate(boolCase.Value, bindings, hint) & hint))
						throw new Exception($"Expected case to be {hint} at {boolCase.Value.Token.ErrorString}");
					break;

				case Case.Otherwise otherwiseCase:
					if (Evaluate(otherwiseCase.Value, bindings, hint) & hint) break;
					throw new Exception($"Expected case to be {hint} at {otherwiseCase.Value.Token.ErrorString}");
			}
		}

		return hint;
		throw new Exception($"Type checking not implemented for cases at {cases.Token.ErrorString}");
	}

	public Type BinOp(Expression.BinaryOperator binOp, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		if (binOp.Type == TokenTypes.FuncType)
			return new Type.Function(Evaluate(binOp.Left, bindings, hint), Evaluate(binOp.Right, bindings, hint));
		if (binOp.Type == TokenTypes.MapsTo)
			switch (hint)
			{
				case null:
					if (binOp.Left is not Expression.Identifier)
						throw new Exception($"Functions require bindings at {binOp.Token.ErrorString}");
					return Evaluate(binOp.Right, bindings);
				case Type.Function function:
					var left = binOp.Left;
					if (left is not Expression.Identifier bind)
						throw new Exception($"Functions require bindings at {binOp.Token.ErrorString}");
					var newBindings = new Dictionary<IdValue, Type>(bindings)
					{
						[new IdValue(bind.Name)] = function.From
					};
					return Evaluate(binOp.Right, newBindings, function.To);
				default:
					throw new Exception($"Expected a function type but got {hint} instead at {binOp.Token.ErrorString}");
			}

		if (binOp.Type == TokenTypes.Equals)
		{
			var l = Evaluate(binOp.Left, bindings);
			var r = Evaluate(binOp.Right, bindings);
			return (l & r) switch
			{
				true => Type.Bool,
				false => throw new Exception($"Cannot compare values of different types {l}, {r} at {binOp.Token.ErrorString}")
			};
		}
		
		if (binOp.Type == TokenTypes.Times || binOp.Type == TokenTypes.Minus)
		{
			var l = Evaluate(binOp.Left, bindings);
			var r = Evaluate(binOp.Right, bindings);
			return (IsNumeric(l) && IsNumeric(r), l & r) switch
			{
				(true, true) => l,
				(true, false) => throw new Exception($"Cannot multiply values of different types {l}, {r} at {binOp.Token.ErrorString}"),
				(false, _) => throw new Exception($"Cannot multiply values of non-numeric types {l}, {r} at {binOp.Token.ErrorString}")
			};
		}

		throw new Exception($"Type checking not implemented for token at {binOp.Token.ErrorString}");
	}

	public Type Apply(Expression.Application app, Dictionary<IdValue, Type> bindings, Type? hint = null)
	{
		var fun = Evaluate(app.Function, bindings);
		var arg = Evaluate(app.Argument, bindings);
		return fun switch
		{
			Type.Function f when f.From & arg => f.To,
			Type.Function f => throw new Exception($"Type {f} takes a {f.From}, but got a {arg} at {app.Token.ErrorString}"),
			_ => throw new Exception($"Type {fun} is not a function at {app.Token.ErrorString}")
		};
	}

	private Type Id(Expression.Identifier identifier, Dictionary<IdValue, Type> bindings)
	{
		switch (identifier)
		{
			case { Name: "i32" }: return new Type.Builtin(Builtins.I32);
			case { Name: "proc" }: return new Type.Builtin(Builtins.Proc);
			default:
				var maybe = bindings.GetValueOrDefault(new IdValue(identifier.Name));
				if (maybe == null) throw new Exception($"No identifier found for {identifier.Name} at {identifier.Token.ErrorString}");
				return maybe;
		}
	}
	
	private bool IsNumeric(Type type) => type & new Type.IntLiteral(0);
}