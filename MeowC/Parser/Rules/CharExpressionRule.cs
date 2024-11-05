using System.Text.RegularExpressions;
using MeowC.Parser.Matches;

namespace MeowC.Parser.Rules;

public class CharExpressionRule : PrefixExpressionRule
{
	public override Expression Parse(Parser parser, Token token) => new Expression.Number(token, Regex.Unescape(token.Data)[0]);
	public override Priorities Priority => Priorities.Const;
}