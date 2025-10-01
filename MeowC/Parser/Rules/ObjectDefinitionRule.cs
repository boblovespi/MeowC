using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class ObjectDefinitionRule
{
	public ObjectDefinition Parse(Parser parser)
	{
		var id = parser.Identifier();
		parser.Consume(TokenTypes.TypeDef);
		var type = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Semicolon);
		return new ObjectDefinition(id, type);
	}
}
