using System;
using System.Text;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.AL;

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

        switch (typeDefinition)
        {
            case CodeEnum:
                fullNameBuilder.Append("Enum");
                break;
            case CodeClass:
                fullNameBuilder.Append("Codeunit");
                break;
            default:
                throw new InvalidOperationException($"Type {typeDefinition.Name} is neither a CodeEnum nor a CodeClass.");
        }
        fullNameBuilder.Append(' ');
        fullNameBuilder.Append('"');
        fullNameBuilder.Append(typeDefinition.GetShortName().ToFirstCharacterUpperCase());
        fullNameBuilder.Append('"');
        return fullNameBuilder;
    }
    public static string GetShortName(this ICodeElement codeElement)
    {
        ArgumentNullException.ThrowIfNull(codeElement);

        return GetShortName(codeElement.Name);
    }
    public static string GetShortName(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.Length <= 30 ? input : ShortenName(input);
    }
    private static string ShortenName(string newName)
    {
        ArgumentNullException.ThrowIfNull(newName);
        if (newName.Contains("Extended", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Extended", "Ext", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Message", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Message", "Msg", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Response", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Response", "Rsp", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Property", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Property", "Prop", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Collection", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Collection", "Coll", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Override", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Override", "Ovrd", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Classification", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Classification", "Class", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Request", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Request", "Req", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Builder", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Builder", "Bldr", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Configuration", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Configuration", "Cfg", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Microsoft", "ms", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Parameters", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Parameters", "Params", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Query", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Query", "Qry", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Number", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Number", "Num", StringComparison.OrdinalIgnoreCase);
        if (newName.Contains("Alignment", StringComparison.OrdinalIgnoreCase))
            newName = newName.Replace("Alignment", "Algnmt", StringComparison.OrdinalIgnoreCase);
        if (newName.Length > 30)
            newName = newName.Substring(0, 30);
        return newName;
    }
}