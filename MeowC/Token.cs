using MeowC.Parser;

namespace MeowC;

public readonly struct Token(TokenType type, string data, int line, int col)
{
	public TokenType Type { get; } = type;
	public string Data { get; } = data;
	public int Line { get; } = line;
	public int Col { get; } = col;

	public override string ToString() => Data.Length == 0 ? Type.ToString() : $"{Type}[{Data}]";
}