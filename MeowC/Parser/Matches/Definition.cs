namespace MeowC.Parser.Matches;

public class Definition(string id, string type, Expression val)
{
	public string Id { get; } = id;
	public string Type { get; } = type;
	public Expression Val { get; } = val;
}