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

	public record Record(List<string> Names, List<Type> Fields) : Type;

	public record Variant(List<string> Names, List<Type> Entries) : Type;

	public static bool operator &(Type left, Type right)
	{
		if (left == right) return true;
		switch (left, right)
		{
			case (IntLiteral, IntLiteral): return true;
			case (TypeIdentifier lt, TypeIdentifier rt): return lt.Type & rt.Type;
			case (Product lp, Product rp): return lp.Left & rp.Left && lp.Right & rp.Right;
			case (Sum ls, Sum rs): return ls.Left & rs.Left && ls.Right & rs.Right;
			case (Function lf, Function rf): return lf.From & rf.From && lf.To & rf.To;
			case (Polymorphic lpy, Polymorphic rpy): return lpy.TypeClass & rpy.TypeClass && lpy.To & rpy.To;
			case (Record lr, Record rr):
				return lr.Fields.Zip(rr.Fields).All(tuple => tuple.First & tuple.Second) &&
				       lr.Names.Zip(rr.Names).All(tuple => tuple.First == tuple.Second);
			case (Variant lv, Variant rv):
				return lv.Entries.Zip(rv.Entries).All(tuple => tuple.First & tuple.Second) &&
				       lv.Names.Zip(rv.Names).All(tuple => tuple.Second == tuple.First);
		}

		if (left is TypeIdentifier && right == Types || left == Types && right is TypeIdentifier) return true;
		if (left is Hole || right is Hole) return true;
		var builtin = left;
		var il = right;
		if (right is Builtin)
		{
			builtin = right;
			il = left;
		}

		return builtin switch
		{
			Builtin { Value: Builtins.I8 } => il is IntLiteral { Value: <= sbyte.MaxValue and >= sbyte.MinValue },
			Builtin { Value: Builtins.I16 } => il is IntLiteral { Value: <= short.MaxValue and >= short.MinValue },
			Builtin { Value: Builtins.I32 } => il is IntLiteral { Value: <= int.MaxValue and >= int.MinValue },
			Builtin { Value: Builtins.I64 } => il is IntLiteral,
			Builtin { Value: Builtins.U8 } => il is IntLiteral { Value: <= byte.MaxValue and >= byte.MinValue },
			Builtin { Value: Builtins.U16 } => il is IntLiteral { Value: <= ushort.MaxValue and >= ushort.MinValue },
			Builtin { Value: Builtins.U32 } => il is IntLiteral { Value: <= uint.MaxValue and >= uint.MinValue },
			Builtin { Value: Builtins.U64 } => il is IntLiteral,
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
		Record record => $"{{{string.Join(" * ", record.Names.Zip(record.Fields).Select(n => $"({n.First}: {n.Second})"))}}}",
		Variant variant => $"{{{string.Join(" + ", variant.Names.Zip(variant.Entries).Select(n => $"({n.First}: {n.Second})"))}}}",
		object t => $"unknown {t.GetType()}!",
	};
	
	public string ToStringNoVal => this switch
	{
		Builtin builtin => builtin.Value.ToString().ToLowerInvariant(),
		CString => "ConstString",
		Enum { Value: 1 } => "unit",
		Enum { Value: 2 } => "boolean",
		Enum @enum => $"{@enum.Value}",
		Function function => $"{function.From} -> {function.To}",
		IntLiteral intLiteral => $"ConstInt[{intLiteral.Value}]",
		Polymorphic polymorphic => $"'{polymorphic.From} : {polymorphic.TypeClass} => {polymorphic.To}",
		Product product => $"{product.Left} * {product.Right}",
		Sum sum => $"{sum.Left} + {sum.Right}",
		TypeIdentifier typeIdentifier => typeIdentifier.Type.ToStringNoVal,
		TypeUniverse typeUniverse => $"Type {typeUniverse.Level}",
		Variable variable => $"'{variable.Name} : {variable.TypeClass}",
		Hole hole => $"Hole[{hole.Name}]",
		Record record => $"{{{string.Join(" * ", record.Names.Zip(record.Fields).Select(n => $"({n.First}: {n.Second})"))}}}",
		Variant variant => $"{{{string.Join(" + ", variant.Names.Zip(variant.Entries).Select(n => $"({n.First}: {n.Second})"))}}}",
		object t => $"unknown {t.GetType()}!",
	};

	public bool IsStricterType(Type other) => (this, other) switch
	{
		(IntLiteral i1, IntLiteral i2) => Math.Abs(i1.Value) > Math.Abs(i2.Value),
		(Builtin, IntLiteral) => this & other,
		_ => false
	};

	public Type GetStricterType(Type other) => (this, other) switch
	{
		(IntLiteral i1, IntLiteral i2) when Math.Abs(i1.Value) > Math.Abs(i2.Value) => i1.Value < 0 ? i1 :
			i2.Value < 0 ? new IntLiteral(-i1.Value) : i1,
		(IntLiteral i1, IntLiteral i2) => i1.Value < 0 ? i2.Value < 0 ? i2 : new IntLiteral(-i2.Value) : i2,
		(Builtin, IntLiteral) => this,
		_ => other
	};
}