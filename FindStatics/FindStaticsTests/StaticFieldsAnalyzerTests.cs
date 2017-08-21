using System;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>
    /// These tests are for the FIELDS analyzer only
    /// </summary>
    public class StaticFieldsAnalyzerTests : CodeFixVerifier
    {
        [Fact]
        public void ShouldNotFindAnyStaticFieldsTest()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }


        [Fact]
        public void IgnoreStaticFieldInOrleansGeneratedCodeTest()
        {
            var test = File.ReadAllText(@"testfiles\codegen.txt");

            //This should not generate any CodeAnalysis warnings
            VerifyCSharpDiagnostic(test);
        }



        [Fact]
        public void IgnoreStaticFieldInGeneratedCodeTest()
        {
            var test = 
            @"
            namespace junk
            {

                [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""yy"", ""1.0.0.0"")]
                class ItWasMeCodeGenThing
                {
                    private static string _findThis = ""Blergh"";

                    public static global::System.Object DeepCopier(global::System.Object original, global::System.Object context)
                    {
                        return new Object();
                    }         
                }
            }
            ";

            //This should not generate any CodeAnalysis warnings
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void ShouldFindAStaticFieldTest()
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
                    private static string Findme = ""ugh"";

                    public Class1()
                    {
                        Findme = ""sdgsgsgsg"";
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
                        new DiagnosticResultLocation("Test0.cs", 12, 21)
                    }
            };

            //Verify that we found the static field
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
