using MeowC.Parser.Rules;

namespace MeowC.Parser;

public class Parser(List<Token> tokens)
{
	private List<Token> Tokens { get; } = tokens;
	public int CurrentIndex { get; set; } = 0;
	public List<Definition> Definitions { get; } = [];

	public void Parse()
	{
		while (Peek.Type != TokenTypes.EndOfFile)
			Definitions.Add(ParseDefinition());
	}

	internal Definition ParseDefinition() => Rules.Rules.Definition.Parse(this);

	internal Expression ParseExpression()
	{
		// TODO: replace with pratt parser
		var num = Peek.Data;
		Consume(TokenTypes.Number);
		return new Expression();
	}

	internal string Identifier()
	{
		var id = Peek.Data;
		Consume(TokenTypes.Identifier);
		return id;
	}

	internal Token Peek => Tokens[CurrentIndex];

	internal void Consume(TokenType expected) => Consume(expected, "");

	internal void Consume(TokenType expected, string data)
	{
		if (Peek.Type != expected || (data != "" && Peek.Data != data))
			throw new WrongTokenException(expected, Peek.Type);
		// Program.Error(Peek.Line, Peek.Col, $"Expected {expected}, but got {Peek.Data}");
		this.CurrentIndex++;
	}
}