using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class CasesExpressionRule : PrefixExpressionRule
{
	public override Priorities Priority => Priorities.Conditional;
	public override Expression Parse(Parser parser, Token token)
	{
		// TODO: make last case end with period, rather than otherwise case
		var cases = new List<Case>();
		while (parser.Peek.Type != TokenTypes.RBrace) cases.Add(parser.ParseCase());
		parser.Consume(TokenTypes.RBrace);
		return new Expression.Case(token, cases);
	}
}