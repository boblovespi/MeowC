using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Interpreter;

public abstract record Constraint
{
	public record Unification(Type Type) : Constraint;
	
	public record Satisfaction(Type TypeClass) :  Constraint;
}