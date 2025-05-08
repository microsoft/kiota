using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.AL;
public class ALConventionService : CommonLanguageConventionService // This is currently based on the CSharp-file, needs to be modified for AL
{
    public string ModelCodeunitJsonBodyVariableName { get; } = "JsonBody";
    public override string StreamTypeName => "stream";
    public override string VoidTypeName => "void";
    public override string DocCommentPrefix => "/// ";
    public override string ParseNodeInterfaceName => "IParseNode";
    public override string TempDictionaryVarName => "urlTplParams";
    private const string ReferenceTypePrefix = "<see cref=\"";
    private const string ReferenceTypeSuffix = "\"/>";
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Internal => "internal ",
            AccessModifier.Public => "", // public is the default
            AccessModifier.Protected => throw new InvalidOperationException("AL does not support protected access modifier"),
            _ => "local ",
        };
    }
#pragma warning disable S1006 // Method overrides should not change parameter defaults
    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "<summary>", string suffix = "</summary>")
#pragma warning restore S1006 // Method overrides should not change parameter defaults
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (element is not CodeElement codeElement) return false;
        if (!element.Documentation.DescriptionAvailable) return false;
        var description = element.Documentation.GetDescription(type => GetTypeStringForDocumentation(type), normalizationFunc: static x => x.CleanupXMLString());
        writer.WriteLine($"{DocCommentPrefix}{prefix}{description}{suffix}");
        return true;
    }
    public bool WriteLongDescription(IDocumentedElement element, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (element.Documentation is not { } documentation) return false;
        if (element is not CodeElement codeElement) return false;
        if (documentation.DescriptionAvailable || documentation.ExternalDocumentationAvailable)
        {
            writer.WriteLine($"{DocCommentPrefix}<summary>");
            if (documentation.DescriptionAvailable)
            {
                var description = element.Documentation.GetDescription(type => GetTypeStringForDocumentation(type), normalizationFunc: static x => x.CleanupXMLString());
                writer.WriteLine($"{DocCommentPrefix}{description}");
            }
            if (documentation.ExternalDocumentationAvailable)
                writer.WriteLine($"{DocCommentPrefix}{documentation.DocumentationLabel} <see href=\"{documentation.DocumentationLink}\" />");
            writer.WriteLine($"{DocCommentPrefix}</summary>");
            return true;
        }
        return false;
    }
    public string GetTypeStringForDocumentation(CodeTypeBase code)
    {
        var typeString = GetTypeString(code, true); // don't include nullable markers
        if (typeString.EndsWith('>'))
            return typeString.CleanupXMLString(); // don't generate cref links for generic types as concrete types generate invalid links

        return $"{ReferenceTypePrefix}{typeString.CleanupXMLString()}{ReferenceTypeSuffix}";
    }
    public string GetTypeString(CodeTypeBase code, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        return GetTypeString(code, null, includeCollectionInformation, writer);
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement? targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        return GetTypeString(code, includeCollectionInformation);
    }
    public bool IsPrimitiveType(CodeTypeBase code)
    {
        if (IsEnumType(code))
            return false;
        if (IsCodeunitType(code))
            return false;
        return true;
    }
    public bool IsEnumType(CodeTypeBase code)
    {
        if (code is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum)
                return true;
        }
        return false;
    }
    public bool IsCodeunitType(CodeTypeBase code)
    {
        if (code is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeClass codeClass)
                return true;
        }
        return false;
    }
    public string GetTypeString(CodeTypeBase code, bool includeCollectionInformation)
    {
        if (code is CodeComposedTypeBase)
            throw new InvalidOperationException($"AL does not support union types, the union type {code.Name} should have been filtered out by the refiner");
        if (code is CodeType currentType)
        {
            var typeName = TranslateType(currentType);
            var collectionPrefix = currentType.CollectionKind != CodeTypeCollectionKind.None && includeCollectionInformation ? "List of [" : string.Empty;
            var collectionSuffix = currentType.CollectionKind switch
            {
                CodeTypeCollectionKind.Array when includeCollectionInformation => "]", // Arrays will also be handled as complex types
                CodeTypeCollectionKind.Complex when includeCollectionInformation => "]",
                _ => string.Empty,
            };
            return $"{collectionPrefix}{typeName}{collectionSuffix}";
        }

        throw new InvalidOperationException($"type of type {code?.GetType()} is unknown");
    }

    public override string TranslateType(CodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.TypeDefinition is ITypeDefinition typeDefinition)
            return typeDefinition.GetFullName();

        return type.Name switch
        {
            "integer" => "Integer",
            "boolean" => "Boolean",
            "string" => "Text",
            "int64" => "BigInteger",
            "sbyte" or "byte" => "Byte",
            "float" or "double" or "decimal" => "Decimal",
            "binary" or "base64" or "base64url" => "byte", // TODO-SF: this was byte[] (copied from CSharpConventionService) but AL does not support byte[] in the same way
            "date" => "Date",
            "time" => "Time",
            "datetime" or "DateTimeOffset" => "DateTime",
            "void" => String.Empty,
            _ => type.Name.ToFirstCharacterUpperCase()
        };
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var parameterType = GetTypeString(parameter.Type, targetElement);
        return $"{parameter.Name} : {parameterType}";
    }
    private string GetDeprecationInformation(IDeprecableElement element)
    {
        if (element.Deprecation is null || !element.Deprecation.IsDeprecated) return string.Empty;

        var versionComment = string.IsNullOrEmpty(element.Deprecation.Version) ? string.Empty : $" as of {element.Deprecation.Version}";
        var dateComment = element.Deprecation.Date is null ? string.Empty : $" on {element.Deprecation.Date.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var removalComment = element.Deprecation.RemovalDate is null ? string.Empty : $" and will be removed {element.Deprecation.RemovalDate.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        return $"[Obsolete(\"{element.Deprecation.GetDescription(type => GetTypeString(type, (element as CodeElement)!).Split('.', StringSplitOptions.TrimEntries)[^1])}{versionComment}{dateComment}{removalComment}\")]";
    }
    internal void WriteDeprecationAttribute(IDeprecableElement element, LanguageWriter writer)
    {
        var deprecationMessage = GetDeprecationInformation(element);
        if (!string.IsNullOrEmpty(deprecationMessage))
            writer.WriteLine(deprecationMessage);
    }
}
