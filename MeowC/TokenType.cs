namespace MeowC;

public record TokenType(string Name)
{
	public override string ToString()
	{
		return Name;
	}
}