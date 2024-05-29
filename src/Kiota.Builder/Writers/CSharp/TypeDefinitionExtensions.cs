using System;
using System.Text;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;

internal static class TypeDefinitionExtensions
{
    public static string GetFullyQualifiedName(this ITypeDefinition typeDefinition)
    {
        ArgumentNullException.ThrowIfNull(typeDefinition);

        var fullNameBuilder = new StringBuilder();
        return GetFullyQualifiedName(typeDefinition, fullNameBuilder).ToString();
    }

    private static StringBuilder GetFullyQualifiedName(ITypeDefinition codeClass, StringBuilder fullNameBuilder)
    {
        fullNameBuilder.Insert(0, codeClass.Name.ToFirstCharacterUpperCase());
        if (codeClass.Parent is CodeClass parentClass)
        {
            fullNameBuilder.Insert(0, '.');
            return GetFullyQualifiedName(parentClass, fullNameBuilder);
        }
        if (codeClass.Parent is CodeNamespace ns && !string.IsNullOrEmpty(ns.Name))
        {
            fullNameBuilder.Insert(0, '.');
            fullNameBuilder.Insert(0, ns.Name);
        }

        return fullNameBuilder;
    }
}
