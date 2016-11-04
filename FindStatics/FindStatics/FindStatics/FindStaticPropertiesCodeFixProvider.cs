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

namespace FindStatics
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FindStaticPropertiesCodeFixProvider)), Shared]
    public class FindStaticPropertiesCodeFixProvider : CodeFixProvider
    {
        private const string title = "Remove static from field declaration";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FindStaticPropertiesAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var propertyDeclarationExpression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => RemoveStaticAsync(context.Document, propertyDeclarationExpression, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> RemoveStaticAsync(Document document, PropertyDeclarationSyntax fieldDeclaration, CancellationToken cancellationToken)
        {
            var staticModifier = from x in fieldDeclaration.Modifiers
                                 where x.IsKind(SyntaxKind.StaticKeyword)
                                 select x;

            var staticSyntaxToken = staticModifier.First();

            var root = await document.GetSyntaxRootAsync();

            var emptyToken = SyntaxFactory.Token(SyntaxKind.None);

            var newRootNode = root.ReplaceToken(staticSyntaxToken, emptyToken);

            var newDocument = document.WithSyntaxRoot(newRootNode);

            return newDocument;
        }
    }
}