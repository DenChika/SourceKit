using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceKit.Extensions;

namespace SourceKit.Analyzers.Collections.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DictionaryKeyTypeMustImplementEquatableAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SK1500";
    public const string Title = nameof(DictionaryKeyTypeMustImplementEquatableAnalyzer);

    public const string Format = """Type argument for TKey must implement IEquatable""";

    public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        Format,
        "Design",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(AnalyzeGeneric , SyntaxKind.GenericName);
    }

    private void AnalyzeGeneric(SyntaxNodeAnalysisContext context)
    {
        var node = (GenericNameSyntax) context.Node;
        if (node.Identifier.Text != "Dictionary") return;

        var dictionaryTypeSymbol = GetSymbolFromContext(context, node) as ITypeSymbol;

        var keyTypeSymbol = GetFirstTypeSymbolArgument(dictionaryTypeSymbol!);

        if (keyTypeSymbol is null || keyTypeSymbol.MetadataName == "TKey") return;

        var interfaceNamedType = context.Compilation.GetTypeSymbol(typeof(IEquatable<>));

        var iequatableInterfaces = keyTypeSymbol.FindAssignableTypesConstructedFrom(interfaceNamedType);
        
        if (iequatableInterfaces
            .Select(s => s.TypeArguments.First())
            .Any(s => keyTypeSymbol.IsAssignableTo(s)))
            return;

        var diag = Diagnostic.Create(Descriptor, node.GetLocation());
        context.ReportDiagnostic(diag);
    }

    private ITypeSymbol? GetFirstTypeSymbolArgument(ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol namedTypeSymbol)
            return namedTypeSymbol.TypeArguments.FirstOrDefault();

        return null;
    }

    private static ISymbol? GetSymbolFromContext(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var model = context.SemanticModel;
        
        var symbolInfo = model.GetSymbolInfo(node);

        var symbol = symbolInfo.Symbol;

        return symbol;
    }
}