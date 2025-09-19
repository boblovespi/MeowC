using MeowC.Diagnostics;

namespace MeowC;

public class CompilationUnit
{
	public string FileName { get; }
	public string OutputPath { get; }
	public string Code { get; }
	public List<string> Lines { get; }
	public bool Errored { get; private set; }
	private readonly List<Diagnostic> diagnostics = [];
	public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

	public CompilationUnit(string inputFile, string outputFile = "")
	{
		var targetFile = new FileInfo(inputFile);
		FileName = targetFile.Name;
		using var reader = targetFile.OpenText();
		var lines = reader.ReadToEnd();
		Code = lines;
		Lines = lines.Split('\n').ToList();
		OutputPath = outputFile == "" ? Path.GetFileNameWithoutExtension(FileName) : outputFile;
	}

	private CompilationUnit(string code, bool unused)
	{
		Code = code;
		Lines = code.Split('\n').ToList();
		FileName = "test.meow";
		OutputPath = "test";
	}

	public static CompilationUnit TestFromCode(string code) => new(code, false);

	public void AddDiagnostic(Diagnostic diagnostic)
	{
		diagnostics.Add(diagnostic);
		if (diagnostic.Level == DiagLevel.Error)
			Errored = true;
	}
}