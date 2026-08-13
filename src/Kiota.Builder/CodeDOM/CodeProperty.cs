using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public enum CodePropertyKind
{
    Custom,
    RequestBuilder,
    AdditionalData,
    BackingStore,
    UrlTemplate,
    /// <summary>
    /// The set of parameters to be carried over to the next request builder.
    /// </summary>
    PathParameters,
    RequestAdapter,
    /// <summary>
    /// The request body. Used when request parameters are wrapped in a class.
    /// </summary>
    RequestBody,
    /// <summary>
    /// A request query parameter. Property of the query parameters class.
    /// </summary>
    QueryParameter,
    /// <summary>
    /// The request query parameters. Used when request parameters are wrapped in a class.
    /// </summary>
    QueryParameters,
    /// <summary>
    /// The request headers. Used when request parameters are wrapped in a class.
    /// </summary>
    Headers,
    /// <summary>
    /// The request middleware options. Used when request parameters are wrapped in a class.
    /// </summary>
    Options,
    /// <summary>
    /// The override for the error message for the error/exception type.
    /// </summary>
    ErrorMessageOverride
}

public class CodeProperty : CodeTerminalWithKind<CodePropertyKind>, IDocumentedElement, IAlternativeName, ICloneable, IDeprecableElement, IAccessibleElement
{
    public bool ReadOnly
    {
        get; set;
    }
    public AccessModifier Access { get; set; } = AccessModifier.Public;
    public bool ExistsInBaseType => OriginalPropertyFromBaseType != null;
    public bool ExistsInExternalBaseType
    {
        get; set;
    }
    public CodeMethod? Getter
    {
        get; set;
    }
    public CodeMethod? Setter
    {
        get; set;
    }
    public CodeMethod? GetterFromCurrentOrBaseType
    {
        get
        {
            if (Getter != null)
                return Getter;
            if (ExistsInBaseType)
                return OriginalPropertyFromBaseType?.Getter;
            return default;
        }
    }
    public CodeMethod? SetterFromCurrentOrBaseType
    {
        get
        {
            if (Setter != null)
                return Setter;
            if (ExistsInBaseType)
                return OriginalPropertyFromBaseType?.Setter;
            return default;
        }
    }
#nullable disable // the backing property is required
    private CodeTypeBase type;
#nullable enable
    public required CodeTypeBase Type
    {
        get => type; set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            type = value;
        }
    }
    public string DefaultValue { get; set; } = string.Empty;
    public CodeDocumentation Documentation { get; set; } = new();
    /// <inheritdoc/>
    public string SerializationName { get; set; } = string.Empty;
    public string NamePrefix { get; set; } = string.Empty;
    /// <inheritdoc/>
    public bool IsNameEscaped
    {
        get => !string.IsNullOrEmpty(SerializationName);
    }
    /// <inheritdoc/>
    public string WireName => IsNameEscaped ? SerializationName : Name;
    public CodeProperty? OriginalPropertyFromBaseType
    {
        get; set;
    }
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
    /// <summary>
    /// Indicates if the property is the primary error message for the error/exception type.
    /// </summary>
    public bool IsPrimaryErrorMessage
    {
        get; set;
    }

    public object Clone()
    {
        var property = new CodeProperty
        {
            Name = Name,
            Kind = Kind,
            Parent = Parent,
            ReadOnly = ReadOnly,
            Access = Access,
            ExistsInExternalBaseType = ExistsInExternalBaseType,
            Getter = Getter?.Clone() as CodeMethod,
            Setter = Setter?.Clone() as CodeMethod,
            Type = (CodeTypeBase)Type.Clone(),
            DefaultValue = DefaultValue,
            Documentation = (CodeDocumentation)Documentation.Clone(),
            SerializationName = SerializationName,
            NamePrefix = NamePrefix,
            OriginalPropertyFromBaseType = OriginalPropertyFromBaseType?.Clone() as CodeProperty,
            Deprecation = Deprecation,
            IsPrimaryErrorMessage = IsPrimaryErrorMessage,
        };
        return property;
    }
}

