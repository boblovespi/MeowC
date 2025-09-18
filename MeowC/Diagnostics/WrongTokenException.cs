namespace MeowC.Parser;

public class WrongTokenException(TokenType expected, Token actual) : CompileException($"Expected {expected}; but got {actual}")
{
	public TokenType Expected => expected;
	public TokenType Actual => actual.Type;
	public Token ActualToken => actual;

    public override int Line => actual.Line;
    public override int Col => actual.Col;
}