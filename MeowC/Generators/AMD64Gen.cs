using System.Text;
using MeowC.Parser.Matches;

namespace MeowC.Generators;

public class AMD64Gen(List<Definition> definitions)
{
	private StringBuilder Buffer { get; } = new();
	private List<Definition> Definitions { get; } = definitions;

	public string Output()
	{
		// EmitLine("extern putchar");
		EmitLine(".data");
		EmitLine(".text");
		EmitLine(".globl main");
		foreach (var definition in Definitions)
			if (definition is { Id: "main", Val: Expression.Procedure procedure }) OutputProcedure(procedure, Buffer);
		return Buffer.ToString();
	}

	private void OutputProcedure(Expression.Procedure procedure, StringBuilder buffer)
	{
		EmitLabel("main");
		EmitInstruction("push %rbp");
		EmitInstruction("movq %rsp, %rbp");
		EmitBlank();
		foreach (var statement in procedure.Statements) OutputStatement(statement, buffer);
		EmitBlank();
		EmitInstruction("movq $0, %rax");
		EmitInstruction("movq %rbp, %rsp");
		EmitInstruction("pop %rbp");
		EmitInstruction("ret");
	}

	private void OutputStatement(Statement statement, StringBuilder buffer)
	{
		switch (statement)
		{
			case Statement.Callable callable:
				if (callable.Routine == "print")
				{
					EmitInstruction($"movq {Evaluate(callable.Argument)}, %rdi");
					EmitInstruction($"call putchar");
				}
				break;
		}
	}

	private object Evaluate(Expression expression) =>
		expression switch
		{
			Expression.Number number => $"${number.Value}",
			_ => ""
		};

	private void EmitBlank() => Buffer.AppendLine();
	private void EmitLine(string line) => Buffer.AppendLine(line);
	private void EmitLabel(string label) => Buffer.AppendLine($"{label}:");
	private void EmitInstruction(string instruction) => Buffer.AppendLine($"\t{instruction}");
}