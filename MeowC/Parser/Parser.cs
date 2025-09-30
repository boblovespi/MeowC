using MeowC.Diagnostics;
using MeowC.Parser.Matches;
using MeowC.Parser.Rules;

namespace MeowC.Parser;

public class Parser(CompilationUnit unit, List<Token> tokens)
{
	private List<Token> Tokens { get; } = tokens;
	public int CurrentIndex { get; set; } = 0;
	public List<Definition> Definitions { get; } = [];

	public void Parse()
	{
		while (Peek.Type != TokenTypes.EndOfFile)
			try
			{
				Definitions.Add(ParseDefinition());
			}
			catch (WrongTokenException wte)
			{
				unit.AddDiagnostic(Diagnostic.WrongTokenError(unit, wte));
				Consume(wte.Actual);
				while (Peek.Type != TokenTypes.EndOfFile && Peek.Type != TokenTypes.Keyword && Peek.Data != "let")
					Consume(Peek.Type);
			}
	}

	internal Definition ParseDefinition() => Rules.Rules.Definition.Parse(this);

	internal Statement ParseStatement()
	{
		var token = Peek;
		if (token.Type == TokenTypes.Keyword && token.Data == "return")
			return Rules.Rules.ReturnStatement.Parse(this);
		var second = Tokens[CurrentIndex + 1];
		if (second.Type == TokenTypes.Gets)
			return Rules.Rules.AssignStatement.Parse(this);
		return Rules.Rules.CallStatement.Parse(this);
	}

	internal Expression ParseExpression(Priorities priority)
	{
		var token = Peek;
		PrefixExpressionRule prefixRule;
		if (token.Type == TokenTypes.Keyword)
		{
			if (!Rules.Rules.KeywordPrefixes.TryGetValue(token.Data, out prefixRule!)) throw new ParseException(token);
		}
		else
			if (!Rules.Rules.Prefixes.TryGetValue(token.Type, out prefixRule!)) throw new ParseException(token);
		Consume(token.Type);
		var left = prefixRule.Parse(this, token);
		// Console.WriteLine($"infix peek: {Peek}");
		token = Peek;
		while (HasRule(priority, token, out var infixRule, out var applicationRule))
		{
			if (infixRule != null)
			{
				Consume(token.Type);
				left = infixRule.Parse(this, left, token);
			}
			else left = applicationRule!.Parse(this, left);
			token = Peek;
		}

		return left;
	}

	internal ProcedureDefinition ParseProcedureDefinition() => Rules.Rules.ProcedureDefinition.Parse(this);

	private bool HasRule(Priorities priority, Token token, out InfixExpressionRule? infixRule,
		out ApplicationExpressionRule? applicationRule)
	{
		applicationRule = null;
		var hasRealInfixRule = Rules.Rules.Infixes.TryGetValue(token.Type, out infixRule);
		if (hasRealInfixRule) return priority < infixRule!.Priority;
		infixRule = null;
		applicationRule = Rules.Rules.ApplicationExpression;
		return priority < applicationRule.Priority && Rules.Rules.Prefixes.ContainsKey(token.Type);
	}

	internal Case ParseCase()
	{
		var left = ParseExpression(Priorities.No);
		Consume(TokenTypes.Semicolon);
		var token = Peek;
		Consume(TokenTypes.Keyword);
		if (!Rules.Rules.CaseRules.TryGetValue(token.Data, out var rule)) throw new ParseException(token);
		return rule.Parse(this, left, token);
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
			throw new WrongTokenException(expected, Peek);
		// Program.Error(Peek.Line, Peek.Col, $"Expected {expected}, but got {Peek.Data}");
		CurrentIndex++;
	}
}