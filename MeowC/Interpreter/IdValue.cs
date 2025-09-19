namespace MeowC.Interpreter;

public record struct IdValue(string Name)
{
	public static implicit operator IdValue(string value) => new(value);
}