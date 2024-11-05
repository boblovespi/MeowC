﻿using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class ProcedureExpressionRule : PrefixExpressionRule
{
	public override Priorities Priority => Priorities.Conditional;
	public override Expression Parse(Parser parser, Token token)
	{
		parser.Consume(TokenTypes.RBrack);
		parser.Consume(TokenTypes.LBrace);
		var statements = new List<Statement>();
		while (parser.Peek.Type != TokenTypes.RBrace) statements.Add(parser.ParseStatement());
		parser.Consume(TokenTypes.RBrace);
		return new Expression.Procedure(token, statements);
	}
}