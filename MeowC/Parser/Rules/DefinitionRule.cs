using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class DefinitionRule
{
	public Definition Parse(Parser parser)
	{
		parser.Consume(TokenTypes.Keyword, "let");
		var id = parser.Identifier();
		parser.Consume(TokenTypes.TypeDef);
		var type = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Def);
		var val = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Semicolon);
		return new Definition(id, type, val);
	}
}