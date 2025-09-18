using MeowC.Parser;

namespace MeowC.Diagnostics;

public record Diagnostic(DiagLevel Level, DiagPhase Phase, int Code, Token? Token, string File, int Line, int Column, int Span, string Message)
{
	private static readonly Dictionary<(DiagPhase, int), string> DiagnosticNames = new()
	{
		{(DiagPhase.Lexer, 1), "Unmatched single quote"},
		{(DiagPhase.Lexer, 2), "Unmatched single quote"},
		{(DiagPhase.Lexer, 3), "Unknown character"},
		{(DiagPhase.Lexer, 4), "Unexpected end of file"},
		{(DiagPhase.Parser, 101), "Unexpected token"}
	};
	public static Diagnostic SymbolError(CompilationUnit compilationUnit, int code, int line, int column, string message) =>
		new(DiagLevel.Error, DiagPhase.Lexer, code, null, compilationUnit.FileName, line, column, 1, message);

	public static Diagnostic WrongTokenError(CompilationUnit compilationUnit, WrongTokenException exception) => 
		new(DiagLevel.Error, DiagPhase.Parser, 101, exception.ActualToken, compilationUnit.FileName, exception.Line, exception.Col, exception.ActualToken.Data.Length, exception.Message);
	
	public static string GetDiagnosticName(DiagPhase phase, int code) => DiagnosticNames.GetValueOrDefault((phase, code), "unknown");
}