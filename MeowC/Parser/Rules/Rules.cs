namespace MeowC.Parser.Rules;

public static class Rules
{
	public static readonly DefinitionRule Definition = new();
	public static readonly IdentifierExpressionRule IdentifierExpression = new();
	public static readonly NumberExpressionRule NumberExpression = new();
	public static readonly CharExpressionRule CharExpression = new();
	public static readonly PrefixOperatorExpressionRule PrefixOperatorExpression = new(Priorities.Prefix);
	public static readonly ParensExpressionRule ParensExpression = new();

	public static readonly BinaryOperatorExpressionRule LeftSumOperatorExpression = new(Priorities.Sum, true);

	public static readonly BinaryOperatorExpressionRule FunctionFormationExpression = new(Priorities.FunctionFormation, false);

	public static readonly IReadOnlyDictionary<TokenType, PrefixExpressionRule> Prefixes = new Dictionary<TokenType, PrefixExpressionRule>
	{
		{ TokenTypes.Identifier, IdentifierExpression },
		{ TokenTypes.Number, NumberExpression },
		{ TokenTypes.Char, CharExpression },
		{ TokenTypes.Minus, PrefixOperatorExpression },
		{ TokenTypes.LParen, ParensExpression },
	};

	public static readonly IReadOnlyDictionary<TokenType, InfixExpressionRule> Infixes = new Dictionary<TokenType, InfixExpressionRule>
	{
		{ TokenTypes.Plus, LeftSumOperatorExpression },
		{ TokenTypes.Minus, LeftSumOperatorExpression },
		{ TokenTypes.MapsTo, FunctionFormationExpression },
	};
}