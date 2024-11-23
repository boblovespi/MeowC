using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class CallStatementRule : StatementRule
{
	public override Statement Parse(Parser parser)
	{
		var peek = parser.Peek;
		var routine = peek.Data;
		parser.Consume(peek.Type);
		var expression = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Semicolon);
		return new Statement.Callable(routine, expression);
	}
}