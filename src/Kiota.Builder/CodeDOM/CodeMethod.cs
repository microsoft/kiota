﻿using System;
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
    CommandBuilder,
    /// <summary>
    /// The method to be used during deserialization with the discriminator property to get a new instance of the target type.
    /// </summary>
    Factory,
    /// <summary>
    /// The method to be used during query parameters serialization to get the proper uri template parameter name.
    /// </summary>
    QueryParametersMapper,
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

public class PagingInformation : ICloneable
{
    public string ItemName
    {
        get; set;
    }

    public string NextLinkName
    {
        get; set;
    }

    public string OperationName
    {
        get; set;
    }

    public object Clone()
    {
        return new PagingInformation
        {
            ItemName = ItemName?.Clone() as string,
            NextLinkName = NextLinkName?.Clone() as string,
            OperationName = OperationName?.Clone() as string,
        };
    }
}

public class CodeMethod : CodeTerminalWithKind<CodeMethodKind>, ICloneable, IDocumentedElement
{
    public static CodeMethod FromIndexer(CodeIndexer originalIndexer, CodeClass indexerClass, string methodNameSuffix, bool parameterNullable)
    {
        if(originalIndexer == null)
            throw new ArgumentNullException(nameof(originalIndexer));
        if(indexerClass == null)
            throw new ArgumentNullException(nameof(indexerClass));
        var method = new CodeMethod {
            IsAsync = false,
            IsStatic = false,
            Access = AccessModifier.Public,
            Kind = CodeMethodKind.IndexerBackwardCompatibility,
            Name = originalIndexer.PathSegment + methodNameSuffix,
            Description = originalIndexer.Description,
            ReturnType = new CodeType {
                IsNullable = false,
                TypeDefinition = indexerClass,
                Name = indexerClass.Name,
            },
            OriginalIndexer = originalIndexer,
        };
        var parameter = new CodeParameter {
            Name = "id",
            Optional = false,
            Kind = CodeParameterKind.Custom,
            Description = "Unique identifier of the item",
            Type = new CodeType {
                Name = "String",
                IsNullable = parameterNullable,
                IsExternal = true,
            },
        };
        method.AddParameter(parameter);
        return method;
    }
    public HttpMethod? HttpMethod {get;set;}
    public string RequestBodyContentType { get; set; }
    private HashSet<string> acceptedResponseTypes;
    public HashSet<string> AcceptedResponseTypes {
        get
        {
            if(acceptedResponseTypes == null)
                acceptedResponseTypes = new(StringComparer.OrdinalIgnoreCase);
            return acceptedResponseTypes;
        }
        set
        {
            acceptedResponseTypes = value;
        }
    }
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
    private readonly CodeParameterOrderComparer parameterOrderComparer = new ();
    public IEnumerable<CodeParameter> Parameters { get => parameters.Values.OrderBy(static x => x, parameterOrderComparer); }
    public bool IsStatic {get;set;} = false;
    public bool IsAsync {get;set;} = true;
    public string Description {get; set;}

    public PagingInformation PagingInformation
    {
        get; set;
    }

    /// <summary>
    /// The combination of the path, query and header parameters for the current URL.
    /// Only use this property if the language you are generating for doesn't support fluent API style (e.g. Shell/CLI)
    /// </summary>
    public IEnumerable<CodeParameter> PathQueryAndHeaderParameters
    {
        get; private set;
    }
    public void AddPathQueryOrHeaderParameter(params CodeParameter[] parameters)
    {
        if (parameters == null || !parameters.Any()) return;
        foreach (var parameter in parameters)
        {
            EnsureElementsAreChildren(parameter);
        }
        if (PathQueryAndHeaderParameters == null)
            PathQueryAndHeaderParameters = new List<CodeParameter>(parameters);
        else if (PathQueryAndHeaderParameters is List<CodeParameter> cast)
            cast.AddRange(parameters);
    }
    /// <summary>
    /// The property this method accesses to when it's a getter or setter.
    /// </summary>
    public CodeProperty AccessedProperty { get; set; }
    public bool IsAccessor { 
        get => IsOfKind(CodeMethodKind.Getter, CodeMethodKind.Setter);
    }
    public HashSet<string> SerializerModules { get; set; }
    public HashSet<string> DeserializerModules { get; set; }
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

    private ConcurrentDictionary<string, CodeTypeBase> errorMappings = new();
    
    /// <summary>
    /// Mapping of the error code and response types for this method.
    /// </summary>
    public IOrderedEnumerable<KeyValuePair<string, CodeTypeBase>> ErrorMappings
    {
        get
        {
            return errorMappings.OrderBy(static x => x.Key);
        }
    }
    public void ReplaceErrorMapping(CodeTypeBase oldType, CodeTypeBase newType)
    {
        var codes = errorMappings.Where(x => x.Value == oldType).Select(x => x.Key).ToArray();
        foreach (var code in codes)
        {
            errorMappings[code] = newType;
        }
    }
    private ConcurrentDictionary<string, CodeTypeBase> discriminatorMappings = new();
    /// <summary>
    /// Gets/Sets the discriminator values for the class where the key is the value as represented in the payload.
    /// </summary>
    public IOrderedEnumerable<KeyValuePair<string, CodeTypeBase>> DiscriminatorMappings
    {
        get
        {
            return discriminatorMappings.OrderBy(static x => x.Key);
        }
    }
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
            Kind = Kind,
            ReturnType = ReturnType?.Clone() as CodeTypeBase,
            Name = Name.Clone() as string,
            HttpMethod = HttpMethod,
            IsAsync = IsAsync,
            Access = Access,
            IsStatic = IsStatic,
            Description = Description?.Clone() as string,
            RequestBodyContentType = RequestBodyContentType?.Clone() as string,
            BaseUrl = BaseUrl?.Clone() as string,
            AccessedProperty = AccessedProperty,
            SerializerModules = SerializerModules == null ? null : new (SerializerModules),
            DeserializerModules = DeserializerModules == null ? null : new (DeserializerModules),
            OriginalMethod = OriginalMethod,
            Parent = Parent,
            OriginalIndexer = OriginalIndexer,
            errorMappings = errorMappings == null ? null : new (errorMappings),
            discriminatorMappings = discriminatorMappings == null ? null : new (discriminatorMappings),
            DiscriminatorPropertyName = DiscriminatorPropertyName?.Clone() as string,
            acceptedResponseTypes = acceptedResponseTypes == null ? null : new (acceptedResponseTypes),
            PagingInformation = PagingInformation?.Clone() as PagingInformation,
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
    public void AddErrorMapping(string errorCode, CodeTypeBase type)
    {
        if(type == null) throw new ArgumentNullException(nameof(type));
        if(string.IsNullOrEmpty(errorCode)) throw new ArgumentNullException(nameof(errorCode));
        errorMappings.TryAdd(errorCode, type);
    }

    public void AddDiscriminatorMapping(string key, CodeTypeBase type)
    {
        if(type == null) throw new ArgumentNullException(nameof(type));
        if(string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        discriminatorMappings.TryAdd(key, type);
    }
    public CodeTypeBase GetDiscriminatorMappingValue(string key)
    {
        if(string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if(discriminatorMappings.TryGetValue(key, out var value))
            return value;
        return null;
    }
    public void RemoveDiscriminatorMapping(params string[] keys) {
        ArgumentNullException.ThrowIfNull(keys, nameof(keys));
        foreach(var key in keys)
            discriminatorMappings.TryRemove(key, out var _);
    }
    public CodeTypeBase GetErrorMappingValue(string key)
    {
        if(string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if(errorMappings.TryGetValue(key, out var value))
            return value;
        return null;
    }
}
