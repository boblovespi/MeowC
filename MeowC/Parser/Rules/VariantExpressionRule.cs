using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class VariantExpressionRule : PrefixExpressionRule
{
	public override Priorities Priority => Priorities.Conditional;

	public override Expression Parse(Parser parser, Token token)
	{
		parser.Consume(TokenTypes.LBrace);
		var definitions = new List<ObjectDefinition>();
		while (parser.Peek.Type != TokenTypes.RBrace) definitions.Add(parser.ParseObjectDefinition());
		parser.Consume(TokenTypes.RBrace);
		return new Expression.Record(token, definitions);
	}
}