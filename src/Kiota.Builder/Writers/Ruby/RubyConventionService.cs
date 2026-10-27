using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby;

public class RubyConventionService : CommonLanguageConventionService
{
    public override string StreamTypeName => "StringIO";
    private const string InternalVoidTypeName = "nil";
    public override string VoidTypeName => InternalVoidTypeName;
    public override string DocCommentPrefix => "# ";
    public override string ParseNodeInterfaceName => "parse_node";
    public override string TempDictionaryVarName => "url_tpl_params";
    // a namespace barrel autoloads these, so their files are only ever loaded through it
    internal static bool IsAutoloaded(CodeElement element) => element switch
    {
        CodeClass codeClass => codeClass.Parent is CodeNamespace ns && !ns.HasClassNamedAfterItself() && codeClass.IsOfKind(CodeClassKind.Model),
        CodeEnum codeEnum => codeEnum.Parent is CodeNamespace ns && !ns.HasClassNamedAfterItself() && codeEnum.Options.Any(),
        _ => false,
    };
    internal const string FrozenStringLiteralComment = "# frozen_string_literal: true";
    // one blank line between class members, none at the start of a block
    internal static void WriteMemberSeparator(LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!writer.IsAtBlockStart) writer.WriteLine();
    }
    // a model with a backing store keeps its values there instead of in instance variables
    internal static string? GetBackingStoreVariable(CodeProperty property) =>
        property.Kind is CodePropertyKind.Custom or CodePropertyKind.AdditionalData &&
        property.Parent is CodeClass parentClass && parentClass.GetBackingStoreProperty() is CodeProperty store ?
            $"@{store.NamePrefix}{store.Name.ToSnakeCase()}" :
            null;
    internal static string ReadProperty(CodeProperty property) =>
        GetBackingStoreVariable(property) is string store ?
            $"{store}.get({ToRubyStringLiteral(property.WireName)})" :
            $"@{property.NamePrefix}{property.Name.ToSnakeCase()}";
    internal static string AssignProperty(CodeProperty property, string value) =>
        GetBackingStoreVariable(property) is string store ?
            $"{store}.set({ToRubyStringLiteral(property.WireName)}, {value})" :
            $"@{property.NamePrefix}{property.Name.ToSnakeCase()} = {value}";
    // a barrel is written, and required, only when it has something to autoload
    internal static bool HasAutoloadedMembers(CodeNamespace codeNamespace) =>
        codeNamespace.Classes.Any(IsAutoloaded) || codeNamespace.Enums.Any(IsAutoloaded);
    // double quotes are the only Ruby quoting in which the shared escapes such as \n and \t mean what they say
    internal static string ToRubyStringLiteral(string? value) =>
        $"\"{(value ?? string.Empty).SanitizeDoubleQuote().Replace("#", "\\#", StringComparison.Ordinal)}\"";
    internal static string SanitizeRubyDoubleQuoteLiteral(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        if (value.Length > 1)
        {
            if (value[0] == '"' && value[^1] == '"')
                return $"\"{value[1..^1].SanitizeDoubleQuote().Replace("#", "\\#", StringComparison.Ordinal)}\"";

            if (value[0] == '\'' && value[^1] == '\'')
                return ToRubyStringLiteral(value[1..^1]);
        }

        return value.SanitizeDoubleQuote().Replace("#", "\\#", StringComparison.Ordinal);
    }
    public override string GetAccessModifier(AccessModifier access)
    {
        return access switch
        {
            AccessModifier.Public => "public",
            AccessModifier.Protected => "protected",
            _ => "private",
        };
    }
    /// <summary>
    /// The one place a parameter becomes a Ruby local. The signature and every site that refers to
    /// a parameter read it from here, so the two cannot disagree.
    /// </summary>
    internal static string GetParameterName(CodeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.Name.ToSnakeCase();
    }
    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var defaultValue = parameter.Optional && (targetElement is not CodeMethod currentMethod || !currentMethod.IsOfKind(CodeMethodKind.Setter)) ?
            $" = {(string.IsNullOrEmpty(parameter.DefaultValue) ? "nil" : SanitizeRubyDoubleQuoteLiteral(parameter.DefaultValue))}" :
            string.Empty;
        return $"{GetParameterName(parameter)}{defaultValue}";
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        if (code is CodeType currentType)
        {
            return $"{TranslateType(currentType)}";
        }

        throw new InvalidOperationException();
    }
    /// <summary>
    /// The single description of a Ruby primitive: the constant that names it, and the parse node
    /// and serialization writer methods that read and write it. Everything that used to decide
    /// those three separately, by matching rendered type names, now reads them from here.
    /// </summary>
    internal sealed record RubyPrimitive(string Constant, string Reader, string Writer);
    private static readonly Dictionary<string, RubyPrimitive> PrimitiveTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "string", new("String", "get_string_value", "write_string_value") },
        { "boolean", new("\"boolean\"", "get_boolean_value", "write_boolean_value") },
        { "number", new("Integer", "get_number_value", "write_number_value") },
        { "float", new("Float", "get_float_value", "write_float_value") },
        { "Guid", new("UUIDTools::UUID", "get_guid_value", "write_guid_value") },
        { "Date", new("Date", "get_date_value", "write_date_value") },
        { "Time", new("Time", "get_time_value", "write_time_value") },
        { "DateTime", new("DateTime", "get_date_time_value", "write_date_time_value") },
        { DurationTypeName, new(DurationTypeName, "get_duration_value", "write_duration_value") },
        // a binary value inside a payload is carried by JSON as a string, unlike a binary request
        // or response body, which the refiner rewrites to the native stream type
        { "binary", new("String", "get_string_value", "write_string_value") },
    };
    internal const string DurationTypeName = "MicrosoftKiotaAbstractions::ISODuration";
    internal static bool IsPrimitiveType(string typeName) => PrimitiveTypes.ContainsKey(typeName ?? string.Empty);
    internal static bool TryGetPrimitiveType(string typeName, out RubyPrimitive primitive) =>
        PrimitiveTypes.TryGetValue(typeName ?? string.Empty, out primitive!);
    /// <summary>
    /// The constant a collection element or a primitive send method names, such as the Integer in
    /// get_collection_of_primitive_values(Integer).
    /// </summary>
    internal static string GetPrimitiveConstant(string typeName) =>
        TryGetPrimitiveType(typeName, out var primitive) ? primitive.Constant : typeName.ToFirstCharacterUpperCase();
    public override string TranslateType(CodeType type)
    {
        // a resolved model or enum keeps the name the builder gave it, so the aliases below only
        // rename types the builder never resolved
        if (type?.TypeDefinition is not null)
            return type.Name.ToFirstCharacterUpperCase();
        return type?.Name switch
        {
            "integer" or "int64" or "int8" or "uint8" or "sbyte" or "byte" => "number",
            "double" or "decimal" => "float",
            "binary" or "base64" or "base64url" => "binary",
            // the refiner normally replaces these, but resolving the aliases here too keeps the
            // rendered name the single key everything else looks up
            "dateTimeOffset" => "DateTime",
            "dateOnly" => "Date",
            "timeOnly" => "Time",
            "timeSpan" => DurationTypeName,
            "float" or "string" or "object" or "boolean" or "void" => type.Name, // little casing hack
            null => "object",
            _ => type.Name.ToFirstCharacterUpperCase() is string typeName && !string.IsNullOrEmpty(typeName) ? typeName : "object",
        };
    }
    public override bool WriteShortDescription(IDocumentedElement element, LanguageWriter writer, string prefix = "", string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Documentation.DescriptionAvailable) return false;
        if (element is not CodeElement codeElement) return false;

        var description = element.Documentation.GetDescription(type => GetTypeString(type, codeElement), normalizationFunc: RemoveInvalidDescriptionCharacters);
        if (string.IsNullOrWhiteSpace(description)) return false;
        writer.WriteLine($"{DocCommentPrefix}{description}");
        return true;
    }
#pragma warning disable CA1822 // Method should be static
    public string GetNormalizedNamespacePrefixForType(CodeTypeBase type)
    {
        if (type is CodeType xType)
            if ((xType.TypeDefinition is CodeClass || xType.TypeDefinition is CodeEnum) &&
                xType.TypeDefinition.Parent is CodeNamespace ns)
                return ns.Name.NormalizeNameSpaceName("::") is string normalized && !string.IsNullOrEmpty(normalized) ?
                    $"{normalized}::" :
                    string.Empty;
            else if (xType.TypeDefinition is CodeType definition && definition.IsExternal && !string.IsNullOrEmpty(definition.Name))
                return $"{definition.Name}::";
        return string.Empty;
    }
    public string GetQualifiedTypeName(CodeTypeBase type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return $"{GetNormalizedNamespacePrefixForType(type)}{type.Name.ToFirstCharacterUpperCase()}";
    }
#pragma warning restore CA1822 // Method should be static
    /// <summary>
    /// The one text of a deprecation, read by the YARD tag and the runtime warning alike: null when
    /// the element is not deprecated, empty when it is deprecated without a word of explanation.
    /// </summary>
    internal string? GetDeprecationMessage(IDeprecableElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.Deprecation is null || !element.Deprecation.IsDeprecated) return null;
        var versionComment = string.IsNullOrEmpty(element.Deprecation.Version) ? string.Empty : $" as of {NormalizeDeprecationText(element.Deprecation.Version)}";
        var dateComment = element.Deprecation.Date is null ? string.Empty : $" on {element.Deprecation.Date.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var removalComment = element.Deprecation.RemovalDate is null ? string.Empty : $" and will be removed {element.Deprecation.RemovalDate.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var description = element.Deprecation.GetDescription(type => GetDeprecationTypeReference(type, (element as CodeElement)!), normalizationFunc: NormalizeDeprecationText);
        return $"{description}{versionComment}{dateComment}{removalComment}".Trim();
    }
    // the description reader XML-escapes deprecation text, Ruby comments and strings want it as written
    private static string NormalizeDeprecationText(string text) => RemoveInvalidDescriptionCharacters(WebUtility.HtmlDecode(text));
    // a replacement method or property is named the way Ruby code calls it
    private string GetDeprecationTypeReference(CodeTypeBase type, CodeElement targetElement) =>
        type is CodeType { TypeDefinition: CodeMethod or CodeProperty } codeType ? codeType.TypeDefinition!.Name.ToSnakeCase() : GetTypeString(type, targetElement);
    internal void WriteDeprecationComment(IDeprecableElement element, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (GetDeprecationMessage(element) is not string message) return;
        writer.WriteLine($"{DocCommentPrefix}@deprecated {message}".TrimEnd());
    }
    // silent unless the program opts in with Warning[:deprecated] = true or -W:deprecated, like Ruby's own deprecations
    internal void WriteDeprecationWarning(IDeprecableElement element, string rubyName, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (GetDeprecationMessage(element) is not string message) return;
        if (string.IsNullOrEmpty(message)) message = $"{rubyName} is deprecated";
        // qualified, so a generated `warn` accessor (a sibling /warn path) cannot shadow it
        writer.WriteLine($"::Kernel.warn({ToRubyStringLiteral(message)}, category: :deprecated, uplevel: 1)");
    }
    internal static string RemoveInvalidDescriptionCharacters(string originalDescription) =>
        string.IsNullOrEmpty(originalDescription) ? string.Empty :
        originalDescription.Replace("\\", "#", StringComparison.OrdinalIgnoreCase)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Trim();
#pragma warning disable CA1822 // Method should be static
    internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string? urlTemplateVarName = default, string? prefix = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp &&
            parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProp)
        {
            var urlTemplateParams = string.IsNullOrEmpty(urlTemplateVarName) ? $"@{pathParametersProp.Name.ToSnakeCase()}" : urlTemplateVarName;
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(GetParameterName))}";
            writer.WriteLine($"{prefix}{returnType}.new({urlTemplateParams}, @{requestAdapterProp.Name.ToSnakeCase()}{pathParametersSuffix})");
        }
    }
#pragma warning restore CA1822 // Method should be static
#pragma warning disable CA1822 // Method should be static
    internal void WriteNamespaceModules(CodeNamespace currentNamespace, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(currentNamespace);
        var namespaceName = currentNamespace.Name.NormalizeNameSpaceName("::");
        var namespaceParts = namespaceName.Split("::", StringSplitOptions.RemoveEmptyEntries);
        foreach (var namespacePart in namespaceParts)
        {
            writer.StartBlock($"module {namespacePart}");
        }
    }
    internal void WriteNamespaceClosing(CodeNamespace currentNamespace, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(currentNamespace);
        var namespaceName = currentNamespace.Name.NormalizeNameSpaceName("::");
        var namespacePartsCount = namespaceName.Split("::", StringSplitOptions.RemoveEmptyEntries).Length;
        for (var i = 0; i < namespacePartsCount; i++)
        {
            writer.CloseBlock("end");
        }
    }
#pragma warning restore CA1822 // Method should be static
}
