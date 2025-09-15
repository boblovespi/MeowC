namespace MeowC;

public static class TokenTypes
{
	public static Dictionary<string, TokenType> Tokens { get; } = new();

	// keywords
	public static readonly TokenType Keyword = MakeToken("KEYWORD");

	// literals
	public static readonly TokenType Identifier = MakeToken("IDENTIFIER");
	public static readonly TokenType Type = MakeToken("TYPE");
	public static readonly TokenType Number = MakeToken("NUMBER");
	public static readonly TokenType Char = MakeToken("CHAR");
	public static readonly TokenType String = MakeToken("STRING");

	// symbols
	public static readonly TokenType TypeDef = MakeToken("TYPEDEF");
	public static readonly TokenType FuncType = MakeToken("FUNCTYPE");
	public static readonly TokenType Def = MakeToken("DEF");
	public static readonly TokenType MapsTo = MakeToken("MAPSTO");
	public static readonly TokenType Gets = MakeToken("GETS");
	public static readonly TokenType LBrack = MakeToken("LBRACK");
	public static readonly TokenType RBrack = MakeToken("RBRACK");
	public static readonly TokenType LParen = MakeToken("LPAREN");
	public static readonly TokenType RParen = MakeToken("RPAREN");
	public static readonly TokenType LBrace = MakeToken("LBRACE");
	public static readonly TokenType RBrace = MakeToken("RBRACE");
	public static readonly TokenType Semicolon = MakeToken("SEMICOLON");
	public static readonly TokenType Comma = MakeToken("COMMA");
	public static readonly TokenType Period = MakeToken("PERIOD");
	public static readonly TokenType Minus = MakeToken("MINUS");
	public static readonly TokenType Plus = MakeToken("PLUS");
	public static readonly TokenType Times = MakeToken("TIMES");
	public static readonly TokenType Slash = MakeToken("SLASH");
	public new static readonly TokenType Equals = MakeToken("EQUALS");
	public static readonly TokenType Less = MakeToken("LESS");
	public static readonly TokenType DoubleTo = MakeToken("DOUBLETO");
	public static readonly TokenType DoubleMapsTo = MakeToken("DOUBLEMAPSTO");
	public static readonly TokenType EndOfFile = MakeToken("EOF");

	private static TokenType MakeToken(string name)
	{
		var token = new TokenType(name);
		Tokens[name] = token;
		return token;
	}
}