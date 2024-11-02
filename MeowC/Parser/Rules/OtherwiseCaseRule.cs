using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class OtherwiseCaseRule : CaseRule
{
	public override Case Parse(Parser parser, Expression value, Token token)
	{
		parser.Consume(TokenTypes.Period);
		return new Case.Otherwise(value);
	}
}