using System.Globalization;
using ConsoleAppFramework;
using MeowC.Diagnostics;
using MeowC.Generators;
using MeowC.Interpreter;

namespace MeowC;

public static class Program
{
	public static Dictionary<string, TokenType> TokenMap { get; } = new();
	public static ISet<string> Keywords { get; } = new HashSet<string>();

	public static void Main(string[] args)
	{
		using (var reader = new StreamReader("tokendef"))
		{
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine().Split(' ');
				var token = TokenTypes.Tokens[line[0]];
				for (var i = 1; i < line.Length; i++) TokenMap[line[i]] = token;
			}
		}

		using (var reader = new StreamReader("keyworddef"))
		{
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				Keywords.Add(line);
			}
		}

		var app = ConsoleApp.Create();
		app.Add("", Root);
		app.Add("typecheck", Typecheck);
		app.Run(args);
	}

	/// <summary>
	/// Compile a meow program.
	/// </summary>
	/// <param name="targetFileName">File to compile</param>
	/// <param name="output">-o, Output file (defaults to input file)</param>
	/// <param name="arch">Architecture to target</param>
	/// <param name="os">Operating system to target</param>
	public static void Root([Argument] string targetFileName, string output = "", string arch = "", string os = "")
	{
		Info($"Compiling file {targetFileName}");
		var compUnit = new CompilationUnit(targetFileName, output);
		var lexer = new Lexer(compUnit);
		lexer.Parse();
		// foreach (var token in lexer.Tokens) Console.WriteLine(token);
		var parser = new Parser.Parser(compUnit, lexer.Tokens);
		parser.Parse();
		//foreach (var def in parser.Definitions)
		//	Console.WriteLine($"defined {def.Id} to be a type {def.Type} with value {def.Val}");
		var typer = new TypeChecker(parser.Definitions);
		typer.Check();
		if (compUnit.Errored)
		{
			Console.WriteLine("something went wrong");
			return;
		}

		var interpreter = new Interpreter.Interpreter(parser.Definitions);
		interpreter.Run();
		// var outputter = new AMD64Gen(parser.Definitions);
		// using (var writer = new StreamWriter("./test.s"))
		// 	writer.Write(outputter.Output());

		var llvmGen = new LLVMGen(compUnit.OutputPath, parser.Definitions, typer.TypeTable);
		llvmGen.Compile();
	}

	/// <summary>
	/// Typecheck a meow program.
	/// </summary>
	/// <param name="targetFileName">File to typecheck</param>
	public static void Typecheck([Argument] string targetFileName)
	{
		Info($"Typechecking file {targetFileName}");
		var compUnit = new CompilationUnit(targetFileName);
		var lexer = new Lexer(compUnit);
		lexer.Parse();
		// foreach (var token in lexer.Tokens) Console.WriteLine(token);
		var parser = new Parser.Parser(compUnit, lexer.Tokens);
		parser.Parse();
		//foreach (var def in parser.Definitions)
		//	Console.WriteLine($"defined {def.Id} to be a type {def.Type} with value {def.Val}");
		var typer = new TypeChecker(parser.Definitions);
		typer.Check();
		PrintDiagnostics(compUnit);
	}

	private static void PrintDiagnostics(CompilationUnit compilationUnit)
	{
		foreach (var diagnostic in compilationUnit.Diagnostics)
		{
			DisplayDiagnostic(compilationUnit, diagnostic);
		}

		Console.ForegroundColor = ConsoleColor.White;
		Console.Error.WriteLine(compilationUnit.Errored ? "Something went wrong :(" : "Success");
		Console.Error.WriteLine($"Caught {compilationUnit.Diagnostics.Count} issue{(compilationUnit.Diagnostics.Count == 1 ? "" : "s")}");
		Console.ResetColor();
	}
	
	public static void Info(string message)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Error.WriteLine($"Info: {message}");
		Console.ResetColor();
	}

	[Obsolete("To be removed")]
	public static void Info(int line, int column, string message)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Error.WriteLine($"[{line}:{column}] Info: {message}");
		Console.ResetColor();
	}

	[Obsolete("To be removed")]
	public static void Error(int line, int column, string message)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.WriteLine($"[{line}:{column}] Error: {message}");
		Console.ForegroundColor = ConsoleColor.Gray;
		// Console.Error.WriteLine($"    {Lines[line - 1]}");
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.WriteLine($"    {new string(' ', column - 3)}^^^");
		Console.ResetColor();
		// Errored = true;
	}

	[Obsolete("To be removed")]
	public static void Error(int line, int column, string message, Token token)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.WriteLine($"[{line}:{column}] Error: {message}");
		Console.ForegroundColor = ConsoleColor.Gray;
		// Console.Error.WriteLine($" |   {Lines[line - 1]}");
		Console.Error.Write(" '-- ");
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.WriteLine($"{new string(' ', column - 1 - token.Data.Length)}{new string('^', token.Data.Length)}");
		Console.ResetColor();
		// Errored = true;
	}

	[Obsolete("To be removed")]
	public static void Error(CompileException exception)
	{
		if (exception is TokenException te)
			Error(exception.Line, exception.Col, exception.Message, te.At);
		else
			Error(exception.Line, exception.Col, exception.Message);
	}

	[Obsolete("To be removed")]
	public static void AddDiagnostic(Diagnostic diagnostic)
	{
		// Diagnostics.Add(diagnostic);
		// if (diagnostic.Level == DiagLevel.Error)
		// 	Errored = true;
	}

	public static void DisplayDiagnostic(CompilationUnit compilationUnit, Diagnostic diagnostic)
	{
		var primaryColor = diagnostic.Level switch
		{
			DiagLevel.Error => ConsoleColor.Red,
			DiagLevel.Warning => ConsoleColor.Yellow,
			DiagLevel.WeakWarning => ConsoleColor.Cyan,
			DiagLevel.Info => ConsoleColor.White
		};

		Console.ForegroundColor = primaryColor;
		var line = diagnostic.Line;
		var column = diagnostic.Column;
		var diagCode = diagnostic.Phase.ToString()[0] + diagnostic.Code.ToString("D3");
		var name = Diagnostic.GetDiagnosticName(diagnostic.Phase, diagnostic.Code);
		Console.Error.WriteLine($"[{diagnostic.File}:{line}:{column}] {ToTitleCase(diagnostic.Level.ToString())}: ({diagCode}) {name}");
		Console.ResetColor();
		Console.Error.WriteLine($" |    {compilationUnit.Lines[line - 1]}");
		Console.Error.Write(" |    ");
		Console.ForegroundColor = primaryColor;
		Console.Error.WriteLine($"{new string(' ', column - 1 - diagnostic.Span)}{new string('^', diagnostic.Span)}");
		Console.ResetColor();
		// Console.Error.Write(" '.__ ");
		Console.Error.WriteLine($" '.__ {diagnostic.Message}");
	}

	private static string ToTitleCase(string s) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
}