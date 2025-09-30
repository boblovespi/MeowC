using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class RecordExpressionRule : PrefixExpressionRule
{
	public override Priorities Priority => Priorities.Conditional;

	public override Expression.Record Parse(Parser parser, Token token)
	{
		// parser.Consume(TokenTypes.Keyword, "record");
		parser.Consume(TokenTypes.LBrace);
		var definitions = new List<ProcedureDefinition>();
		while (parser.Peek.Type != TokenTypes.RBrace) definitions.Add(parser.ParseProcedureDefinition());
		parser.Consume(TokenTypes.RBrace);
		return new Expression.Record(token, definitions);
	}
}