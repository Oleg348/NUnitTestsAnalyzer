using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NUnitTests
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NUnitTestsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NUnitTests";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Refactoring";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.ContainingType != null)
                return;

            var testAttrFound = false;
            var curSymbol = symbol;
            while (curSymbol != null)
            {
                var attributes = curSymbol.GetAttributes();
                if (attributes.Any(a => a.AttributeClass.Name.EndsWith("TESTFIXTUREATTRIBUTE", StringComparison.OrdinalIgnoreCase)))
                {
                    testAttrFound = true;
                    break;
                }
                curSymbol = curSymbol.BaseType;
            }

            if (!testAttrFound)
                return;

            foreach (var methodSymbol in symbol.GetMembers().Where(mem => mem.Kind == SymbolKind.Method).Cast<IMethodSymbol>())
            {
                if (methodSymbol.DeclaredAccessibility != Accessibility.Public || methodSymbol.MethodKind == MethodKind.Constructor)
                    continue;

                if (!methodSymbol.GetAttributes()
                    .Any(a =>
                    {
                        var attrName = a.AttributeClass.Name.ToUpper();
                        return attrName.StartsWith("TEST")
                        || attrName.EndsWith("SETUPATTRIBUTE")
                        || attrName.EndsWith("TEARDOWNATTRIBUTE");
                    })
                )
                {
                    var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], methodSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
