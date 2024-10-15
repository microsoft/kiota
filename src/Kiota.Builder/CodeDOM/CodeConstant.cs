using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public class CodeConstant : CodeTerminalWithKind<CodeConstantKind>, IDocumentedElement
{
    public BlockDeclaration StartBlock { get; set; } = new();
    public void AddUsing(params CodeUsing[] codeUsings) => StartBlock.AddUsings(codeUsings);
    public CodeElement? OriginalCodeElement
    {
        get;
        set;
    }
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string? UriTemplate
    {
        get; init;
    }
    /// <inheritdoc/>
    public CodeDocumentation Documentation
    {
        get; set;
    } = new();
#pragma warning restore CA1056 // URI-like properties should not be strings
    public static CodeConstant? FromQueryParametersMapping(CodeInterface source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind is not CodeInterfaceKind.QueryParameters) throw new InvalidOperationException("Cannot create a query parameters constant from a non query parameters interface");
        if (!source.Properties.Any(static x => !string.IsNullOrEmpty(x.SerializationName))) return default;
        var result = new CodeConstant
        {
            Name = $"{source.Name.ToFirstCharacterLowerCase()}Mapper",
            Kind = CodeConstantKind.QueryParametersMapper,
            OriginalCodeElement = source,
        };
        result.Documentation.DescriptionTemplate = "Mapper for query parameters from symbol name to serialization name represented as a constant.";
        return result;
    }
    public static CodeConstant? FromCodeEnum(CodeEnum source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new CodeConstant
        {
            Name = $"{source.Name.ToFirstCharacterLowerCase()}Object",
            Kind = CodeConstantKind.EnumObject,
            OriginalCodeElement = source,
        };
    }
    internal const string UriTemplateSuffix = "UriTemplate";
    internal const string RequestsMetadataSuffix = "RequestsMetadata";
    internal const string NavigationMetadataSuffix = "NavigationMetadata";
    public static CodeConstant? FromRequestBuilderClassToUriTemplate(CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder) return default;
        if (codeClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException($"Couldn't find the url template property for class {codeClass.Name}");
        var result = new CodeConstant
        {
            Name = $"{codeClass.Name.ToFirstCharacterLowerCase()}{UriTemplateSuffix}",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = urlTemplateProperty.DefaultValue,
            OriginalCodeElement = codeClass
        };
        result.Documentation.DescriptionTemplate = "Uri template for the request builder.";
        return result;
    }
    public static CodeConstant? FromRequestBuilderToNavigationMetadata(CodeClass codeClass, CodeUsing[]? usingsToAdd = default)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder) return default;
        if (!(codeClass.Properties.Any(static x => x.Kind is CodePropertyKind.RequestBuilder) ||
            codeClass.Methods.Any(static x => x.Kind is CodeMethodKind.IndexerBackwardCompatibility or CodeMethodKind.RequestBuilderWithParameters)))
            return default;
        var result = new CodeConstant
        {
            Name = $"{codeClass.Name.ToFirstCharacterLowerCase()}{NavigationMetadataSuffix}",
            Kind = CodeConstantKind.NavigationMetadata,
            OriginalCodeElement = codeClass,
        };
        result.Documentation.DescriptionTemplate = "Metadata for all the navigation properties in the request builder.";
        if (usingsToAdd is { Length: > 0 } usingsToAddList)
            result.AddUsing(usingsToAddList);
        return result;
    }
    public static CodeConstant? FromRequestBuilderToRequestsMetadata(CodeClass codeClass, CodeUsing[]? usingsToAdd = default)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder) return default;
        if (!codeClass.Methods.Any(static x => x.Kind is CodeMethodKind.RequestExecutor or CodeMethodKind.RequestGenerator))
            return default;
        var result = new CodeConstant
        {
            Name = $"{codeClass.Name.ToFirstCharacterLowerCase()}{RequestsMetadataSuffix}",
            Kind = CodeConstantKind.RequestsMetadata,
            OriginalCodeElement = codeClass,
            UriTemplate = $"{codeClass.Name.ToFirstCharacterLowerCase()}{UriTemplateSuffix}",
        };
        result.Documentation.DescriptionTemplate = "Metadata for all the requests in the request builder.";
        if (usingsToAdd is { Length: > 0 } usingsToAddList)
            result.AddUsing(usingsToAddList);
        return result;
    }
}
public enum CodeConstantKind
{
    /// <summary>
    /// Mapper for query parameters from symbol name to serialization name represented as a constant.
    /// </summary>
    QueryParametersMapper,
    /// <summary>
    /// Enum mapping keys represented as a constant.
    /// </summary>
    EnumObject,
    /// <summary>
    /// Uri template for the request builder.
    /// </summary>
    UriTemplate,
    /// <summary>
    /// Indexer methods, builders with parameters, and request builder properties represented as a single constant for proxy generation.
    /// </summary>
    NavigationMetadata,
    /// <summary>
    /// Request generators and executors represented as a single constant for proxy generation.
    /// </summary>
    RequestsMetadata,
}
