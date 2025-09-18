using MeowC;

public class TokenException(string message, Token at) : CompileException($"({at}) {message}")
{
    public override int Line => at.Line;
    public override int Col => at.Col;
    public Token At => at;
}