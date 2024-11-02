namespace MeowC.Parser.Matches;

public abstract record Case(Expression Value)
{
	public record Bool(Expression Value, Expression Pattern) : Case(Value);
	
	public record Otherwise(Expression Value): Case(Value);
}