using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace NUnitTests
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NUnitTestsCodeFixProvider)), Shared]
    public class NUnitTestsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NUnitTestsAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(context.Document);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

                string attrName = "Test";
                string title = CodeFixResources.AddTestAttributeTitle;
                if (declaration.ParameterList.Parameters.Count > 0)
                {
                    attrName = "TestCase";
                    title = CodeFixResources.AddTestCaseAttributeTitle;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: _ => _AddAttribute(context.Document, generator, root, declaration, attrName),
                        equivalenceKey: title),
                    diagnostic);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.MakeMethodPrivateTitle,
                        createChangedSolution: _ => _ChangeMethodAccessabilityAsync(context.Document, generator, root, declaration, Accessibility.Private),
                        equivalenceKey: nameof(CodeFixResources.MakeMethodPrivateTitle)),
                    diagnostic);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.MakeMethodProtectedTitle,
                        createChangedSolution: _ => _ChangeMethodAccessabilityAsync(context.Document, generator, root, declaration, Accessibility.Protected),
                        equivalenceKey: nameof(CodeFixResources.MakeMethodProtectedTitle)),
                    diagnostic);
            }
        }

        private Task<Solution> _ChangeMethodAccessabilityAsync(Document doc, SyntaxGenerator gen, SyntaxNode root, SyntaxNode methodNode, Accessibility accessibility)
        {
            var newMethodNode = gen.WithAccessibility(methodNode, accessibility);
            var newRoot = root.ReplaceNode(methodNode, newMethodNode);
            return Task.FromResult(doc.WithSyntaxRoot(newRoot).Project.Solution);
        }

        private Task<Solution> _AddAttribute(Document doc, SyntaxGenerator gen, SyntaxNode root, SyntaxNode node, string attrName)
        {
            var newMethodNode = gen.InsertAttributes(node, 0, SyntaxFactory.Attribute(SyntaxFactory.ParseName(attrName)));
            var newRoot = root.ReplaceNode(node, newMethodNode);
            return Task.FromResult(doc.WithSyntaxRoot(newRoot).Project.Solution);
        }
    }
}
