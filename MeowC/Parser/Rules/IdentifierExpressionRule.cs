using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class IdentifierExpressionRule : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token) => new Expression.Identifier(token.Data);
}