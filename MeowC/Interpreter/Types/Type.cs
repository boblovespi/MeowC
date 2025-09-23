namespace MeowC.Interpreter.Types;

public abstract record Type
{
	public static readonly Enum Unit = new(1);
	public static readonly Enum Bool = new(2);
	public static readonly CString ConstString = new();
	public static readonly TypeUniverse Types = new(1);

	public record Enum(int Value) : Type;

	public record Builtin(Builtins Value) : Type;

	public record IntLiteral(long Value) : Type;

	public record Function(Type From, Type To) : Type;

	public record Sum(Type Left, Type Right) : Type;

	public record Product(Type Left, Type Right) : Type;

	public record CString : Type;

	public record TypeIdentifier(Type Type) : Type;

	public record TypeUniverse(int Level) : Type;

	public record Polymorphic(string From, Type TypeClass, Type To) : Type;

	public record Variable(string Name, Type TypeClass) : Type;

	public record Hole(string Name, Token Token) : Type;

	public static bool operator &(Type left, Type right)
	{
		if (left == right) return true;
		if (left is IntLiteral && right is IntLiteral) return true;
		if (left is TypeIdentifier lt && right is TypeIdentifier rt) return lt.Type & rt.Type;
		if (left is Product lp && right is Product rp) return lp.Left & rp.Left && lp.Right & rp.Right;
		if (left is Hole || right is Hole) return true;
		return left switch
		{
			IntLiteral { Value: <= sbyte.MaxValue and >= sbyte.MinValue } => right is Builtin
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
			Builtin { Value: Builtins.I8 } => right is IntLiteral { Value: <= sbyte.MaxValue and >= sbyte.MinValue },
			Builtin { Value: Builtins.I16 } => right is IntLiteral { Value: <= short.MaxValue and >= short.MinValue },
			Builtin { Value: Builtins.I32 } => right is IntLiteral { Value: <= int.MaxValue and >= int.MinValue },
			Builtin { Value: Builtins.I64 } => right is IntLiteral,
			Builtin { Value: Builtins.U8 } => right is IntLiteral { Value: <= byte.MaxValue and >= byte.MinValue },
			Builtin { Value: Builtins.U16 } => right is IntLiteral { Value: <= ushort.MaxValue and >= ushort.MinValue },
			Builtin { Value: Builtins.U32 } => right is IntLiteral { Value: <= uint.MaxValue and >= uint.MinValue },
			Builtin { Value: Builtins.U64 } => right is IntLiteral,
			_ => false
		};
	}

	public static bool operator <(Type left, Type right) => right is TypeUniverse && left switch
	{
		TypeIdentifier(var type) => type is not TypeUniverse,
		Variable(_, var typeClass) => typeClass == right,
		_ => false
	};

	public static bool operator >(Type left, Type right) => right < left;

	public sealed override string? ToString() => this switch
	{
		Builtin builtin => builtin.Value.ToString().ToLowerInvariant(),
		CString => "ConstString",
		Enum { Value: 1 } => "unit",
		Enum @enum => $"{@enum.Value}",
		Function function => $"{function.From} -> {function.To}",
		IntLiteral intLiteral => $"ConstInt[{intLiteral.Value}]",
		Polymorphic polymorphic => $"'{polymorphic.From} : {polymorphic.TypeClass} => {polymorphic.To}",
		Product product => $"{product.Left} * {product.Right}",
		Sum sum => $"{sum.Left} + {sum.Right}",
		TypeIdentifier typeIdentifier => $"val{typeIdentifier.Type}",
		TypeUniverse typeUniverse => $"Type {typeUniverse.Level}",
		Variable variable => $"'{variable.Name} : {variable.TypeClass}",
		Hole hole => $"Hole[{hole.Name}]",
		object t => "unknown!",
	};

	public bool IsStricterType(Type other) => (this, other) switch
	{
		(IntLiteral i1, IntLiteral i2) => Math.Abs(i1.Value) > Math.Abs(i2.Value),
		(Builtin, IntLiteral) => this & other,
		_ => false
	};
}