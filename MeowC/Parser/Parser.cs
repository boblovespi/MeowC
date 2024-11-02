using MeowC.Parser.Matches;
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

	internal Expression ParseExpression(Priorities priority)
	{
		var token = Peek;
		if (!Rules.Rules.Prefixes.TryGetValue(token.Type, out var prefixRule)) throw new ParseException(token);
		Consume(token.Type);
		var left = prefixRule.Parse(this, token);
		// Console.WriteLine($"infix peek: {Peek}");
		token = Peek;
		while (Rules.Rules.Infixes.TryGetValue(token.Type, out var infixRule) && priority < ExprPriority)
		{
			Consume(token.Type);
			left = infixRule.Parse(this, left, token);
		}

		return left;
	}

	public Priorities ExprPriority => Rules.Rules.Infixes.TryGetValue(Peek.Type, out var rule) ? rule.Priority : Priorities.No;

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
		CurrentIndex++;
	}
}