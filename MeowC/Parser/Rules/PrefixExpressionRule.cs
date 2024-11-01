using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public abstract class PrefixExpressionRule : ExpressionRule
{
	public abstract Expression Parse(Parser parser, Token token);
}