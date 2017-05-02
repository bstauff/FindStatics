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
using Microsoft.CodeAnalysis;

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



        [Fact]
        public void IgnoreGeneratedCodeTest()
        {
            var test = @"
namespace crap
    {

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""yy"", ""1.0.0.0"")]
        class ItWasMeCodeGenThing
        {
            public static global::System.Object DeepCopier(global::System.Object original, global::System.Object context)
            {
                return new Object();
            }
            public static string Thing
            {
                get; set;
            }
        }
    }

";

            //we want no error
            VerifyCSharpDiagnostic(test);
        }



        [Fact]
        public void FoundAStaticTest()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProgram
{
    public class Class1
    {
        private static string Findme = ""ugh"";

        public Class1()
            {
                Findme = ""sdgsgsgsg"";
            }

        public static string Thing
        {
            get; set;
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "RB001",
                Message = "Field 'Findme' is marked static",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 9)
                    }
            };

            //we want no error
            VerifyCSharpDiagnostic(test, expected);
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
