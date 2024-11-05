using System.Text.RegularExpressions;
using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class StringExpressionRule : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token) => new Expression.String(token, Regex.Unescape(token.Data));
	public override Priorities Priority => Priorities.Const;
}