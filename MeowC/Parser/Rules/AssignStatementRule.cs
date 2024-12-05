using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class AssignStatementRule : StatementRule
{
	public override Statement Parse(Parser parser)
	{
		var peek = parser.Peek;
		var variable = peek.Data;
		parser.Consume(peek.Type);
		parser.Consume(TokenTypes.Gets);
		var expression = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Semicolon);
		return new Statement.Assignment(variable, expression);
	}
}