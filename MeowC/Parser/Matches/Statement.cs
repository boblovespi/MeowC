namespace MeowC.Parser.Matches;

public abstract record Statement
{
	public record Callable(string Routine, Expression Argument) : Statement;
	public record Return(Expression Argument) : Statement;
	public record Assignment(string Variable, Expression Value) : Statement;
}