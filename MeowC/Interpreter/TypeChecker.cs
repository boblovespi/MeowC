using MeowC.Interpreter.Types;
using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Interpreter;

public class TypeChecker(List<Definition> definitions)
{
	private List<Definition> Definitions { get; } = definitions;
	private TypeEvaluator Evaluator { get; } = new();
	private Dictionary<IdValue, Type> GlobalBindings { get; } = new();

	public void Check()
	{
		foreach (var definition in Definitions)
		{
			var type = Evaluator.Evaluate(definition.Type, new Dictionary<IdValue, Type>(GlobalBindings));
			if (type is Type.TypeIdentifier typeIdentifier)
				GlobalBindings[new IdValue(definition.Id)] = typeIdentifier.Type;
			else
				throw new Exception($"Type {type} for definition {definition.Id} ought to be a type identifier");
		}

		foreach (var definition in Definitions)
		{
			Evaluator.Evaluate(definition.Val, new Dictionary<IdValue, Type>(GlobalBindings), GlobalBindings[new IdValue(definition.Id)]);
			if (definition is { Val: Expression.Procedure procedure })
				CheckProcedure(procedure);
			Console.WriteLine(GlobalBindings[new IdValue(definition.Id)]);
		}
	}

	private void CheckProcedure(Expression.Procedure procedure)
	{
		foreach (var statement in procedure.Statements) CheckStatement(statement);
	}

	private void CheckStatement(Statement statement)
	{
		switch (statement)
		{
			case Statement.Callable callable:
				if (callable.Routine == "print")
				{
					var value = Evaluator.Evaluate(callable.Argument, new Dictionary<IdValue, Type>(GlobalBindings));
				}

				break;
		}
	}
}