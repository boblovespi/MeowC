using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class CharExpressionRule : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token) => new Expression.Number(char.Parse(token.Data));
	public override Priorities Priority => Priorities.Const;
}