namespace MeowC.Interpreter;

public class BindingDict<T>
{
	private Dictionary<string, T> Globals { get; } = new();
	// a dict that holds bindings
	// has three dicts - a globals dict, a locals dict, and a builtins dict
	// the globals dict resolves global names
	// the locals dict resolves local names
	// the builtins dict resolves builtin names
	// names are always unique (ad-hoc polymorphism)
	// naming convention:
	// <name>(_uniqueNum)?:<type>
	// the name is the original name
	// a unique num is given if the name already exists
	// the type is the type
	// type convention:
	// base types are: b, s, i, l, B, S, I, L for unsigned, signed integer types
	// 2 for bools
	// 1 for unit
	// > for function types
	// , for tuple types
	// f, F for floats
	// $<name>$ for other types
	// operators are op_<opName> e.g. op_times (*) or op_and (&) or op_to (->)
}