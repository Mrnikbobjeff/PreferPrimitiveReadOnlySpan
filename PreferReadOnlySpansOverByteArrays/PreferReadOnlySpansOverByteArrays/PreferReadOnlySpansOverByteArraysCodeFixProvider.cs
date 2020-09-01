using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace PreferReadOnlySpansOverByteArrays
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferReadOnlySpansOverByteArraysCodeFixProvider)), Shared]
    public class PreferReadOnlySpansOverByteArraysCodeFixProvider : CodeFixProvider
    {
        private const string title = "Change to ReadOnlySpan<byte>";
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PreferReadOnlySpansOverByteArraysAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => ChangeToReadOnlySpan(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> ChangeToReadOnlySpan(Document document, FieldDeclarationSyntax fieldDeclaration, CancellationToken cancellationToken)
        {
            var originalSolution = document.Project.Solution;
            var separatedTypes = new SeparatedSyntaxList<TypeSyntax>().Add((fieldDeclaration.Declaration.Type as ArrayTypeSyntax).ElementType);
            var typeListSyntax = SyntaxFactory.TypeArgumentList(separatedTypes);
            var readonlySpanType = SyntaxFactory.GenericName(SyntaxFactory.Identifier("ReadOnlySpan"), typeListSyntax);

            var equalsValueClauseSyntax = fieldDeclaration.Declaration.Variables.First().Initializer;
            var arrowExpressionSyntax = SyntaxFactory.ArrowExpressionClause(equalsValueClauseSyntax.Value);
            var propertyDeclaration = SyntaxFactory
                                        .PropertyDeclaration
                                            (fieldDeclaration.AttributeLists, 
                                            new SyntaxTokenList(fieldDeclaration.Modifiers.Where(x => !x.IsKind(SyntaxKind.ReadOnlyKeyword))), 
                                            readonlySpanType, 
                                            null, 
                                            fieldDeclaration.Declaration.Variables.First().Identifier,
                                            null, 
                                            arrowExpressionSyntax, 
                                            null) ;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(fieldDeclaration, propertyDeclaration.WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia()).WithSemicolonToken(fieldDeclaration.SemicolonToken).WithTrailingTrivia(fieldDeclaration.GetTrailingTrivia()));
            return originalSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }
    }
}
