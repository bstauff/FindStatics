using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FindStatics;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace FindStaticsTests
{
    public class StaticFieldsAnalyzerTests : CodeFixVerifier
    {
        [Fact]
        public void EnsureNoDiagnosticsShowWhenTheyShouldntTest()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new FindStaticFieldsCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new FindStaticFieldsAnalyzer();
        }
    }
}
