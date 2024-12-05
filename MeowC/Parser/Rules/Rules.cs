namespace MeowC.Parser.Rules;

public static class Rules
{
	public static readonly DefinitionRule Definition = new();

	public static readonly CallStatementRule CallStatement = new();
	public static readonly ReturnStatementRule ReturnStatement = new();
	public static readonly AssignStatementRule AssignStatement = new();

	public static readonly ProcedureDefinitionRule ProcedureDefinition = new();

	public static readonly IdentifierExpressionRule IdentifierExpression = new();
	public static readonly NumberExpressionRule NumberExpression = new();
	public static readonly CharExpressionRule CharExpression = new();
	public static readonly StringExpressionRule StringExpression = new();
	public static readonly PrefixOperatorExpressionRule PrefixOperatorExpression = new(Priorities.Prefix);
	public static readonly ParensExpressionRule ParensExpression = new();
	public static readonly ProcedureExpressionRule ProcedureExpression = new();
	public static readonly CasesExpressionRule CasesExpression = new();

	
	public static readonly BinaryOperatorExpressionRule FunctionDefinitionExpression = new(Priorities.FunctionFormation, false);
	
	public static readonly BinaryOperatorExpressionRule LeftSumOperatorExpression = new(Priorities.Sum, true);
	public static readonly BinaryOperatorExpressionRule LeftProductOperatorExpression = new(Priorities.Product, true);

	public static readonly BinaryOperatorExpressionRule EqualsOperatorExpression = new(Priorities.Conditional, false);
	public static readonly BinaryOperatorExpressionRule FunctionFormationExpression = new(Priorities.FunctionFormation, false);

	public static readonly ApplicationExpressionRule ApplicationExpression = new();

	public static readonly BoolCaseRule BoolCase = new();
	public static readonly OtherwiseCaseRule OtherwiseCase = new();

	public static readonly IReadOnlyDictionary<TokenType, PrefixExpressionRule> Prefixes = new Dictionary<TokenType, PrefixExpressionRule>
	{
		{ TokenTypes.Identifier, IdentifierExpression },
		{ TokenTypes.Number, NumberExpression },
		{ TokenTypes.Char, CharExpression },
		{ TokenTypes.String, StringExpression },
		{ TokenTypes.Minus, PrefixOperatorExpression },
		{ TokenTypes.LParen, ParensExpression },
		{ TokenTypes.LBrack, ProcedureExpression },
		{ TokenTypes.LBrace, CasesExpression },
	};

	public static readonly IReadOnlyDictionary<TokenType, InfixExpressionRule> Infixes = new Dictionary<TokenType, InfixExpressionRule>
	{
		{ TokenTypes.Plus, LeftSumOperatorExpression },
		{ TokenTypes.Minus, LeftSumOperatorExpression },
		{ TokenTypes.Times, LeftProductOperatorExpression },
		{ TokenTypes.Slash, LeftProductOperatorExpression },
		{ TokenTypes.Equals, EqualsOperatorExpression },
		{ TokenTypes.MapsTo, FunctionFormationExpression },
		{ TokenTypes.FuncType, FunctionDefinitionExpression },
	};

	public static readonly IReadOnlyDictionary<string, CaseRule> CaseRules = new Dictionary<string, CaseRule>
	{
		{ "if", BoolCase },
		{ "otherwise", OtherwiseCase },
	};
}