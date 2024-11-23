using MeowC.Parser.Matches;

namespace MeowC.Interpreter;

public class Interpreter(List<Definition> definitions)
{
	private List<Definition> Definitions { get; } = definitions;
	private RuntimeEvaluator Evaluator { get; } = new(definitions);

	public void Run()
	{
		foreach (var definition in Definitions)
			if (definition is { Id: "main", Val: Expression.Procedure procedure })
				RunProcedure(procedure);
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
				{
					var value = Evaluator.Evaluate(callable.Argument, new());
					switch (value)
					{
						case long l and <= byte.MaxValue and >= byte.MinValue:
							Console.Write((char)l);
							break;
						case long l:
							Console.Write(l);
							break;
						default:
							Console.Write(value.ToString());
							break;
					}
				}
				break;
			case Statement.Return @return:
				return;
		}
	}
}