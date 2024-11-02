using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public abstract class CaseRule
{
	public abstract Case Parse(Parser parser, Expression value, Token token);
}