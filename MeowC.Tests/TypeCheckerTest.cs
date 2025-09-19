using System;
using JetBrains.Annotations;
using MeowC.Interpreter;
using Xunit;

namespace MeowC.Tests;

[TestSubject(typeof(TypeChecker))]
public class TypeCheckerTest(TypeCheckerTest.Fixture fixture) : IClassFixture<TypeCheckerTest.Fixture>
{
	[Fact]
	public void TypeCheckerUnknownIdentifier()
	{
		var code = "let x: i32 := undefinedVar;";
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Contains(unit.Diagnostics, d => d.Code == 200);
		Assert.Contains(unit.Diagnostics, d => d.Message.Contains("undefinedVar"));
	}

	[Fact]
	public void TypeCheckerTypeMismatchInDefinition()
	{
		var code = "let x: i32 := \"hello\";";
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Contains(unit.Diagnostics, d => d.Code == 201);
		Assert.Contains(unit.Diagnostics, d => d.Message.Contains("Expected type"));
	}

	[Fact]
	public void TypeCheckerInvalidFunctionParameter()
	{
		var code = "let f: (i32, i32) -> i32 := (x, 42) |-> x + 10;";
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Contains(unit.Diagnostics, d => d.Code == 203);
	}

	[Fact]
	public void TypeCheckerPolymorphicConstraintNotSatisfied()
	{
		var code = """
		           let id: T => T -> T := T |=> x |-> x;
		           let result: i32 := id "hello" + 42;
		           """;
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Contains(unit.Diagnostics, d => d.Code == 210);
	}

	[Fact]
	public void TypeCheckerPolymorphicBindingError()
	{
		var code = "let f: T => T -> T := 42 |=> x |-> x;";
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Contains(unit.Diagnostics, d => d.Code == 211);
	}

	[Fact]
	public void TypeCheckerInconsistentReturnTypes()
	{
		var code = """
		           let f: i32 -> i32 := x \mt []{
		               return 42;
		               return "hello";
		           };
		           """;
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Contains(unit.Diagnostics, d => d.Code == 220);
	}

	[Fact]
	public void TypeCheckerReturnTypeMismatch()
	{
		var code = """
		           let f: i32 -> i32 := x \mt [] {
		               return "hello";
		           };
		           """;
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Contains(unit.Diagnostics, d => d.Code == 221);
	}

	[Fact]
	public void TypeCheckerReportsMultipleErrors()
	{
		var code = """
		           let x: i32 := "hello";
		           let y: i32 := undefinedVar;
		           let f: i32 -> i32 := x \mt [] {
		               return "world";
		           };
		           """;
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.InRange(unit.Diagnostics.Count, 3, 1000);
		Assert.True(unit.Errored);
	}

	[Fact]
	public void TypeCheckerSuccess()
	{
		var code = """
		           let x: i32 := 42;
		           let y: i32 := x + 10;
		           let f: i32 -> i32 := x \mt x * 2;
		           """;
		var unit = CompilationUnit.TestFromCode(code);
		RunFullCompilation(unit);

		Assert.Empty(unit.Diagnostics);
		Assert.False(unit.Errored);
	}

	private static void RunFullCompilation(CompilationUnit unit)
	{
		var lexer = new Lexer(unit);
		lexer.Parse();

		var parser = new Parser.Parser(unit, lexer.Tokens);
		parser.Parse();

		var typeChecker = new TypeChecker(unit, parser.Definitions);
		typeChecker.Check();
	}

	public class Fixture : IDisposable
	{
		public Fixture()
		{
			Program.Bootstrap("../../../../MeowC/");
		}

		public void Dispose()
		{
		}
	}
}