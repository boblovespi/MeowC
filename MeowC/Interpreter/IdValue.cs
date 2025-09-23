namespace MeowC.Interpreter;

public readonly record struct IdValue(string Name, bool Generated, bool Operator)
{
	public static implicit operator IdValue(string value) => new(value, false, false);

	public override string ToString() => (Generated, Operator) switch {
		(false, false) => Name,
		(true, false) => $"$g_{Name}",
		(true, true) => $"$op_{Name}",
		(false, true) => $"$g_op_{Name}",
	};
}