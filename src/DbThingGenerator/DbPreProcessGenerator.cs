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

        var nonGlobalNamespace = !string.IsNullOrEmpty(ns) && ns != "<global namespace>";
        if (nonGlobalNamespace)
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }
        
        sb.AppendLine($"    using DbThing;");
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

            var columnName = prop.Name.ToUpperInvariant();
            var required = false;

            if (attr.ConstructorArguments.Length >= 1 && attr.ConstructorArguments[0].Value is string val)
            {
                columnName = val;
            }

            if (attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is bool req)
            {
                required = req;
            }

            // Allow override via named arguments just in case
            if (attr.NamedArguments.Any(kv => kv.Key == "ColumnName"))
            {
                columnName = attr.NamedArguments.First(kv => kv.Key == "ColumnName").Value.Value as string ?? columnName;
            }
            if (attr.NamedArguments.Any(kv => kv.Key == "Required"))
            {
                required = attr.NamedArguments.First(kv => kv.Key == "Required").Value.Value as bool? ?? required;
            }

            sb.AppendLine($"            {prop.Name} = values.TryGet{(required ? "Required" : string.Empty)}<{typeName}>(\"{columnName}\");");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");

        if (nonGlobalNamespace)
        {
            sb.AppendLine("}"); 
        }

        return sb.ToString();
    }
}