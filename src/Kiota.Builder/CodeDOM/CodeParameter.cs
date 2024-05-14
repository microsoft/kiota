using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;
public enum CodeParameterKind
{
    Custom,
    ///<summary>
    /// The request query parameters when used as a executor/generator parameter. Most languages use the intermediate RequestConfiguration wrapper class.
    ///</summary>
    QueryParameter,
    ///<summary>
    /// The request headers when used as a executor/generator parameter. Most languages use the intermediate RequestConfiguration wrapper class.
    ///</summary>
    Headers,
    RequestBody,
    SetterValue,
    RequestAdapter,
    /// <summary>
    /// The set of parameters to be carried over to the next request builder.
    /// </summary>
    PathParameters,
    ///<summary>
    /// The request middleware options when used as a executor/generator parameter. Most languages use the intermediate RequestConfiguration wrapper class.
    ///</summary>
    Options,
    Serializer,
    BackingStore,
    /// <summary>
    /// A single parameter to be provided by the SDK user which will be added to the path parameters.
    /// </summary>
    Path,
    RawUrl,
    /// <summary>
    /// A single parameter to be provided by the SDK user which can be used to cancel requests.
    /// </summary>
    Cancellation,
    /// <summary>
    /// A parameter representing the parse node to be used for deserialization during discrimination.
    /// </summary>
    ParseNode,
    /// <summary>
    /// Parameter representing the original name of the query parameter symbol in the generated class.
    /// </summary>
    QueryParametersMapperParameter,
    /// <summary>
    /// Configuration for the request to be sent with the headers, query parameters, and middleware options
    /// </summary>
    RequestConfiguration,
    /// <summary>
    /// The content type of the request body when it couldn't be inferred from the description.
    /// </summary>
    RequestBodyContentType,
    /// <summary>
    /// When the deserialization method is replaced as a function, this is the parameter representing instance we're deserializing into.
    /// </summary>
    DeserializationTarget,
}

public class CodeParameter : CodeTerminalWithKind<CodeParameterKind>, ICloneable, IDocumentedElement, IDeprecableElement
{
#nullable disable // exposing property is required
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
    public bool Optional
    {
        get; set;
    }
    [JsonIgnore]
    public CodeDocumentation Documentation { get; set; } = new();
    public string DefaultValue { get; set; } = string.Empty;
    public string SerializationName { get; set; } = string.Empty;
    [JsonIgnore]
    public DeprecationInformation? Deprecation
    {
        get;
        set;
    }

    public IList<string> PossibleValues { get; init; } = new List<string>();

    public object Clone()
    {
        return new CodeParameter
        {
            Optional = Optional,
            Kind = Kind,
            Name = Name,
            DefaultValue = DefaultValue,
            Parent = Parent,
            SerializationName = SerializationName,
            Documentation = (CodeDocumentation)Documentation.Clone(),
            Type = (CodeTypeBase)Type.Clone(),
            Deprecation = Deprecation,
            PossibleValues = PossibleValues.ToList()
        };
    }
}
