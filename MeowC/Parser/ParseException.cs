namespace MeowC.Parser;

public class ParseException(Token badToken) : Exception($"Could not parse {badToken}");