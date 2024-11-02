namespace MeowC.Parser.Matches;

public abstract record Expression
{
	public record Identifier(string Name) : Expression;

	public record Number(long Value) : Expression;
	
	public record Prefix(TokenType Type, Expression Expression) : Expression;

	public record BinaryOperator(TokenType Type, Expression Left, Expression Right) : Expression;

	public record Procedure(List<Statement> Statements) : Expression
	{
		public override string ToString() => $"Procedure: \n{string.Join('\n', Statements)}";
	}
}