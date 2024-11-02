using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class NumberExpressionRule : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token) => new Expression.Number(long.Parse(token.Data));
	public override Priorities Priority => Priorities.Const;
}