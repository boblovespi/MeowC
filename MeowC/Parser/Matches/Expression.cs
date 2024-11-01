namespace MeowC.Parser.Matches;

public record Expression
{
	public record Identifier(string Name) : Expression;

	public record Number(long Value) : Expression;
}