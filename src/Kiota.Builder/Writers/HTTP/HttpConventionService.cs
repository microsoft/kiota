using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Http;

public class HttpConventionService : CommonLanguageConventionService
{
    public HttpConventionService()
    {
    }
    public override string StreamTypeName => "stream";
    public override string VoidTypeName => "void";
    public override string DocCommentPrefix => "###";
    public static string NullableMarkerAsString => "?";
    public override string ParseNodeInterfaceName => "ParseNode";
    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Documentation.DescriptionAvailable) return false;
        if (element is not CodeElement codeElement) return false;

        var description = element.Documentation.GetDescription(type => GetTypeString(type, codeElement));
        writer.WriteLine($"{DocCommentPrefix} {prefix}{description}{prefix}");

        return true;
    }
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => "public",
            AccessModifier.Protected => "internal",
            _ => "private",
        };
    }
    public override string TempDictionaryVarName => "urlTplParams";
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        if (code is CodeType currentType)
        {
            var typeName = TranslateType(currentType);
            var nullableSuffix = code.IsNullable ? NullableMarkerAsString : string.Empty;
            var collectionPrefix = currentType.IsCollection && includeCollectionInformation ? "[" : string.Empty;
            var collectionSuffix = currentType.IsCollection && includeCollectionInformation ? $"]{nullableSuffix}" : string.Empty;
            if (currentType.IsCollection && !string.IsNullOrEmpty(nullableSuffix))
                nullableSuffix = string.Empty;

            if (currentType.ActionOf)
                return $"({collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}>) -> Void";
            return $"{collectionPrefix}{typeName}{nullableSuffix}{collectionSuffix}";
        }

        throw new InvalidOperationException($"type of type {code?.GetType()} is unknown");
    }
    public override string TranslateType(CodeType type)
    {
        return type?.Name switch
        {
            "integer" => "Int32",
            "boolean" => "Bool",
            "float" => "Float32",
            "long" => "Int64",
            "double" or "decimal" => "Float64",
            "guid" => "UUID",
            "void" or "uint8" or "int8" or "int32" or "int64" or "float32" or "float64" or "string" => type.Name.ToFirstCharacterUpperCase(),
            "binary" or "base64" or "base64url" => "[UInt8]",
            "DateTimeOffset" => "Date",
            null => "object",
            _ => type.Name.ToFirstCharacterUpperCase() is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "object",
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var parameterType = GetTypeString(parameter.Type, targetElement);
        var defaultValue = parameter switch
        {
            _ when !string.IsNullOrEmpty(parameter.DefaultValue) => $" = {parameter.DefaultValue}",
            _ when parameter.Optional => " = default",
            _ => string.Empty,
        };
        return $"{parameter.Name.ToFirstCharacterLowerCase()} : {parameterType}{defaultValue}";
    }

    /// <summary>
    /// Gets the default value for the given property.
    /// </summary>
    /// <param name="codeProperty">The property to get the default value for.</param>
    /// <returns>The default value as a string.</returns>
    public static string GetDefaultValueForProperty(CodeProperty codeProperty)
    {
        return codeProperty?.Type.Name switch
        {
            "int" or "integer" => "0",
            "string" => "\"string\"",
            "bool" or "boolean" => "false",
            _ when codeProperty?.Type is CodeType enumType && enumType.TypeDefinition is CodeEnum enumDefinition =>
                enumDefinition.Options.FirstOrDefault()?.Name is string enumName ? $"\"{enumName}\"" : "null",
            _ => "null"
        };
    }
}
