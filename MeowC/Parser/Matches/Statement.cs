namespace MeowC.Parser.Matches;

public abstract record Statement
{
	public record Callable(string Routine, Expression Argument) : Statement;
}