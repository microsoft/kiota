using System;

namespace Kiota.Builder;
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
    ResponseHandler,
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
}

public class CodeParameter : CodeTerminalWithKind<CodeParameterKind>, ICloneable, IDocumentedElement
{
    private CodeTypeBase type;
    public CodeTypeBase Type {get => type; set {
        EnsureElementsAreChildren(type);
        type = value;
    }}
    public bool Optional {get;set;}= false;
    public string Description {get; set;}
    public string DefaultValue {get; set;}
    public string SerializationName { get; set; }
    public object Clone()
    {
        return new CodeParameter {
            Optional = Optional,
            Kind = Kind,
            Name = Name.Clone() as string,
            Type = Type?.Clone() as CodeTypeBase,
            Description = Description?.Clone() as string,
            DefaultValue = DefaultValue?.Clone() as string,
            Parent = Parent,
            SerializationName = SerializationName?.Clone() as string,
        };
    }
}
