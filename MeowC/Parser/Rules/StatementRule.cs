using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class StatementRule
{
	public Statement Parse(Parser parser)
	{
		var peek = parser.Peek;
		var procedure = peek.Data;
		parser.Consume(peek.Type);
		var expression = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Semicolon);
		return new Statement.Callable(procedure, expression);
	}
}