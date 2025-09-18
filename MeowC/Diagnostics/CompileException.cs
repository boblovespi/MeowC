namespace MeowC;

public abstract class CompileException(string message) : Exception(message)
{
    public abstract int Line { get; }
	public abstract int Col { get; }
}