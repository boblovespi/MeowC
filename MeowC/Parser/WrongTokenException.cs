namespace MeowC.Parser;

internal class WrongTokenException(TokenType expected, TokenType actual) : Exception($"Expected {expected}; but got {actual}")
{
	public TokenType Expected { get; } = expected;
	public TokenType Actual { get; } = actual;
}