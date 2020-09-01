using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using PreferReadOnlySpansOverByteArrays;

namespace PreferReadOnlySpansOverByteArrays.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptySource_NoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        static readonly byte[]  test = new byte[1] { 1 };
        static ReadOnlySpan<byte>  testSpan => new byte[1] { 1 };
        static ReadOnlySpan<Int16> testSpanInt => new Int16[1] { 1 };

        [TestMethod]
        public void PropertyDeclaration_SingleDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            static readonly byte[] test = new byte[1] { 1 };
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "PreferReadOnlySpansOverByteArrays",
                Message = String.Format("Field declaration '{0}' can be improved by changing its type.", "static readonly byte[] test = new byte[1] { 1 };"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 13)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
        }
        [TestMethod]
        public void PropertyDeclaration_SignedByte_SingleDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            static readonly sbyte[] test = new sbyte[1] { 1 };
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "PreferReadOnlySpansOverByteArrays",
                Message = String.Format("Field declaration '{0}' can be improved by changing its type.", "static readonly sbyte[] test = new sbyte[1] { 1 };"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 13)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void PropertyDeclaration_Int16_SingleDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            static readonly ushort[] test = new ushort[1] { 1 };
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void PropertyDeclaration_NonPrimitiveByte_SingleDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            static readonly Byte[] test = new Byte[1] { 1 };
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "PreferReadOnlySpansOverByteArrays",
                Message = String.Format("Field declaration '{0}' can be improved by changing its type.", "static readonly Byte[] test = new Byte[1] { 1 };"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 13)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void PropertyDeclaration_SingleFix()
        {

            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            static readonly byte[] test = new byte[1] { 1 };
        }
    }";


            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            static ReadOnlySpan<byte> test => new byte[1] { 1 };
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new PreferReadOnlySpansOverByteArraysCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PreferReadOnlySpansOverByteArraysAnalyzer();
        }
    }
}
