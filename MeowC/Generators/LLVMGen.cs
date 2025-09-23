using System.Runtime.InteropServices;
using LLVMSharp.Interop;
using MeowC.Interpreter;
using MeowC.Parser.Matches;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.Generators;

public unsafe class LLVMGen(string targetFile)
{
	public LLVMTypeRef PtrType { get; }
	public LLVMTargetDataRef DataLayout { get; }

	public LLVMGen(string targetFile, List<Definition> definitions, Dictionary<Expression, Type> typeTable) : this(targetFile)
	{
		LLVM.InitializeNativeTarget();
		LLVM.InitializeNativeAsmParser();
		LLVM.InitializeNativeAsmPrinter();
		var target = "x86_64-unknown-linux-gnu"; // LLVMTargetRef.DefaultTriple;
		Target = LLVMTargetRef.GetTargetFromTriple(target).CreateTargetMachine(target,
			"generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelNone, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
		Definitions = definitions;
		TypeTable = typeTable;
		Context = LLVM.ContextCreate();
		Builder = LLVM.CreateBuilder();
		Module = LLVMModuleRef.CreateWithName("meowc");
		DataLayout = Target.CreateTargetDataLayout();
		LLVM.SetModuleDataLayout(Module, DataLayout);
		Evaluator = new(Context, Builder, Module, TypeTable, this);
		PtrType = LLVMTypeRef.CreateIntPtr(DataLayout);
	}

	public LLVMTargetMachineRef Target { get; }
	public List<Definition> Definitions { get; }
	public Dictionary<Expression, Type> TypeTable { get; }
	public LLVMContextRef Context { get; }
	public LLVMBuilderRef Builder { get; }
	public LLVMModuleRef Module { get; }
	public Dictionary<IdValue, LLVMValueRef> NamedValues { get; } = new();
	private LLVMEvaluator Evaluator { get; }

	public void Compile()
	{
		Module.AddFunction("print:3i32_t", LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [LLVMTypeRef.Int32]));
		Module.AddFunction("print:3str_t", LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [PtrType]));

		foreach (var definition in Definitions)
		{
			Console.WriteLine(definition.Id);
			if (definition is { Id: "main", Val: Expression.Procedure procedure })
				GenMainDef("main", procedure);
			else if (definition is { Val: Expression.BinaryOperator binop } && binop.Type == TokenTypes.MapsTo)
				GenFunctionDef(definition.Id, binop.Left, binop.Right);
		}

		// Console.Write(Module.PrintToString());
		// Module.Dump();
		Module.PrintToFile($"{targetFile}.uoll");
		// Console.WriteLine();
		// var fpm = Module.CreateFunctionPassManager();
		// fpm.InitializeFunctionPassManager();
		var pbo = LLVM.CreatePassBuilderOptions();

		var passes = new MarshaledString("constmerge,mem2reg,always-inline,reassociate,aggressive-instcombine,simplifycfg,gvn,tailcallelim");
		var err = LLVM.RunPasses(Module, passes, Target, pbo);

		if (err != null)
		{
			Console.WriteLine("something went wrong in passes!");
			var errorMessage = LLVM.GetErrorMessage(err);
			var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)errorMessage);
			Console.WriteLine(span.AsString());
		}

		LLVM.DisposePassBuilderOptions(pbo);

		// Module.Dump();
		Module.PrintToFile($"{targetFile}.opll");
		// Console.WriteLine();
		var jit = Module.CreateMCJITCompiler();
		// jit.TargetMachine.EmitToFile(Module, "out.a", LLVMCodeGenFileType.LLVMAssemblyFile);
		Target.EmitToFile(Module, $"{targetFile}.o", LLVMCodeGenFileType.LLVMObjectFile);
		Target.EmitToFile(Module, $"{targetFile}.s", LLVMCodeGenFileType.LLVMAssemblyFile);
		Module.WriteBitcodeToFile($"{targetFile}.b");
	}

	private void GenMainDef(string name, Expression.Procedure body)
	{
		var function = Module.AddFunction(name, LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, []));
		var bb = function.AppendBasicBlock("entry");
		Builder.PositionAtEnd(bb);
		var bindings = new Dictionary<IdValue, LLVMValueRef>(NamedValues);
		foreach (var variable in body.Definitions)
		{
			var type = Evaluator.LLVMType(TypeTable[variable.Type]);
			var varName = variable.Id;
			var alloca = Builder.BuildAlloca(type, varName);
			if (variable.Val is not null)
				Builder.BuildStore(Evaluator.Evaluate(variable.Val, bindings), alloca);
			bindings[variable.Id] = alloca;
		}

		foreach (var statement in body.Statements)
		{
			switch (statement)
			{
				case Statement.Callable callable:
					if (!bindings.TryGetValue(callable.Routine, out var func))
					{
						var typeStr = TypeTable[callable.Argument] switch
						{
							Type.CString => "str_t",
							Type.IntLiteral => "i32_t",
							Type.Builtin builtin => builtin.Value.ToString().ToLowerInvariant() + "_t",
							{ } a => throw new ArgumentOutOfRangeException(a.ToString())
						};
						func = Module.GetNamedFunction($"{callable.Routine}:3{typeStr}");
					}

					var args = new LLVMValueRef[1];
					args[0] = Evaluator.Evaluate(callable.Argument, bindings);
					var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [Evaluator.LLVMType(TypeTable[callable.Argument])]);
					var ret = Builder.BuildCall2(funcType, func, args);
					break;

				case Statement.Assignment assignment:
					var val = Evaluator.Evaluate(assignment.Value, bindings);
					Builder.BuildStore(val, bindings[assignment.Variable]);
					break;
			}
		}

		Builder.BuildRetVoid();
		function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
	}

	public void GenFunctionDef(string functionName, Expression args, Expression body)
	{
		int argCount;
		var argNames = new List<string>();
		switch (args)
		{
			case Expression.Identifier bind:
				var bindId = (IdValue)bind.Name;
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
		var bindings = new Dictionary<IdValue, LLVMValueRef>(NamedValues);
		for (int i = 0; i < argNames.Count; i++)
		{
			var param = function.GetParam((uint)i);
			param.Name = argNames[i];
			bindings[argNames[i]] = param;
		}

		var bb = function.AppendBasicBlock("entry");
		Builder.PositionAtEnd(bb);
		var ret = Evaluator.Evaluate(body, bindings);
		Builder.BuildRet(ret);
		function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);
		NamedValues[functionName] = function;
		// function.ViewFunctionCFG();
	}
}