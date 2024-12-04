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
			try
			{
				var type = Evaluator.Evaluate(definition.Type, new Dictionary<IdValue, Type>(GlobalBindings));
				GlobalBindings[new IdValue(definition.Id)] = type switch
				{
					Type.TypeIdentifier => FixTypes(type),
					Type.IntLiteral { Value: <= int.MaxValue and >= 1 } intLiteral => new Type.Enum((int)intLiteral.Value),
					_ => throw new TokenException($"Type {type} for definition {definition.Id} ought to be a type identifier", definition.Val.Token)
				};
			}
			catch (CompileException e)
			{
				Program.Error(e);
			}
		}

		foreach (var definition in Definitions)
		{
			try
			{
				Evaluator.Evaluate(definition.Val, new Dictionary<IdValue, Type>(GlobalBindings), GlobalBindings[new IdValue(definition.Id)]);
			}
			catch (CompileException e)
			{
				Program.Error(e);
			}
			if (definition is { Val: Expression.Procedure procedure })
				CheckProcedure(procedure);
			//Console.WriteLine(GlobalBindings[new IdValue(definition.Id)]);
		}
	}

	private static Type FixTypes(Type type) =>
		type switch
		{
			Type.Function function => new Type.Function(FixTypes(function.From), FixTypes(function.To)),
			Type.Product product => new Type.Product(FixTypes(product.Left), FixTypes(product.Right)),
			Type.Sum sum => new Type.Sum(FixTypes(sum.Left), FixTypes(sum.Right)),
			Type.TypeIdentifier typeIdentifier => FixTypes(typeIdentifier.Type),
			Type.Builtin or Type.CString or Type.Enum => type,
			Type.IntLiteral { Value: <= int.MaxValue and >= 1 } intLiteral => new Type.Enum((int)intLiteral.Value),
			_ => throw new Exception($"Type {type} for not fixable?")
		};

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