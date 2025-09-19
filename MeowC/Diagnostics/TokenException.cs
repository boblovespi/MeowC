namespace MeowC.Diagnostics;

public class TokenException(int code, string message, Token at) : CompileException($"({at}) {message}")
{
    public override int Line => at.Line;
    public override int Col => at.Col;
    public Token At => at;
    public int Code => code;
}