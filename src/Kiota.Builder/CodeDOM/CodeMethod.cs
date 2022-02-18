using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder;

public enum CodeMethodKind
{
    Custom,
    IndexerBackwardCompatibility,
    RequestExecutor,
    RequestGenerator,
    Serializer,
    Deserializer,
    Constructor,
    Getter,
    Setter,
    ClientConstructor,
    RequestBuilderBackwardCompatibility,
    RequestBuilderWithParameters,
    RawUrlConstructor,
    NullCheck,
    CommandBuilder,
}
public enum HttpMethod {
    Get,
    Post,
    Patch,
    Put,
    Delete,
    Options,
    Connect,
    Head,
    Trace
}

public class CodeMethod : CodeTerminal, ICloneable, IDocumentedElement
{
    public HttpMethod? HttpMethod {get;set;}
    public CodeMethodKind MethodKind {get;set;} = CodeMethodKind.Custom;
    public string ContentType { get; set; }
    public AccessModifier Access {get;set;} = AccessModifier.Public;
    private CodeTypeBase returnType;
    public CodeTypeBase ReturnType {get => returnType;set {
        EnsureElementsAreChildren(value);
        returnType = value;
    }}
    private readonly List<CodeParameter> parameters = new ();
    public void RemoveParametersByKind(params CodeParameterKind[] kinds) {
        parameters.RemoveAll(p => p.IsOfKind(kinds));
    }

    public void ClearParameters()
    {
        parameters.Clear();
    }
    public IEnumerable<CodeParameter> Parameters { get => parameters; }
    public bool IsStatic {get;set;} = false;
    public bool IsAsync {get;set;} = true;
    public string Description {get; set;}
    /// <summary>
    /// The property this method accesses to when it's a getter or setter.
    /// </summary>
    public CodeProperty AccessedProperty { get; set;
    }
    /// <summary>
    /// The combination of the path and query parameters for the current URL.
    /// Only use this property if the language you are generating for doesn't support fluent API style (e.g. Shell/CLI)
    /// </summary>
    public IEnumerable<CodeParameter> PathAndQueryParameters
    {
        get; private set;
    }
    public void AddPathOrQueryParameter(params CodeParameter[] parameters)
    {
        if (parameters == null || !parameters.Any()) return;
        foreach (var parameter in parameters)
        {
            EnsureElementsAreChildren(parameter);
        }
        if (PathAndQueryParameters == null)
            PathAndQueryParameters = new List<CodeParameter>(parameters);
        else if (PathAndQueryParameters is List<CodeParameter> cast)
            cast.AddRange(parameters);
    }
    public bool IsOfKind(params CodeMethodKind[] kinds) {
        return kinds?.Contains(MethodKind) ?? false;
    }
    public bool IsAccessor { 
        get => IsOfKind(CodeMethodKind.Getter, CodeMethodKind.Setter);
    }
    public bool IsSerializationMethod {
        get => IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer);
    }
    public List<string> SerializerModules { get; set; }
    public List<string> DeserializerModules { get; set; }
    /// <summary>
    /// Indicates whether this method is an overload for another method.
    /// </summary>
    public bool IsOverload { get { return OriginalMethod != null; } }
    /// <summary>
    /// Provides a reference to the original method that this method is an overload of.
    /// </summary>
    public CodeMethod OriginalMethod { get; set; }
    /// <summary>
    /// The original indexer codedom element this method replaces when it is of kind IndexerBackwardCompatibility.
    /// </summary>
    public CodeIndexer OriginalIndexer { get; set; }
    /// <summary>
    /// The base url for every request read from the servers property on the description.
    /// Only provided for constructor on Api client
    /// </summary>
    public string BaseUrl { get; set;
    }

    /// <summary>
    /// This is currently used for CommandBuilder methods to get the original name without the Build prefix & Command suffix.
    /// Avoids regex operations
    /// </summary>
    public string SimpleName { get; set; } = String.Empty;

    /// <summary>
    /// Mapping of the error code and response types for this method.
    /// </summary>
    public Dictionary<string, CodeTypeBase> ErrorMappings { get; set; } = new ();

    public object Clone()
    {
        var method = new CodeMethod {
            MethodKind = MethodKind,
            ReturnType = ReturnType?.Clone() as CodeTypeBase,
            Name = Name.Clone() as string,
            HttpMethod = HttpMethod,
            IsAsync = IsAsync,
            Access = Access,
            IsStatic = IsStatic,
            Description = Description?.Clone() as string,
            ContentType = ContentType?.Clone() as string,
            BaseUrl = BaseUrl?.Clone() as string,
            AccessedProperty = AccessedProperty,
            SerializerModules = SerializerModules == null ? null : new (SerializerModules),
            DeserializerModules = DeserializerModules == null ? null : new (DeserializerModules),
            OriginalMethod = OriginalMethod,
            Parent = Parent,
            OriginalIndexer = OriginalIndexer,
            ErrorMappings = ErrorMappings == null ? null : new (ErrorMappings)
        };
        if(Parameters?.Any() ?? false)
            method.AddParameter(Parameters.Select(x => x.Clone() as CodeParameter).ToArray());
        return method;
    }

    public void AddParameter(params CodeParameter[] methodParameters)
    {
        if(methodParameters == null || methodParameters.Any(x => x == null))
            throw new ArgumentNullException(nameof(methodParameters));
        if(!methodParameters.Any())
            throw new ArgumentOutOfRangeException(nameof(methodParameters));
        EnsureElementsAreChildren(methodParameters);
        parameters.AddRange(methodParameters);
    }
}
