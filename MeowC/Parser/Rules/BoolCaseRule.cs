using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class BoolCaseRule : CaseRule
{
	public override Case Parse(Parser parser, Expression value, Token token)
	{
		var @case = new Case.Bool(value, parser.ParseExpression(Priorities.No));
		// Console.WriteLine(@case);
		parser.Consume(TokenTypes.Comma);
		return @case;
	}
}