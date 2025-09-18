using MeowC.Diagnostics;

namespace MeowC;

public class CompilationUnit
{
	public string FileName { get; init; }
	public string OutputPath { get; init; }
	public string Code { get; init; }
	public List<string> Lines { get; init; }
	public bool Errored { get; private set; }
	private readonly List<Diagnostic> diagnostics = new();
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

	public void AddDiagnostic(Diagnostic diagnostic)
	{
		diagnostics.Add(diagnostic);
		if (diagnostic.Level == DiagLevel.Error)
			Errored = true;
	}
}