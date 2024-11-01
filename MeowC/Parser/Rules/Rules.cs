namespace MeowC.Parser.Rules;

public static class Rules
{
	public static readonly DefinitionRule Definition = new();
	public static readonly IdentifierExpressionRule IdentifierExpression = new();
	public static readonly NumberExpressionRule NumberExpression = new();
	
	public static readonly Dictionary<TokenType, PrefixExpressionRule> Prefixes = new()
	{
		{ TokenTypes.Identifier, IdentifierExpression },
		{ TokenTypes.Number, NumberExpression }
	};
}