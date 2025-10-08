using MeowC.Parser;

namespace MeowC.Diagnostics;

public record Diagnostic(
	DiagLevel Level,
	DiagPhase Phase,
	int Code,
	Token? Token,
	string File,
	int Line,
	int Column,
	int Span,
	string Message)
{
	private static readonly Dictionary<(DiagPhase, int), string> DiagnosticNames = new()
	{
		{ (DiagPhase.Lexer, 1), "Unmatched single quote" },
		{ (DiagPhase.Lexer, 2), "Unmatched single quote" },
		{ (DiagPhase.Lexer, 3), "Unknown character" },
		{ (DiagPhase.Lexer, 4), "Unexpected end of file" },
		{ (DiagPhase.Parser, 101), "Unexpected token" },
		{ (DiagPhase.TypeChecker, 200), "Unknown identifier" },
		{ (DiagPhase.TypeChecker, 201), "Type mismatch" },
		{ (DiagPhase.TypeChecker, 202), "Could not unify types" },
		{ (DiagPhase.TypeChecker, 203), "Invalid function parameter" },
		{ (DiagPhase.TypeChecker, 204), "Not a type" },
		{ (DiagPhase.TypeChecker, 205), "Not an identifier" },
		{ (DiagPhase.TypeChecker, 206), "No property" },
		{ (DiagPhase.TypeChecker, 210), "Polymorphic constraint not satisfied" },
		{ (DiagPhase.TypeChecker, 211), "Polymorphic binding error" },
		{ (DiagPhase.TypeChecker, 220), "Inconsistent return types" },
		{ (DiagPhase.TypeChecker, 221), "Return type does not match declared type" },
	};

	public static Diagnostic SymbolError(CompilationUnit compilationUnit, int code, int line, int column, string message) =>
		new(DiagLevel.Error, DiagPhase.Lexer, code, null, compilationUnit.FileName, line, column, 1, message);

	public static Diagnostic WrongTokenError(CompilationUnit compilationUnit, WrongTokenException exception) =>
		new(DiagLevel.Error, DiagPhase.Parser, 101, exception.ActualToken, compilationUnit.FileName, exception.Line, exception.Col,
			exception.ActualToken.Data.Length, exception.Message);

	public static Diagnostic TypecheckError(CompilationUnit compilationUnit, int code, Token token, string message) =>
		new(DiagLevel.Error, DiagPhase.TypeChecker, code, token, compilationUnit.FileName, token.Line, token.Col, token.Data.Length,
			message);

	public static string GetDiagnosticName(DiagPhase phase, int code) => DiagnosticNames.GetValueOrDefault((phase, code), "unknown");
}