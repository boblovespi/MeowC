using MediatR;
using MeowC.Diagnostics;
using MeowC.Interpreter;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Diagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace MeowC.LSP;

public class TextHandler : TextDocumentSyncHandlerBase
{
	private Dictionary<DocumentUri, CompilationUnit> buffer = new();
	private ILanguageServerFacade server;

	public TextHandler(ILanguageServerFacade server)
	{
		this.server = server;
	}

	public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
	{
		return new TextDocumentAttributes(uri, "meow");
	}

	public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
	{
		return Unit.Task;
	}

	public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
	{
		foreach (var change in request.ContentChanges)
		{
			buffer[request.TextDocument.Uri] = CompilationUnit.TestFromCode(change.Text);
		}

		return Unit.Task;
	}

	public override async Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
	{
		var text = request.Text!;
		var uri = request.TextDocument.Uri;
		if (!buffer.ContainsKey(uri))
			buffer[uri] = CompilationUnit.TestFromCode(text);
		var compUnit = buffer[uri];
		var lexer = new Lexer(compUnit);
		lexer.Parse();
		var parser = new Parser.Parser(compUnit, lexer.Tokens);
		parser.Parse();
		var typeChecker = new TypeChecker(compUnit, parser.Definitions);
		typeChecker.Check();
		Program.PrintDiagnostics(compUnit);
		var diagnostics = new PublishDiagnosticsParams
		{
			Uri = uri,
			Version = null,
			Diagnostics = compUnit.Diagnostics.Select(diagnostic =>
			{
				return new Diagnostic
				{
					Range = new Range(diagnostic.Line - 1, diagnostic.Column - 1 - diagnostic.Span, diagnostic.Line - 1,
						diagnostic.Column - 1),
					Severity = diagnostic.Level switch
					{
						DiagLevel.Error => DiagnosticSeverity.Error,
						DiagLevel.Warning or DiagLevel.WeakWarning => DiagnosticSeverity.Warning,
						DiagLevel.Info => DiagnosticSeverity.Information
					},
					Code = diagnostic.Phase.ToString()[0] + diagnostic.Code.ToString("D3"),
					CodeDescription = null, // Diagnostics.Diagnostic.GetDiagnosticName(diagnostic.Phase, diagnostic.Code),
					Source = "MeowC",
					Message = diagnostic.Message,
					Tags = null,
					RelatedInformation = null,
					Data = null
				};
			}).ToList()
		};
		
		server.TextDocument.PublishDiagnostics(diagnostics);
		// await server.TextDocument.SendRequest(diagnostics, CancellationToken.None);
		return await Unit.Task;
	}

	public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
	{
		return Unit.Task;
	}

	protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability,
		ClientCapabilities clientCapabilities)
	{
		var options = new TextDocumentSyncRegistrationOptions();
		options.Change = TextDocumentSyncKind.Full;
		// options.DocumentSelector = TextDocumentSelector.ForLanguage("meow");
		options.Save = new SaveOptions
		{
			IncludeText = true
		};
		return options;
	}
}