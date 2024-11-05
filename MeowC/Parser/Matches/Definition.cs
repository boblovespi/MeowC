namespace MeowC.Parser.Matches;

public class Definition(string id, Expression type, Expression val)
{
	public string Id { get; } = id;
	public Expression Type { get; } = type;
	public Expression Val { get; } = val;
}