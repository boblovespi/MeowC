namespace MeowC.Parser.Matches;

public abstract record Expression(Token Token)
{
	public record Identifier(Token Token, string Name) : Expression(Token);

	public record Number(Token Token, long Value) : Expression(Token);

	public record Unit(Token Token) : Expression(Token);

	public record Tuple(Token Token, List<Expression> Values) : Expression(Token)
	{
		public override string ToString() => $"Tuple: (\n{string.Join(",\n\t", Values)}\n)";
	}

	public record String(Token Token, string Value) : Expression(Token);

	public record Prefix(Token Token, TokenType Type, Expression Expression) : Expression(Token);

	public record BinaryOperator(Token Token, TokenType Type, Expression Left, Expression Right) : Expression(Token);

	public record Procedure(Token Token, List<ProcedureDefinition> Definitions, List<Statement> Statements) : Expression(Token)
	{
		public override string ToString() => $"Procedure: {{\n{string.Join(";\n", Statements)}\n}}";
	}

	public record Case(Token Token, List<Matches.Case> Cases) : Expression(Token)
	{
		public override string ToString() => $"Cases: \n{string.Join('\n', Cases)}";
	}

	public record Application(Token Token, Expression Function, Expression Argument) : Expression(Token);
}