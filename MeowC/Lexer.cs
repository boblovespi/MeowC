using MeowC.Diagnostics;

namespace MeowC;

public class Lexer(CompilationUnit unit)
{
	private const string GreedySymbols = ":;=-,.+-*/|()[]{}<>";
	private int Current { get; set; }
	private int ColNum { get; set; }
	private int LineNum { get; set; }
	private string Lines { get; } = unit.Code;
	public List<Token> Tokens { get; } = [];

	/// <summary>
	/// Parse and build the token list for the given file.
	/// </summary>
	public void Parse()
	{
		LineNum = 1;
		ColNum = 1;
		Current = 0;
		while (Current < Lines.Length)
		{
			// Comments
			if (Peek == '/' && PeekAhead(1) == '/')
				while (Peek != '\n' && NotEOF)
					Advance();
			else if (Peek == '/' && PeekAhead(1) == '*')
			{
				while (!(Peek == '*' && PeekAhead(1) == '/') && NotEOF) Advance();
				// clean up the dangling '*/'
				Advance();
				Advance();
			}
			else if (Peek == '\\')
			{
				EatMacro();
			}
			else if (char.IsWhiteSpace(Peek))
				// NO OP: whitespace
				Advance();
			else if (GreedySymbols.Contains(Peek))
				EatSymbol();
			else if (char.IsDigit(Peek))
				EatNumber();
			else if (Peek == '\'')
				EatChar();
			else if (Peek == '"')
				EatString();
			else if (char.IsLetter(Peek))
				EatLiteral();
			else
			{
				// Program.Error(LineNum, ColNum, $"Unexpected character '{Peek}'.");
				unit.AddDiagnostic(
					Diagnostic.SymbolError(unit, 2, LineNum, ColNum + 1, $"Unexpected character '{Peek}'."));
				Advance();
			}
		}

		Tokens.Add(new Token(TokenTypes.EndOfFile, "", LineNum, ColNum));
	}

	private void EatLiteral()
	{
		var start = Current;
		while (char.IsLetter(Peek) || char.IsDigit(Peek)) Advance();
		var str = Lines.Substring(start, Current - start);
		Tokens.Add(
			Program.Keywords.Contains(str)
				? new Token(TokenTypes.Keyword, str, LineNum, ColNum)
				: new Token(TokenTypes.Identifier, str, LineNum, ColNum));
	}

	private void EatChar()
	{
		Advance();
		var start = Current;
		while (Peek != '\'' && Peek != '\n' && NotEOF) Advance();
		if (EndOfFile || Peek == '\n')
		{
			// Program.Error(LineNum, ColNum, $"Unexpected end of character literal '{Lines.Substring(start, Current - start)}'.");
			unit.AddDiagnostic(
				Diagnostic.SymbolError(unit, 1, LineNum, ColNum,
					$"Unexpected end of character literal '{Lines.Substring(start, Current - start).TrimEnd()}'."));
			return;
		}

		Tokens.Add(new Token(TokenTypes.Char, Lines.Substring(start, Current - start), LineNum, ColNum));
		Advance();
	}

	private void EatString()
	{
		Advance();
		var start = Current;
		while (Peek != '"' && NotEOF) Advance();
		if (EndOfFile)
		{
			unit.AddDiagnostic(
				Diagnostic.SymbolError(unit, 1, LineNum, ColNum,
					$"Unexpected end of string '{Lines.Substring(start, Current - start).TrimEnd()}'."));
			return;
		}

		Tokens.Add(new Token(TokenTypes.String, Lines.Substring(start, Current - start), LineNum, ColNum));
		Advance();
	}

	private void EatNumber()
	{
		var start = Current;
		while (char.IsDigit(Peek)) Advance();
		Tokens.Add(new Token(TokenTypes.Number, Lines.Substring(start, Current - start), LineNum, ColNum));
	}

	private void EatSymbol()
	{
		var start = Current;
		while (GreedySymbols.Contains(Peek)) Advance();
		GreedySymbol(Lines.Substring(start, Current - start), 0, Current - start);
	}

	private void EatMacro()
	{
		var start = Current;
		Advance();
		while (char.IsLetter(Peek) || char.IsDigit(Peek)) Advance();
		var str = Lines.Substring(start, Current - start);
		Tokens.Add(new Token(Program.TokenMap[str], "", LineNum, ColNum));
	}

	/// <summary>
	/// Match a sequence of characters greedily to the corresponding symbol tokens, recursively.
	/// </summary>
	/// <param name="symbols">The string of tokens to match.</param>
	/// <param name="start">Where in the string to start looking from.</param>
	/// <param name="end">Where in the string to end looking at.</param>
	private void GreedySymbol(string symbols, int start, int end)
	{
		while (true)
		{
			if (start >= end) return;
			if (Program.TokenMap.ContainsKey(symbols[start..end]))
			{
				Tokens.Add(new Token(Program.TokenMap[symbols[start..end]], "", LineNum, ColNum));
				start = end;
				end = symbols.Length;
			}
			else
				end -= 1;
		}
	}

	private char PeekAhead(int i) => i + Current < Lines.Length ? Lines[i + Current] : '\0';
	private char Peek => Current < Lines.Length ? Lines[Current] : '\0';

	private bool EndOfFile => !NotEOF;
	private bool NotEOF => Peek != '\0';

	private void Advance()
	{
		if (Peek == '\0') 
			// Program.Error(LineNum, ColNum, "Reached end of file unexpectedly.");
			unit.AddDiagnostic(
				Diagnostic.SymbolError(unit, 3, LineNum, ColNum, "Reached end of file unexpectedly."));
		if (Peek == '\n')
		{
			LineNum++;
			ColNum = 1;
		}
		else
			ColNum++;

		Current++;
	}
}