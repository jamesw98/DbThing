using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DbThingGenerator;

public static class Util
{
    public static bool HasAttrib(this GeneratorSyntaxContext context, string attribName, SyntaxList<AttributeListSyntax> attribList)
    {
        foreach (var attrib in attribList.SelectMany(list => list.Attributes))
        {
            if (context.SemanticModel.GetSymbolInfo(attrib).Symbol is not IMethodSymbol att) 
            {
                continue;
            }

            var name = att.ContainingType.ToDisplayString();
            if (name == attribName)
            {
                return true;
            }
        }

        return false;
    }
}