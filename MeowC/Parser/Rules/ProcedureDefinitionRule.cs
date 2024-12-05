using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class ProcedureDefinitionRule
{
	public ProcedureDefinition Parse(Parser parser)
	{
		var id = parser.Identifier();
		parser.Consume(TokenTypes.TypeDef);
		var type = parser.ParseExpression(Priorities.No);
		if (parser.Peek.Type == TokenTypes.Semicolon)
		{
			parser.Consume(TokenTypes.Semicolon);
			return new ProcedureDefinition(id, type, null);
		}
		parser.Consume(TokenTypes.Def);
		var val = parser.ParseExpression(Priorities.No);
		parser.Consume(TokenTypes.Semicolon);
		return new ProcedureDefinition(id, type, val);
	}
}