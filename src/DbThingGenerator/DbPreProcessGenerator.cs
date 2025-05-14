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
            var (compilation, classes) = source;

            foreach (var classDecl in classes)
            {
                var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol || !symbol.Interfaces.Any(i => i.Name == "IDbPreProcessModel"))
                {
                    continue;
                }

                var properties = symbol.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name.StartsWith("DbColumn") == true))
                    .ToList();

                var sourceText = GenerateInitializeMethod(symbol, properties);
                spc.AddSource($"{symbol.Name}_DbModel.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        });
    }

    private static string GenerateInitializeMethod(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties)
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
            var typeName = prop.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var attr = prop.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("DbColumn") == true);
            if (attr is null)
            {
                continue;
            }

            var columnName = attr.NamedArguments.FirstOrDefault(kv => kv.Key == "ColumnName").Value.Value as string
                ?? prop.Name.ToUpperInvariant();
            
            var required = attr.NamedArguments
                .FirstOrDefault(kv => kv.Key == "Required").Value.Value as bool? ?? false;

            sb.AppendLine($"            {prop.Name} = values.TryGet{(required ? "Required" : string.Empty)}<{typeName}>(\"{columnName}\");");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}