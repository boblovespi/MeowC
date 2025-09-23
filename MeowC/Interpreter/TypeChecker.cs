using MeowC.Diagnostics;
using MeowC.Interpreter.Types;
using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Interpreter;

public class TypeChecker
{
	public TypeChecker(CompilationUnit unit, List<Definition> definitions)
	{
		Unit = unit;
		Definitions = definitions;
		Evaluator = new(TypeTable, Constraints);
		GlobalBindings["i8"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.I8));
		GlobalBindings["i16"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.I16));
		GlobalBindings["i32"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.I32));
		GlobalBindings["i64"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.I64));
		GlobalBindings["u8"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.U8));
		GlobalBindings["u16"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.U16));
		GlobalBindings["u32"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.U32));
		GlobalBindings["u64"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.U64));
		GlobalBindings["f32"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.F32));
		GlobalBindings["f64"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.F64));
		GlobalBindings["proc"] = new Type.TypeIdentifier(new Type.Builtin(Builtins.Proc));
	}

	private CompilationUnit Unit { get; }
	private List<Definition> Definitions { get; }
	private TypeEvaluator Evaluator { get; }
	private Dictionary<IdValue, Type> GlobalBindings { get; } = new();
	private bool Errored { get; set; } = false;
	private Dictionary<Type.Hole, List<Constraint>> Constraints { get; } = new();
	public Dictionary<Expression, Type> TypeTable { get; } = new();

	public void Check()
	{
		foreach (var definition in Definitions)
		{
			try
			{
				var type = Evaluator.Evaluate(definition.Type, new Dictionary<IdValue, Type>(GlobalBindings));
				GlobalBindings[definition.Id] = type switch
				{
					Type.TypeIdentifier => NormalizeTypes(type),
					Type.IntLiteral { Value: <= int.MaxValue and >= 1 } intLiteral => new Type.Enum((int)intLiteral.Value),
					_ => throw new TokenException(0, $"Type `{type}` for definition `{definition.Id}` ought to be a type identifier",
						definition.Val.Token)
				};
				TypeTable[definition.Val] = GlobalBindings[definition.Id];
			}
			catch (TokenException e)
			{
				Unit.AddDiagnostic(
					Diagnostic.TypecheckError(Unit, e.Code, e.At, e.Message));
				Errored = true;
			}
			catch (CompileException e)
			{
				Program.Error(e);
				Errored = true;
			}
		}

		foreach (var definition in Definitions)
		{
			try
			{
				var expected = GlobalBindings[definition.Id];
				var actual = Evaluator.Evaluate(definition.Val, new Dictionary<IdValue, Type>(GlobalBindings), expected);
				Evaluator.AddUnificationConstraint(expected, actual);
				if (!(expected & actual))
				{
					Unit.AddDiagnostic(
						Diagnostic.TypecheckError(Unit, 201, definition.Val.Token, $"Expected type `{expected}` but got `{actual}`"));
					Errored = true;
				}
			}
			catch (TokenException e)
			{
				Unit.AddDiagnostic(
					Diagnostic.TypecheckError(Unit, e.Code, e.At, e.Message));
				Errored = true;
			}
			catch (CompileException e)
			{
				Program.Error(e);
				Errored = true;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}
			// if (definition is { Val: Expression.Procedure procedure })
			// CheckProcedure(procedure);
			// Console.WriteLine(GlobalBindings[new IdValue(definition.Id)]);
		}

		// Unify some stuff
		foreach (var (hole, constraints) in Constraints)
		{
			Console.Out.WriteLine($"{hole}: {string.Join(", ", constraints)}");
			Type? type = null;
			foreach (var constraint in constraints)
			{
				switch (constraint)
				{
					case Constraint.Unification unification:
						if (type == null)
							type = unification.Type;
						else if (type & unification.Type)
						{
							if (unification.Type.IsStricterType(type))
								type = unification.Type;
						}
						else
						{
							Unit.AddDiagnostic(Diagnostic.TypecheckError(Unit, 202, hole.Token,
								$"Could not unify {hole}: types `{type}` and `{unification.Type}` are not unifiable"));
							Errored = true;
						}

						break;
				}
			}
			if (type == null)
			{
				Unit.AddDiagnostic(Diagnostic.TypecheckError(Unit, 202, hole.Token,
					$"Could not unify {hole}: got no concrete types to realize it with"));
				Errored = true;
			}
			else
			{
				// TODO: fill in holes
			}
		}

		if (!Errored)
			Program.Info($"Successfully typechecked {Unit.FileName}");
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
			_ => throw new Exception($"Type `{type}` for not fixable?")
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