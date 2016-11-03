using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FindStatics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FindLocksAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RB003";

        private static readonly string Title = "Lock statement found";
        private static readonly string MessageFormat = "Lock statement should be avoided in Orleans environment";
        private static readonly string Description = "Avoid using lock statements when working with Orleans.  Consider removing the lock.";
        private const string Category = "Design";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
        }

        private void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
        {
            var lockStatementNode = (LockStatementSyntax) context.Node;

            var diagnostic = Diagnostic.Create(Rule, lockStatementNode.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
