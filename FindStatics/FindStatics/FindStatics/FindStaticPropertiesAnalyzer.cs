using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace FindStatics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FindStaticPropertiesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RB002";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Design";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            
            if (!AreThereAnyCodegenAttributes(context))
            {
                var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

                var staticModifier = from x in propertyDeclarationSyntax.Modifiers
                    where x.IsKind(SyntaxKind.StaticKeyword)
                    select x;

                if (!staticModifier.Any())
                {
                    return;
                }

                var variableName = propertyDeclarationSyntax.Identifier.ValueText;
                var diagnostic = Diagnostic.Create(Rule, propertyDeclarationSyntax.GetLocation(), variableName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Helper method to check for codegen attributes
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns true if there are codegen attributes, false if there aren't.</returns>
        private bool AreThereAnyCodegenAttributes(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

            var classDeclarationSyntaxNode = propertyDeclarationSyntax.SyntaxTree
                .GetRoot()
                .DescendantNodes()
                .Single(x => x.IsKind(SyntaxKind.ClassDeclaration));

            var classDeclaractionSyntax = classDeclarationSyntaxNode as ClassDeclarationSyntax;

            var attributesOnThisClass = classDeclaractionSyntax.AttributeLists.SelectMany(x => x.Attributes);

            var generatedCodeAttributeNameSyntaxes = attributesOnThisClass
                .Select(y => y.Name)
                .Cast<QualifiedNameSyntax>()
                .Where(y => y.Right.Identifier.ValueText.Equals("GeneratedCodeAttribute"));

            return generatedCodeAttributeNameSyntaxes.Any();
        }
    }
}
