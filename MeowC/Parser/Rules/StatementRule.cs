using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public abstract class StatementRule
{
	public abstract Statement Parse(Parser parser);
}