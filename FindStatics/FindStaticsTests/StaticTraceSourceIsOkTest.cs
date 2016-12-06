using System;
using FindStatics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoslynTester;
using RoslynTester.DiagnosticResults;
using RoslynTester.Helpers.CSharp;
using RoslynTester.Helpers.Testing;

namespace Tests.Tests
{
	[TestClass]
	public class StaticTraceSourceIsOkTest : CSharpCodeFixVerifier
	{
		protected override CodeFixProvider CodeFixProvider => new FindStaticFieldsCodeFixProvider();

		protected override DiagnosticAnalyzer DiagnosticAnalyzer => new FindStaticFieldsAnalyzer();

		[TestMethod]
		public void StaticTraceSourceIsOk()
		{
			#region source code
			var originalSource = 
@"	using System;
	using System.Text;

	namespace ConsoleApplication1
	{
		class MyClass
		{
			private static TraceSource var1;
			private static string text;

			void Method()
			{
			}
		}
	}";

			var fixedSource =
@"	using System;
	using System.Text;

	namespace ConsoleApplication1
	{
		class MyClass
		{
			private static TraceSource var1;
			private string text;

			void Method()
			{
			}
		}
	}";
			#endregion source code

			var expectedDiagnostic = new DiagnosticResult
			{
				Id = FindStaticFieldsAnalyzer.DiagnosticId,
				Message = "Field 'text' is marked static",
				Severity = DiagnosticSeverity.Warning,
				Locations =
					new[]
					{
						new DiagnosticResultLocation("Test0.cs", 9, 4)
					}
			};

			VerifyDiagnostic(originalSource, expectedDiagnostic);
			VerifyFix(originalSource, fixedSource);
		}
	}
}
