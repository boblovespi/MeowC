using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public abstract class InfixExpressionRule : ExpressionRule
{
	public abstract Expression Parse(Parser parser, Expression left, Token token);
}