using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FindStatics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace FindStaticsTests
{
    /// <summary>
    /// These tests are for the PROPERTIES analyzer only
    /// </summary>
    public class StaticPropertiesAnalyzerTests : CodeFixVerifier
    {
        [Fact]
        public void ShouldNotFindAnyStaticPropertiesTest()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IgnoreStaticPropertyInGeneratedCodeTest()
        {
            var test = @"
namespace junk
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
        public void ShouldFindAStaticPropertyTest()
        {
            var test =
            @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace TestProgram
            {
                public class Class1
                {
                    public static string Findme {get; set;} = ""ugh"";

                    public Class1()
                    {
                        Findme = ""sdgsgsgsg"";
                    }
                }
            }
            ";

            var expected = new DiagnosticResult
            {
                Id = "RB002",
                Message = "Field 'Findme' is marked static",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 21)
                    }
            };

            //Verify that we found the static field
            VerifyCSharpDiagnostic(test, expected);
        }


        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new FindStaticPropertiesCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new FindStaticPropertiesAnalyzer();
        }
    }
}
