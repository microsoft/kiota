using System;
using System.Collections.Concurrent;
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
    /// <summary>
    /// The method to be used during deserialization with the discriminator property to get a new instance of the target type.
    /// </summary>
    Factory,
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
    private readonly ConcurrentDictionary<string, CodeParameter> parameters = new ();
    public void RemoveParametersByKind(params CodeParameterKind[] kinds) {
        parameters.Where(p => p.Value.IsOfKind(kinds))
                            .Select(x => x.Key)
                            .ToList()
                            .ForEach(x => parameters.Remove(x, out var _));
    }

    public void ClearParameters()
    {
        parameters.Clear();
    }
    public IEnumerable<CodeParameter> Parameters { get => parameters.Values; }
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
    public ConcurrentDictionary<string, CodeTypeBase> ErrorMappings { get; set; } = new ();
    /// <summary>
    /// Gets/Sets the discriminator values for the class where the key is the value as represented in the payload.
    /// </summary>
    public ConcurrentDictionary<string, CodeTypeBase> DiscriminatorMappings { get; set; } = new();
    /// <summary>
    /// Gets/Sets the name of the property to use for discrimination during deserialization.
    /// </summary>
    public string DiscriminatorPropertyName { get; set; } 

    public bool ShouldWriteDiscriminatorSwitch { get {
        return !string.IsNullOrEmpty(DiscriminatorPropertyName) && DiscriminatorMappings.Any();
    } }

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
            ErrorMappings = ErrorMappings == null ? null : new (ErrorMappings),
            DiscriminatorMappings = DiscriminatorMappings == null ? null : new (DiscriminatorMappings),
            DiscriminatorPropertyName = DiscriminatorPropertyName?.Clone() as string
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
        methodParameters.ToList().ForEach(x => parameters.TryAdd(x.Name, x));
    }
}
