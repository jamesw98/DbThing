using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DbThingGenerator;

[Generator]
public class DbPreProcessGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the generator.
    /// </summary>
    /// <param name="context">The context for the generator.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Syntax provider to identify candidate classes.
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(c => c is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            // Get the compilation and classes.
            var (compilation, classes) = source;
            var dbComplexAttrSymbol = compilation.GetTypeByMetadataName("DbThing.Attributes.DbComplexColumnAttribute");
            var dbColumnAttrSymbol = compilation.GetTypeByMetadataName("DbThing.Attributes.DbColumnAttribute");
            
            foreach (var classDecl in classes)
            {
                // Check if the class implements IDbPreProcessModel.
                var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol || !symbol.Interfaces.Any(i => i.Name == "IDbPreProcessModel"))
                {
                    continue;
                }

                var properties = symbol.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name.StartsWith("Db") == true))
                    .ToList();

                var sourceText = GenerateInitializeMethod(symbol, properties, dbColumnAttrSymbol, dbComplexAttrSymbol);
                spc.AddSource($"{symbol.Name}_DbModel.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        });
    }

    /// <summary>
    /// Generates the Initialize method for the class based on the properties and their attributes.
    /// </summary>
    /// <param name="classSymbol">The class symbol we're working with.</param>
    /// <param name="properties">The properties of the class.</param>
    /// <param name="standardAttrName">The standard column attribute name.</param>
    /// <param name="complexAttrName">The complex column attribute name.</param>
    /// <returns></returns>
    private static string GenerateInitializeMethod(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, INamedTypeSymbol? standardAttrName, INamedTypeSymbol? complexAttrName)
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
            
            // First, check if we need to do a complex object setup.
            var complexAttr = prop.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, complexAttrName));
            if (complexAttr is not null)
            {
                BuildComplex(prop, sb, typeName);
                continue;
            }

            // Then, check if we need to do a standard/primitive object setup. 
            var columnAttr = prop.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, standardAttrName));
            if (columnAttr is not null)
            {
                BuildPrimitive(prop, columnAttr, sb, typeName);
            }
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");

        if (nonGlobalNamespace)
        {
            sb.AppendLine("}"); 
        }

        return sb.ToString();
    }
    
    /// <summary>
    /// Builds a complex object.
    /// </summary>
    /// <param name="prop"></param>
    /// <param name="sb"></param>
    /// <param name="typeName"></param>
    private static void BuildComplex(IPropertySymbol prop, StringBuilder sb, string typeName)
    {
        sb.AppendLine($"            {prop.Name} = new {typeName}();");
        sb.AppendLine($"            {prop.Name}.Initialize(values);");
    }

    /// <summary>
    /// Builds a primitive.
    /// </summary>
    /// <param name="prop">The property we're looking at.</param>
    /// <param name="attr">The attribute we're using.</param>
    /// <param name="sb">The StringBuilder to add to.</param>
    /// <param name="typeName">The name of the type to use.</param>
    private static void BuildPrimitive(IPropertySymbol prop, AttributeData attr, StringBuilder sb, string typeName)
    {
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
}