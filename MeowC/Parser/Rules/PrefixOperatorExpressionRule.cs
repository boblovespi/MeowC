using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class PrefixOperatorExpressionRule(Priorities priority) : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token) => new Expression.Prefix(token, token.Type, parser.ParseExpression(Priority));
	public override Priorities Priority => priority;
}