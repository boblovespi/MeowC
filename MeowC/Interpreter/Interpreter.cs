using MeowC.Interpreter.Types;
using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Interpreter;

public class Interpreter(List<Definition> definitions, Dictionary<Expression, Type> typeTable)
{
	private List<Definition> Definitions { get; } = definitions;
	private RuntimeEvaluator Evaluator { get; } = new(definitions, typeTable);
	private Dictionary<IdValue, object> GlobalBindings { get; } = new();

	public void Run()
	{
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
		GlobalBindings["Type"] = new Type.TypeIdentifier(Type.Types);
		foreach (var definition in Definitions)
			if (definition is { Id: "main", Val: Expression.Procedure procedure })
				RunProcedure(procedure);
	}


	private void RunProcedure(Expression.Procedure procedure)
	{
		Evaluator.Evaluate(procedure, GlobalBindings);
		// foreach (var statement in procedure.Statements) RunStatement(statement);
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