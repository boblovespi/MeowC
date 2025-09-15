using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Interpreter;

public class TypeChecker
{
	public TypeChecker(List<Definition> definitions)
	{
		Definitions = definitions;
		Evaluator = new(TypeTable);
	}

	private List<Definition> Definitions { get; }
	private TypeEvaluator Evaluator { get; }
	private Dictionary<IdValue, Type> GlobalBindings { get; } = new();
	public Dictionary<Expression, Type> TypeTable { get; } = new();

	public void Check()
	{
		foreach (var definition in Definitions)
		{
			try
			{
				var type = Evaluator.Evaluate(definition.Type, new Dictionary<IdValue, Type>(GlobalBindings));
				GlobalBindings[new IdValue(definition.Id)] = type switch
				{
					Type.TypeIdentifier => NormalizeTypes(type),
					Type.IntLiteral { Value: <= int.MaxValue and >= 1 } intLiteral => new Type.Enum((int)intLiteral.Value),
					_ => throw new TokenException($"Type {type} for definition {definition.Id} ought to be a type identifier", definition.Val.Token)
				};
				TypeTable[definition.Val] = GlobalBindings[new IdValue(definition.Id)];
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
			// if (definition is { Val: Expression.Procedure procedure })
				// CheckProcedure(procedure);
			// Console.WriteLine(GlobalBindings[new IdValue(definition.Id)]);
		}
	}

	/// <summary>
	///  Normalizes a type (i.e. turns an expression tree into its reduced form)
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	internal static Type NormalizeTypes(Type type) =>
		type switch
		{
			Type.Function function => new Type.Function(NormalizeTypes(function.From), NormalizeTypes(function.To)),
			Type.Product product => new Type.Product(NormalizeTypes(product.Left), NormalizeTypes(product.Right)),
			Type.Sum sum => new Type.Sum(NormalizeTypes(sum.Left), NormalizeTypes(sum.Right)),
			Type.TypeIdentifier typeIdentifier => NormalizeTypes(typeIdentifier.Type),
			Type.Builtin or Type.CString or Type.Enum or Type.TypeUniverse or Type.Variable => type,
			Type.IntLiteral { Value: <= int.MaxValue and >= 1 } intLiteral => new Type.Enum((int)intLiteral.Value),
			Type.Polymorphic(var from, var typeClass, var to) => new Type.Polymorphic(from, NormalizeTypes(typeClass), NormalizeTypes(to)),
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