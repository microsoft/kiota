using Kiota.Builder.Extensions;

namespace Kiota.Builder;
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
    /// The request response handler. Used when request parameters are wrapped in a class.
    /// </summary>
    ResponseHandler,
}

public class CodeProperty : CodeTerminalWithKind<CodePropertyKind>, IDocumentedElement
{
    public bool ReadOnly {get;set;} = false;
    public AccessModifier Access {get;set;} = AccessModifier.Public;
    private CodeTypeBase type;
    public CodeMethod Getter {get; set;}
    public CodeMethod Setter {get; set;}
    public CodeTypeBase Type {get => type ;set {
        EnsureElementsAreChildren(value);
        type = value;
    }}
    public string DefaultValue {get;set;}
    public string Description {get; set;}
    public string SerializationName { get; set; }
    public string NamePrefix { get; set; }
    public bool IsNameEscaped { get => !string.IsNullOrEmpty(SerializationName); }
    public string SymbolName { get => IsNameEscaped ? SerializationName.CleanupSymbolName() : Name; }
}
