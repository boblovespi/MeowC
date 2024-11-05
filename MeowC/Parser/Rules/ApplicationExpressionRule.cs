using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class ApplicationExpressionRule : ExpressionRule
{
	public override Priorities Priority => Priorities.Application;

	public Expression Parse(Parser parser, Expression left) =>
		new Expression.Application(left.Token, left, parser.ParseExpression(Priorities.Application - 1));
}