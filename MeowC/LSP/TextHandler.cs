using System.Diagnostics;
using MediatR;
using MeowC.Diagnostics;
using MeowC.Interpreter;
using MeowC.Parser.Matches;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Diagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Type = MeowC.Interpreter.Types.Type;

namespace MeowC.LSP;

public class TextHandler(ILanguageServerFacade server) : TextDocumentSyncHandlerBase, IHoverHandler
{
	private readonly Dictionary<DocumentUri, CompilationUnit> compUnitBuffer = new();
	private readonly Dictionary<DocumentUri, List<Definition>> definitionBuffer = new();
	private readonly Dictionary<DocumentUri, Dictionary<Expression, Type>> typeTableBuffer = new();

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
		var uri = request.TextDocument.Uri;

		foreach (var change in request.ContentChanges)
			compUnitBuffer[uri] = CompilationUnit.TestFromCode(change.Text);

		var compUnit = compUnitBuffer[uri];
		ParseAndTypecheck(compUnit, uri);
		return Unit.Task;
	}

	public override async Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
	{
		var text = request.Text!;
		var uri = request.TextDocument.Uri;
		if (compUnitBuffer.ContainsKey(uri))
			return Unit.Value;
		compUnitBuffer[uri] = CompilationUnit.TestFromCode(text);
		var compUnit = compUnitBuffer[uri];
		ParseAndTypecheck(compUnit, uri);
		// await server.TextDocument.SendRequest(diagnostics, CancellationToken.None);
		return Unit.Value;
	}

	private void ParseAndTypecheck(CompilationUnit compUnit, DocumentUri uri)
	{
		var lexer = new Lexer(compUnit);
		lexer.Parse();
		var parser = new Parser.Parser(compUnit, lexer.Tokens);
		parser.Parse();
		definitionBuffer[uri] = parser.Definitions;
		var typeChecker = new TypeChecker(compUnit, parser.Definitions);
		typeChecker.Check();
		typeTableBuffer[uri] = typeChecker.TypeTable;
		// Program.PrintDiagnostics(compUnit);
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
					Message = $"{Diagnostics.Diagnostic.GetDiagnosticName(diagnostic.Phase, diagnostic.Code)}: {diagnostic.Message}",
					Tags = null,
					RelatedInformation = null,
					Data = null
				};
			}).ToList()
		};

		server.TextDocument.PublishDiagnostics(diagnostics);
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

	public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
	{
		var uri = request.TextDocument.Uri;
		if (!compUnitBuffer.TryGetValue(uri, out var unit))
			return null;
		var pos = request.Position;
		var line = pos.Line + 1;
		var col = pos.Character + 1;
		Expression? expression = null;
		var definitions = definitionBuffer[uri];
		foreach (var definition in definitions)
		{
			var expr = GetExpressionForPos(line, col, definition.Type);
			if (expr == null)
			{
				expr = GetExpressionForPos(line, col, definition.Val);
				if (expr == null)
					continue;
			}
			expression = expr;
			break;
		}

		if (expression == null)
			return null;
		var types = typeTableBuffer[uri];
		if (!types.TryGetValue(expression, out var type))
			return null;
		return new Hover
		{
			Contents = new MarkedStringsOrMarkupContent(new MarkupContent
			{
				Kind = MarkupKind.PlainText,
				Value = type.ToStringNoVal
			}),
			Range = null
		};
	}

	public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
	{
		return new HoverRegistrationOptions
		{
			DocumentSelector = null,
			WorkDoneProgress = false
		};
	}

	private Expression? GetExpressionForPos(int line, int col, Expression expression)
	{
		switch (expression)
		{
			case Expression.Application application:
				return GetExpressionForPos(line, col, application.Function) ?? GetExpressionForPos(line, col, application.Argument);
			case Expression.BinaryOperator binaryOperator:
				var left = GetExpressionForPos(line, col, binaryOperator.Left);
				// var right = GetExpressionForPos(line, col, binaryOperator.Right);
				if (left != null)
					return left;
				if (binaryOperator.Token.Line == line && binaryOperator.Token.Col >= col)
					return expression;
				return GetExpressionForPos(line, col, binaryOperator.Right);
			
			case Expression.Case @case:
				if (@case.Token.Line == line && @case.Token.Col >= col)
					return expression;
				foreach (var caseCase in @case.Cases)
					switch (caseCase)
					{
						case Case.Bool b:
							var boolValue = GetExpressionForPos(line, col, b.Value);
							if (boolValue != null)
								return boolValue;
							var boolPattern = GetExpressionForPos(line, col, b.Pattern);
							if (boolPattern != null)
								return boolPattern;
							break;
						case Case.Otherwise otherwise:
							var caseExpr = GetExpressionForPos(line, col, otherwise.Value);
							if (caseExpr != null)
								return caseExpr;
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(caseCase));
					}
				return null;
			
			case Expression.Prefix prefix:
				if (prefix.Token.Line == line && prefix.Token.Col >= col)
					return expression;
				return GetExpressionForPos(line, col, prefix.Expression);
			case Expression.Procedure procedure:
				break;
			case Expression.Identifier identifier when identifier.Token.Line == line && identifier.Token.Col >= col:
			case Expression.Number number when number.Token.Line == line && number.Token.Col >= col:
			case Expression.String s when s.Token.Line == line && s.Token.Col >= col:
			case Expression.Unit unit when unit.Token.Line == line && unit.Token.Col >= col:
				return expression;

			case Expression.Tuple tuple:
				break;
			case Expression.Record record:
				break;
			case Expression.Variant variant:
				break;
			default:
				return null;
		}

		return null;
		throw new UnreachableException();
	}
}