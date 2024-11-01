namespace MeowC.Parser.Rules;

public class DefinitionRule
{
	public Definition Parse(Parser parser)
	{
		parser.Consume(TokenTypes.Keyword, "let");
		var id = parser.Identifier();
		parser.Consume(TokenTypes.TypeDef);
		var type = parser.Identifier();
		parser.Consume(TokenTypes.Def);
		var val = parser.ParseExpression();
		parser.Consume(TokenTypes.Semicolon);
		return new Definition(id, type, val);
	}
}