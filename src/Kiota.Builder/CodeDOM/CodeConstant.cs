using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public class CodeConstant : CodeTerminalWithKind<CodeConstantKind>
{
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
#pragma warning restore CA1056 // URI-like properties should not be strings
    public static CodeConstant? FromQueryParametersMapping(CodeInterface source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind is not CodeInterfaceKind.QueryParameters) throw new InvalidOperationException("Cannot create a query parameters constant from a non query parameters interface");
        if (!source.Properties.Any(static x => !string.IsNullOrEmpty(x.SerializationName))) return default;
        return new CodeConstant
        {
            Name = $"{source.Name.ToFirstCharacterLowerCase()}Mapper",
            Kind = CodeConstantKind.QueryParametersMapper,
            OriginalCodeElement = source,
        };
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
    public static CodeConstant? FromRequestBuilderClassToUriTemplate(CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder) return default;
        if (codeClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException($"Couldn't find the url template property for class {codeClass.Name}");
        return new CodeConstant
        {
            Name = $"{codeClass.Name.ToFirstCharacterLowerCase()}UriTemplate",
            Kind = CodeConstantKind.UriTemplate,
            UriTemplate = urlTemplateProperty.DefaultValue,
        };
    }
    public static CodeConstant? FromRequestBuilderToNavigationMetadata(CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder) return default;
        if (!(codeClass.Properties.Any(static x => x.Kind is CodePropertyKind.RequestBuilder) ||
            codeClass.Methods.Any(x => x.Kind is CodeMethodKind.IndexerBackwardCompatibility or CodeMethodKind.RequestBuilderWithParameters)))
            return default;
        return new CodeConstant
        {
            Name = $"{codeClass.Name.ToFirstCharacterLowerCase()}NavigationMetadata",
            Kind = CodeConstantKind.NavigationMetadata,
            OriginalCodeElement = codeClass,
        };
    }
    public static CodeConstant? FromRequestBuilderToRequestsMetadata(CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        if (codeClass.Kind != CodeClassKind.RequestBuilder) return default;
        if (!codeClass.Methods.Any(x => x.Kind is CodeMethodKind.RequestExecutor or CodeMethodKind.RequestGenerator))
            return default;
        return new CodeConstant
        {
            Name = $"{codeClass.Name.ToFirstCharacterLowerCase()}RequestsMetadata",
            Kind = CodeConstantKind.RequestsMetadata,
            OriginalCodeElement = codeClass,
        };
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
