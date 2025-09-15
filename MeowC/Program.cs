using ConsoleAppFramework;
using MeowC.Generators;
using MeowC.Interpreter;

namespace MeowC;

public static class Program
{
	public static Dictionary<string, TokenType> TokenMap { get; } = new();
	public static ISet<string> Keywords { get; } = new HashSet<string>();
	private static bool Errored { get; set; } = false;

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
		var targetFile = new FileInfo(targetFileName);
		using var reader = targetFile.OpenText();
		var lines = reader.ReadToEnd();
		var lexer = new Lexer(lines);
		lexer.Parse();
		// foreach (var token in lexer.Tokens) Console.WriteLine(token);
		var parser = new Parser.Parser(lexer.Tokens);
		parser.Parse();
		//foreach (var def in parser.Definitions)
		//	Console.WriteLine($"defined {def.Id} to be a type {def.Type} with value {def.Val}");
		var typer = new TypeChecker(parser.Definitions);
		typer.Check();
		if (Errored)
		{
			Console.WriteLine("something went wrong");
			return;
		}

		var interpreter = new Interpreter.Interpreter(parser.Definitions);
		interpreter.Run();
		// var outputter = new AMD64Gen(parser.Definitions);
		// using (var writer = new StreamWriter("./test.s"))
		// 	writer.Write(outputter.Output());

		output = output == "" ? Path.GetFileNameWithoutExtension(targetFile.Name) : output;

		var llvmGen = new LLVMGen(output, parser.Definitions, typer.TypeTable);
		llvmGen.Compile();
	}

	public static void Info(string message)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Error.WriteLine($"Info: {message}");
		Console.ResetColor();
	}

	public static void Info(int line, int column, string message)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Error.WriteLine($"[{line}:{column}] Info: {message}");
		Console.ResetColor();
	}

	public static void Error(int line, int column, string message)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.WriteLine($"[{line}:{column}] Error: {message}");
		Console.ResetColor();
		Errored = true;
	}

	public static void Error(CompileException exception)
	{
		Error(exception.Line, exception.Col, exception.Message);
	}
}