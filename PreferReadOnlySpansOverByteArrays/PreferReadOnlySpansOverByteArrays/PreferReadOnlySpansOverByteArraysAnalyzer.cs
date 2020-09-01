using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PreferReadOnlySpansOverByteArrays
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferReadOnlySpansOverByteArraysAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PreferReadOnlySpansOverByteArrays";
        static readonly string[] validTypes = new string[] { "Byte","SByte", "Bool" };
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.FieldDeclaration);
        }

        static bool IsPrimitiveTypeArray(ITypeSymbol elementType)
        {
            if (elementType is null)
                return false;
            return validTypes.Contains(elementType.Name);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var propSyntax = context.Node as FieldDeclarationSyntax;

            if(propSyntax.Modifiers.Any(SyntaxKind.StaticKeyword) && propSyntax.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
                && propSyntax.Declaration.Type is ArrayTypeSyntax arrayType //Has to be array property
                && arrayType.RankSpecifiers.Count == 1 //Has to be empty rank initializer and the only one
                && arrayType.RankSpecifiers.First().Sizes[0].Kind() == SyntaxKind.OmittedArraySizeExpression
                && IsPrimitiveTypeArray(context.SemanticModel.GetTypeInfo(arrayType.ElementType).Type))
            {
                var diagnostic = Diagnostic.Create(Rule, propSyntax.GetLocation(), propSyntax);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}