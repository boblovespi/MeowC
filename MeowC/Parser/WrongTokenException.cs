namespace MeowC.Parser;

internal class WrongTokenException(TokenType expected, Token actual) : CompileException($"Expected {expected}; but got {actual}")
{
	public TokenType Expected { get; } = expected;
	public TokenType Actual { get; } = actual.Type;

    public override int Line => actual.Line;
    public override int Col => actual.Col;
}