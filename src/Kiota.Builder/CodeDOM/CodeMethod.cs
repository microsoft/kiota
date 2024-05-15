using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.CodeDOM;

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
    /// <summary>
    /// Method used to distinguish between regular and composed type wrapper models during serialization for loosely-typed languages.
    /// </summary>
    ComposedTypeMarker,
    /// <summary>
    /// Fluent API method returning a request builder with a set raw URL. depends on the RawUrlConstructor.
    /// </summary>
    RawUrlBuilder,
    /// <summary>
    /// The override for the error message for the error/exception type.
    /// </summary>
    ErrorMessageOverride
}
public enum HttpMethod
{
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
    } = string.Empty;

    public string NextLinkName
    {
        get; set;
    } = string.Empty;

    public string OperationName
    {
        get; set;
    } = string.Empty;

    public object Clone()
    {
        return new PagingInformation
        {
            ItemName = ItemName,
            NextLinkName = NextLinkName,
            OperationName = OperationName,
        };
    }
}

public class CodeMethod : CodeTerminalWithKind<CodeMethodKind>, ICloneable, IDocumentedElement, IDeprecableElement
{
    public static readonly CodeParameterKind ParameterKindForConvertedIndexers = CodeParameterKind.Custom;
    public static CodeMethod FromIndexer(CodeIndexer originalIndexer, Func<string, string> methodNameCallback, Func<string, string> parameterNameCallback, bool parameterNullable, bool typeSpecificOverload = false)
    {
        ArgumentNullException.ThrowIfNull(originalIndexer);
        ArgumentNullException.ThrowIfNull(methodNameCallback);
        ArgumentNullException.ThrowIfNull(parameterNameCallback);
        var method = new CodeMethod
        {
            IsAsync = false,
            IsStatic = false,
            Access = AccessModifier.Public,
            Kind = CodeMethodKind.IndexerBackwardCompatibility,
            Name = methodNameCallback(originalIndexer.IndexParameter.Name) + (typeSpecificOverload ? originalIndexer.IndexParameter.Type.Name.ToFirstCharacterUpperCase() : string.Empty),
            Documentation = (CodeDocumentation)originalIndexer.Documentation.Clone(),
            ReturnType = (CodeTypeBase)originalIndexer.ReturnType.Clone(),
            OriginalIndexer = originalIndexer,
            Deprecation = originalIndexer.Deprecation,
        };
        if (method.ReturnType is not null)
            method.ReturnType.IsNullable = false;
        var parameter = new CodeParameter
        {
            Name = parameterNameCallback(originalIndexer.IndexParameter.Name),
            Optional = false,
            Kind = ParameterKindForConvertedIndexers,
            Documentation = (CodeDocumentation)originalIndexer.IndexParameter.Documentation.Clone(),
            Type = (CodeTypeBase)originalIndexer.IndexParameter.Type.Clone(),
            SerializationName = originalIndexer.IndexParameter.SerializationName,
        };
        parameter.Type.IsNullable = parameterNullable;
        method.AddParameter(parameter);
        return method;
    }
    public HttpMethod? HttpMethod
    {
        get; set;
    }
    [JsonIgnore]
    public string RequestBodyContentType { get; set; } = string.Empty;
    [JsonIgnore]
    public IList<string> AcceptedResponseTypes { get; private set; } = [];
    public void AddAcceptedResponsesTypes(IEnumerable<string> types)
    {
        if (types == null) return;
        if (AcceptedResponseTypes is List<string> list)
            list.AddRange(types);
    }
    [JsonIgnore]
    public bool ShouldAddAcceptHeader => AcceptedResponseTypes.Any();
    [JsonIgnore]
    public string AcceptHeaderValue => string.Join(", ", AcceptedResponseTypes);
    public AccessModifier Access { get; set; } = AccessModifier.Public;
#nullable disable // exposing property is required
    private CodeTypeBase returnType;
#nullable enable
    public required CodeTypeBase ReturnType
    {
        get => returnType; set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            returnType = value;
        }
    }
    private readonly ConcurrentDictionary<string, CodeParameter> parameters = new();
    public void RemoveParametersByKind(params CodeParameterKind[] kinds)
    {
        parameters.Where(p => p.Value.IsOfKind(kinds))
                            .Select(static x => x.Key)
                            .ToList()
                            .ForEach(x => parameters.Remove(x, out var _));
    }

    public void ClearParameters()
    {
        parameters.Clear();
    }
    [JsonPropertyName("parameters")]
    public IDictionary<string, CodeParameter> ParametersJSON
    {
        get => parameters;
    }
    private readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    [JsonIgnore]
    public IEnumerable<CodeParameter> Parameters
    {
        get => parameters.Values.OrderBy(static x => x, parameterOrderComparer);
    }
    public bool IsStatic
    {
        get; set;
    }
    public bool IsAsync { get; set; } = true;
    [JsonIgnore]
    public CodeDocumentation Documentation { get; set; } = new();
    [JsonIgnore]
    public PagingInformation? PagingInformation
    {
        get; set;
    }

    /// <summary>
    /// The combination of the path, query and header parameters for the current URL.
    /// Only use this property if the language you are generating for doesn't support fluent API style (e.g. CLI)
    /// </summary>
    [JsonIgnore]
    public IEnumerable<CodeParameter> PathQueryAndHeaderParameters
    {
        get => pathQueryAndHeaderParameters.Values;
    }
    private readonly Dictionary<string, CodeParameter> pathQueryAndHeaderParameters = new(StringComparer.OrdinalIgnoreCase);
    public void AddPathQueryOrHeaderParameter(params CodeParameter[] parameters)
    {
        if (parameters == null || parameters.Length == 0) return;
        foreach (var parameter in parameters.OrderByDescending(static x => x.Kind)) //guarantees that path parameters are added first and other are deduplicated
        {
            EnsureElementsAreChildren(parameter);
            if (!pathQueryAndHeaderParameters.TryAdd(parameter.Name, parameter))
            {
                if (parameter.IsOfKind(CodeParameterKind.QueryParameter))
                    parameter.Name += "-query";
                else if (parameter.IsOfKind(CodeParameterKind.Headers))
                    parameter.Name += "-header";
                else
                    continue;
                pathQueryAndHeaderParameters.Add(parameter.Name, parameter);
            }

        }
    }
    /// <summary>
    /// The property this method accesses to when it's a getter or setter.
    /// </summary>
    public CodeProperty? AccessedProperty
    {
        get; set;
    }
    public bool IsAccessor
    {
        get => IsOfKind(CodeMethodKind.Getter, CodeMethodKind.Setter);
    }
#pragma warning disable CA2227
    [JsonIgnore]
    public HashSet<string> SerializerModules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    [JsonIgnore]
    public HashSet<string> DeserializerModules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227
    /// <summary>
    /// Indicates whether this method is an overload for another method.
    /// </summary>
    public bool IsOverload
    {
        get
        {
            return OriginalMethod != null;
        }
    }
    /// <summary>
    /// Provides a reference to the original method that this method is an overload of.
    /// </summary>
    [JsonIgnore]
    public CodeMethod? OriginalMethod
    {
        get; set;
    }
    /// <summary>
    /// The original indexer codedom element this method replaces when it is of kind IndexerBackwardCompatibility.
    /// </summary>
    [JsonIgnore]
    public CodeIndexer? OriginalIndexer
    {
        get; set;
    }
    /// <summary>
    /// The base url for every request read from the servers property on the description.
    /// Only provided for constructor on Api client
    /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
    [JsonIgnore]
    public string BaseUrl { get; set; } = string.Empty;
#pragma warning restore CA1056 // Uri properties should not be strings

    /// <summary>
    /// This is currently used for CommandBuilder methods to get the original name without the Build prefix & Command suffix.
    /// Avoids regex operations
    /// </summary>
    public string SimpleName { get; set; } = string.Empty;
    /// <summary>
    /// Deduplicates 4XX and 5XX error mappings into a single XXX mapping if they are the same.
    /// </summary>
    public void DeduplicateErrorMappings()
    {
        if (!errorMappings.TryGetValue(ErrorMappingClientRange, out var clientError) || !errorMappings.TryGetValue(ErrorMappingServerRange, out var serverError)) return;
        if ((clientError == serverError || clientError is CodeType clientErrorType && serverError is CodeType serverErrorType && clientErrorType.TypeDefinition == serverErrorType.TypeDefinition && clientErrorType.TypeDefinition is not null) &&
            errorMappings.TryAdd(ErrorMappingAllRange, clientError))
        {
            errorMappings.TryRemove(ErrorMappingServerRange, out var _);
            errorMappings.TryRemove(ErrorMappingClientRange, out var _);
        }
    }
    internal const string ErrorMappingClientRange = "4XX";
    internal const string ErrorMappingServerRange = "5XX";
    internal const string ErrorMappingAllRange = "XXX";
#pragma warning disable CA1056 // URI-like properties should not be strings
    /// <summary>
    /// The URL template override for the method when it's different for the operation
    /// </summary>
    public string UrlTemplateOverride { get; set; } = string.Empty;
#pragma warning restore CA1056 // URI-like properties should not be strings
    [JsonIgnore]
    public bool HasUrlTemplateOverride => !string.IsNullOrEmpty(UrlTemplateOverride);

    private ConcurrentDictionary<string, CodeTypeBase> errorMappings = new(StringComparer.OrdinalIgnoreCase);
    [JsonPropertyName("errorMappings")]
    public IDictionary<string, CodeTypeBase> ErrorMappingsJSON
    {
        get => errorMappings;
    }

    /// <summary>
    /// Mapping of the error code and response types for this method.
    /// </summary>
    [JsonIgnore]
    public IOrderedEnumerable<KeyValuePair<string, CodeTypeBase>> ErrorMappings
    {
        get
        {
            return errorMappings.OrderBy(static x => x.Key);
        }
    }
    public bool HasErrorMappingCode(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        return errorMappings.ContainsKey(code);
    }
    [JsonIgnore]
    public DeprecationInformation? Deprecation
    {
        get; set;
    }

    public void ReplaceErrorMapping(CodeTypeBase oldType, CodeTypeBase newType)
    {
        var codes = errorMappings.Where(x => x.Value == oldType).Select(static x => x.Key).ToArray();
        foreach (var code in codes)
        {
            errorMappings[code] = newType;
        }
    }
    public object Clone()
    {
        var method = new CodeMethod
        {
            Kind = Kind,
            ReturnType = (CodeTypeBase)ReturnType.Clone(),
            Name = Name,
            HttpMethod = HttpMethod,
            IsAsync = IsAsync,
            Access = Access,
            IsStatic = IsStatic,
            RequestBodyContentType = RequestBodyContentType,
            BaseUrl = BaseUrl,
            AccessedProperty = AccessedProperty,
            SerializerModules = new(SerializerModules, StringComparer.OrdinalIgnoreCase),
            DeserializerModules = new(DeserializerModules, StringComparer.OrdinalIgnoreCase),
            OriginalMethod = OriginalMethod,
            Parent = Parent,
            OriginalIndexer = OriginalIndexer,
            errorMappings = new(errorMappings),
            AcceptedResponseTypes = new List<string>(AcceptedResponseTypes),
            PagingInformation = PagingInformation?.Clone() as PagingInformation,
            Documentation = (CodeDocumentation)Documentation.Clone(),
            Deprecation = Deprecation,
            UrlTemplateOverride = UrlTemplateOverride,
        };
        if (Parameters?.Any() ?? false)
            method.AddParameter(Parameters.Select(x => (CodeParameter)x.Clone()).ToArray());
        return method;
    }

    public void AddParameter(params CodeParameter[] methodParameters)
    {
        if (methodParameters == null || methodParameters.Any(static x => x == null))
            throw new ArgumentNullException(nameof(methodParameters));
        if (methodParameters.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(methodParameters));
        EnsureElementsAreChildren(methodParameters);
        methodParameters.ToList().ForEach(x => parameters.TryAdd(x.Name, x));
    }
    public void AddErrorMapping(string errorCode, CodeTypeBase type)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        errorMappings.TryAdd(errorCode, type);
    }
}
