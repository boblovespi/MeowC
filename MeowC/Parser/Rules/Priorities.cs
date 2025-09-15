namespace MeowC.Parser.Rules;

public enum Priorities
{
	No,
	Assignment,
	PolymorphismFormation,
	FunctionFormation,
	Conditional,
	Sum,
	Product,
	Exponent,
	Prefix,
	Application,
	Postfix,
	Const
}