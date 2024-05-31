using System;
using System.Text;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;

internal static class TypeDefinitionExtensions
{
    public static string GetFullName(this ITypeDefinition typeDefinition)
    {
        ArgumentNullException.ThrowIfNull(typeDefinition);

        var fullNameBuilder = new StringBuilder();
        return AppendTypeName(typeDefinition, fullNameBuilder).ToString();
    }

    private static StringBuilder AppendTypeName(ITypeDefinition typeDefinition, StringBuilder fullNameBuilder)
    {
        if (string.IsNullOrEmpty(typeDefinition.Name))
            throw new ArgumentException("Cannot append a full name for a type without a name.", nameof(typeDefinition));

        fullNameBuilder.Insert(0, typeDefinition.Name.ToFirstCharacterUpperCase());
        if (typeDefinition.Parent is null)
            return fullNameBuilder;

        if (typeDefinition.Parent is ITypeDefinition parentTypeDefinition)
        {
            fullNameBuilder.Insert(0, '.');
            return AppendTypeName(parentTypeDefinition, fullNameBuilder);
        }
        else if (typeDefinition.Parent is CodeNamespace codeNamespace)
        {
            if (!string.IsNullOrEmpty(codeNamespace.Name))
                fullNameBuilder.Insert(0, $"{codeNamespace.Name}.");

            return fullNameBuilder;
        }
        else
        {
            throw new InvalidOperationException($"Type {typeDefinition.Name} contains an invalid parent of type {typeDefinition.Parent.GetType().FullName}.");
        }
    }
}
