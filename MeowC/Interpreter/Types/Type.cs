﻿namespace MeowC.Interpreter.Types;

public abstract record Type
{
	public static readonly Enum Unit = new(1);
	public static readonly Enum Bool = new(2);
	public static readonly CString ConstString = new();

	public record Enum(int Value) : Type;

	public record Builtin(Builtins Value) : Type;
	
	public record IntLiteral(long Value) : Type;
	
	public record Function(Type From, Type To) : Type;

	public record CString : Type;

	public static bool operator &(Type left, Type right)
	{
		if (left == right) return true;
		if (left is IntLiteral && right is IntLiteral) return true;
		return left switch
		{
			IntLiteral { Value: <= byte.MaxValue and >= byte.MinValue } => right is Builtin
			{
				Value: >= Builtins.I8 and <= Builtins.I64
			},
			IntLiteral { Value: <= short.MaxValue and >= short.MinValue } => right is Builtin
			{
				Value: >= Builtins.I16 and <= Builtins.I64
			},
			IntLiteral { Value: <= int.MaxValue and >= int.MinValue } => right is Builtin
			{
				Value: >= Builtins.I32 and <= Builtins.I64
			},
			IntLiteral => right is Builtin { Value: Builtins.I64 },
			Builtin { Value: Builtins.I8 } => right is IntLiteral { Value: <= byte.MaxValue and >= byte.MinValue },
			Builtin { Value: Builtins.I16 } => right is IntLiteral { Value: <= short.MaxValue and >= short.MinValue },
			Builtin { Value: Builtins.I32 } => right is IntLiteral { Value: <= int.MaxValue and >= int.MinValue },
			Builtin { Value: Builtins.I64 } => right is IntLiteral,
			_ => false
		};
	}
}