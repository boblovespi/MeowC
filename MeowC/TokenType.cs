namespace MeowC;

public class TokenType(string name)
{
	public string Name { get; } = name;

	public override string ToString()
	{
		return Name;
	}
}