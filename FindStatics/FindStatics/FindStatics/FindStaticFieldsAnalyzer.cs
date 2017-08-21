using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;

namespace FindStatics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FindStaticFieldsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RB001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title =
            new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager,
                typeof(Resources));

        private const string Category = "Design";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!AreThereAnyCodegenAttributes(context))
            {
                var fieldDeclarationNode = (FieldDeclarationSyntax) context.Node;

                var staticModifier = from x in fieldDeclarationNode.Modifiers
                    where x.IsKind(SyntaxKind.StaticKeyword)
                    select x;

                if (!staticModifier.Any())
                {
                    return;
                }

                var variableName = fieldDeclarationNode.Declaration.Variables.First().Identifier.ValueText;
                var diagnostic = Diagnostic.Create(Rule, fieldDeclarationNode.GetLocation(), variableName);
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
            var propertyDeclarationSyntax = (FieldDeclarationSyntax) context.Node;


            if (propertyDeclarationSyntax.Parent.CheckParentForAttributes())
                return true;


            //new System.Linq.SystemCore_EnumerableDebugView<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax>(((Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)((Microsoft.CodeAnalysis.SyntaxNode)propertyDeclarationSyntax).Parent).AttributeLists).Items[0]




            if (propertyDeclarationSyntax.SyntaxTree.IsGeneratedCode(context.CancellationToken))
                return true;


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

    internal static class Extensions
    {


        public static bool CheckParentForAttributes(this SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax)
            {
                var classnode = node as ClassDeclarationSyntax;

                var attributesOnThisClass = classnode.AttributeLists.SelectMany(x => x.Attributes);

                var generatedCodeAttributeNameSyntaxes = attributesOnThisClass
                    .Select(y => y.Name)
                    .Cast<QualifiedNameSyntax>()
                    .Where(y => y.Right.Identifier.ValueText.Equals("GeneratedCodeAttribute"));

                if (generatedCodeAttributeNameSyntaxes.Any())
                {
                    return true;
                }
                else
                {
                    return node.Parent.CheckParentForAttributes();
                }

            }
            else if (node is NamespaceDeclarationSyntax)
            {
                var nsnode = node as NamespaceDeclarationSyntax;

                var cnode = nsnode.Parent as CompilationUnitSyntax;
                var attributesOnThisClass = cnode.AttributeLists.SelectMany(x => x.Attributes);

                var generatedCodeAttributeNameSyntaxes = attributesOnThisClass
                    .Select(y => y.Name)
                    .Cast<QualifiedNameSyntax>()
                    .Where(y => y.Right.Identifier.ValueText.Equals("GeneratedCodeAttribute"));

                if (generatedCodeAttributeNameSyntaxes.Any())
                {
                    return true;
                }
                else
                {
                    return node.Parent.CheckParentForAttributes();
                }
            }

            return false;
        }






        public static bool IsGeneratedCode(this SyntaxTree tree, CancellationToken cancellationToken)
        {
            if (IsGeneratedCodeFile(tree.FilePath))
            {
                return true;
            }

            if (BeginsWithAutoGeneratedComment(tree, cancellationToken))
            {
                return true;
            }

            //recurse, walk the tree back up, looking for the codedom atribute

            return false;
        }

        private static bool IsGeneratedCodeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var fileName = Path.GetFileName(filePath);
            if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (fileNameWithoutExtension.EndsWith("AssemblyInfo", StringComparison.OrdinalIgnoreCase) ||
                fileNameWithoutExtension.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
                fileNameWithoutExtension.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
                fileNameWithoutExtension.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase) ||
                fileNameWithoutExtension.EndsWith(".AssemblyAttributes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool BeginsWithAutoGeneratedComment(SyntaxTree tree, CancellationToken cancellationToken)
        {
            var root = tree.GetRoot(cancellationToken);
            if (root.Kind() == SyntaxKind.CompilationUnit &&
                root.HasLeadingTrivia)
            {
                var leadingTrivia = root.GetLeadingTrivia();

                foreach (var trivia in leadingTrivia)
                {
                    if (trivia.Kind() != SyntaxKind.SingleLineCommentTrivia)
                    {
                        continue;
                    }

                    var text = trivia.ToString();

                    // Should start with single-line comment slashes. If not, move along.
                    if (text.Length < 2 || text[0] != '/' || text[1] != '/')
                    {
                        continue;
                    }

                    // Scan past whitespace.
                    int index = 2;
                    while (index < text.Length && char.IsWhiteSpace(text[index]))
                    {
                        index++;
                    }

                    // Check to see if the text of the comment starts with "<auto-generated>".
                    const string AutoGenerated = "<auto-generated>";

                    if (string.Compare(text, index, AutoGenerated, 0, AutoGenerated.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }




    }
}
