using LLVMSharp.Interop;
using MeowC.Interpreter;
using MeowC.Parser.Matches;

namespace MeowC.Generators;

public unsafe class LLVMGen
{
	public LLVMGen(List<Definition> definitions)
	{
		LLVM.InitializeNativeTarget();
		LLVM.InitializeNativeAsmParser();
		LLVM.InitializeNativeAsmPrinter();
		Definitions = definitions;
		Context = LLVM.ContextCreate();
		Builder = LLVM.CreateBuilder();
		Module = LLVMModuleRef.CreateWithName("meowc");
		// Module.DataLayout = LLVM.D
		Evaluator = new(Context, Builder, Module);
	}

	public List<Definition> Definitions { get; }
	public LLVMContextRef Context { get; }
	public LLVMBuilderRef Builder { get; }
	public LLVMModuleRef Module { get; }
	public Dictionary<string, LLVMValueRef> NamedValues { get; } = new();
	private LLVMEvaluator Evaluator { get; }

	public void Compile()
	{
		foreach (var definition in Definitions)
		{
			if (definition is { Id: "main", Val: Expression.Procedure procedure })
				GenMainDef("main", procedure);
			else if (definition is { Val: Expression.BinaryOperator binop } && binop.Type == TokenTypes.MapsTo)
				GenFunctionDef(definition.Id, binop.Left, binop.Right);
		}

		// Console.Write(Module.PrintToString());
		Module.Dump();
		Console.WriteLine();
		var jit = Module.CreateMCJITCompiler();
		jit.
		// Console.Write(Module.);
		TargetMachine.EmitToFile(Module, "out.a", LLVMCodeGenFileType.LLVMAssemblyFile);
	}

	private void GenMainDef(string name, Expression body)
	{
		// GenFunctionDef("main", Expression., body);
	}

	public void GenFunctionDef(string functionName, Expression args, Expression body)
	{
		int argCount;
		var argNames = new List<string>();
		switch (args)
		{
			case Expression.Identifier bind:
				var bindId = new IdValue(bind.Name);
				argCount = 1;
				argNames.Add(bindId.Name);
				break;
			case Expression.Tuple tuple:
				throw new NotImplementedException("idk");
			default:
				throw new Exception("Functions require bindings");
		}

		var llvmargs = new LLVMTypeRef[argCount];
		llvmargs[0] = LLVMTypeRef.Int32;

		var ftype = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, llvmargs);
		var function = Module.AddFunction(functionName, ftype);
		var bindings = new Dictionary<IdValue, LLVMValueRef>();
		for (int i = 0; i < argNames.Count; i++)
		{
			var param = function.GetParam((uint)i);
			param.Name = argNames[i];
			bindings[new IdValue(argNames[i])] = param;
		}

		var bb = function.AppendBasicBlock("entry");
		Builder.PositionAtEnd(bb);
		var ret = Evaluator.Evaluate(body, bindings);
		Builder.BuildRet(ret);
		function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
	}
}