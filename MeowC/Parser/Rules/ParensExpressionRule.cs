using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class ParensExpressionRule : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token)
	{
		var exp = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.RParen);
		return exp;
	}

	public override Priorities Priority => Priorities.Prefix;
}