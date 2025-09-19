using JetBrains.Annotations;
using Xunit;

namespace MeowC.Tests;

[TestSubject(typeof(Lexer))]
public class LexerTest
{
	[Fact]
	public void LexerUnknownSymbol()
	{
		var code = "@";
		var unit = CompilationUnit.TestFromCode(code);
		var lexer = new Lexer(unit);
		lexer.Parse();
		Assert.Single(unit.Diagnostics);
		Assert.Equal(3, unit.Diagnostics[0].Code);
	}


	[Fact]
	public void LexerUnclosedSingleQuote()
	{
		var code = "'";
		var unit = CompilationUnit.TestFromCode(code);
		var lexer = new Lexer(unit);
		lexer.Parse();
		Assert.Single(unit.Diagnostics);
		Assert.Equal(1, unit.Diagnostics[0].Code);
	}

	[Fact]
	public void LexerUnclosedDoubleQuote()
	{
		var code = "\"";
		var unit = CompilationUnit.TestFromCode(code);
		var lexer = new Lexer(unit);
		lexer.Parse();
		Assert.Single(unit.Diagnostics);
		Assert.Equal(2, unit.Diagnostics[0].Code);
	}
}