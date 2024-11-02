using MeowC.Parser.Matches;

namespace MeowC.Interpreter;

public class Interpreter(List<Definition> definitions)
{
	private List<Definition> Definitions { get; } = definitions;

	public void Run()
	{
		foreach (var definition in Definitions)
			if (definition is { Id: "main", Val: Expression.Procedure procedure }) RunProcedure(procedure);
	}

	private void RunProcedure(Expression.Procedure procedure)
	{
		foreach (var statement in procedure.Statements) RunStatement(statement);
	}

	private void RunStatement(Statement statement)
	{
		switch (statement)
		{
			case Statement.Callable callable: 
				if (callable.Routine == "print")
					Console.Write(Evaluate(callable.Argument));
				break;
		}
	}

	private object Evaluate(Expression expression) =>
		expression switch
		{
			Expression.Number number => (char)number.Value,
			_ => ""
		};
}