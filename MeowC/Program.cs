using System.Xml.Xsl;
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
		var targetFile = args.Length > 0 ? args[0] : "./test.meow";
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

		using (var reader = new StreamReader(targetFile))
		{
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
			if (!Errored)
			{
				var interpreter = new Interpreter.Interpreter(parser.Definitions);
				interpreter.Run();
			}
			// var outputter = new AMD64Gen(parser.Definitions);
			// using (var writer = new StreamWriter("./test.s"))
			// 	writer.Write(outputter.Output());
			
			var llvmGen = new LLVMGen(parser.Definitions, typer.TypeTable);
			llvmGen.Compile();
		}
	}

	public static void Error(int line, int column, string message)
	{
		Console.Error.WriteLine($"[{line}:{column}] Error: {message}");
		Errored = true;
	}

	public static void Error(CompileException exception)
	{
		Error(exception.Line, exception.Col, exception.Message);
	}
}