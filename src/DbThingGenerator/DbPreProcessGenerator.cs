using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DbThingGenerator;

[Generator]
public class DbPreProcessGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Syntax provider to identify candidate classes
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(c => c is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
        
        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            var x = new DiagnosticDescriptor(
                id: "DBGEN001",
                title: "Source Generator Starting!!!",
                messageFormat: $"Starting!!!!",
                category: "DbModelGenerator",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true
            );
            spc.ReportDiagnostic(Diagnostic.Create(x, Location.None));
            
            var (compilation, classes) = source;

            foreach (var classDecl in classes)
            {
                var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                
                var descriptor = new DiagnosticDescriptor(
                    id: "DBGEN001",
                    title: "Source Generator Output",
                    messageFormat: $"Generated Initialize method for class '{symbol?.Name}'",
                    category: "DbModelGenerator",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true
                );

                spc.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));

                if (symbol is null || !symbol.Interfaces.Any(i => i.Name == "IDbPreProcessModel"))
                    continue;

                var properties = symbol.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name.StartsWith("DbColumn") == true))
                    .ToList();

                var sourceText = GenerateInitializeMethod(symbol, properties);
                spc.AddSource($"{symbol.Name}_DbModel.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        });
    }

    private string GenerateInitializeMethod(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties)
    {
        var sb = new StringBuilder();
        var ns = classSymbol.ContainingNamespace.ToDisplayString();

        sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {classSymbol.Name}");
        sb.AppendLine("    {");
        sb.AppendLine("        public void Initialize(Dictionary<string, object> values)");
        sb.AppendLine("        {");

        foreach (var prop in properties)
        {
            var attr = prop.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("DbColumn") == true);

            if (attr is null) continue;

            var columnName = attr.NamedArguments.FirstOrDefault(kv => kv.Key == "columnName").Value.Value as string
                ?? prop.Name.ToUpperInvariant();

            var typeArg = attr.AttributeClass!.TypeArguments.FirstOrDefault();
            var typeName = typeArg?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            sb.AppendLine($"            {prop.Name} = values.TryGetRequired<{typeName}>(\"{columnName}\");");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}