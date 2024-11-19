using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class ParensExpressionRule : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token)
	{
		// unit type
		if (parser.Peek.Type == TokenTypes.RParen)
			return new Expression.Unit(token);
		var exp = parser.ParseExpression(Priorities.No);
		// singleton
		if (parser.Peek.Type == TokenTypes.RParen)
		{
			parser.Consume(TokenTypes.RParen);
			return exp;
		}
		// tuple
		var exps = new List<Expression> { exp };
		while (parser.Peek.Type != TokenTypes.RParen)
		{
			parser.Consume(TokenTypes.Comma);
			exps.Add(parser.ParseExpression(Priorities.No));
		}
		parser.Consume(TokenTypes.RParen);
		return new Expression.Tuple(token, exps);
	}

	public override Priorities Priority => Priorities.Prefix;
}