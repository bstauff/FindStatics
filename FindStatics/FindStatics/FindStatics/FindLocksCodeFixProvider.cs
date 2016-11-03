using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FindStatics
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FindLocksCodeFixProvider)), Shared]
    public class FindLocksCodeFixProvider : CodeFixProvider
    {
        private const string title = "Remove lock statement";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FindLocksAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var lockStatementExpression =
                root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LockStatementSyntax>().First();

            context.RegisterCodeFix(CodeAction.Create(title, c => RemoveLockAsync(context.Document, lockStatementExpression, c), title), diagnostic);
        }

        private async Task<Document> RemoveLockAsync(Document document, LockStatementSyntax lockStatement,
            CancellationToken cancellationToken)
        {
            /*
             * Save the block statement node from within the lock
             * Remove the entire lock statement syntax node
             * re insert the block statement node
             */

            var lockNodeBlockStatements = from x in lockStatement.ChildNodes()
                                          where x is BlockSyntax
                                          select x;

            var lockNodeExpressionStatements = from BlockSyntax x in lockNodeBlockStatements
                                            select x.Statements;

            var allStatements = from SyntaxList<StatementSyntax> x in lockNodeExpressionStatements
                                from StatementSyntax y in x
                                select y;

            var lockNodeParent = lockStatement.Parent;

            var currentRoot = await document.GetSyntaxRootAsync();

            var rootWithoutLockStatement = currentRoot.ReplaceNode(lockStatement, allStatements);

            var newDocument = document.WithSyntaxRoot(rootWithoutLockStatement);

            return newDocument;

        }
    }
}
