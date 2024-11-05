using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class BinaryOperatorExpressionRule(Priorities priority, bool leftBinding) : InfixExpressionRule
{
	public override Expression Parse(Parser parser, Expression left, Token token) =>
		new Expression.BinaryOperator(token, token.Type, left, parser.ParseExpression(leftBinding ? Priority : Priority - 1));

	public override Priorities Priority => priority;
}