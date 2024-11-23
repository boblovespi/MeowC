using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class ReturnStatementRule : StatementRule
{
	public override Statement Parse(Parser parser)
	{
		parser.Consume(TokenTypes.Keyword, "return");
		var expression = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Semicolon);
		return new Statement.Return(expression);
	}
}