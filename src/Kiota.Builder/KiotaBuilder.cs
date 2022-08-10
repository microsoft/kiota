﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeRenderers;
using Kiota.Builder.Exceptions;
using Kiota.Builder.Extensions;
using Kiota.Builder.OpenApiExtensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder;

public class KiotaBuilder
{
    private readonly ILogger<KiotaBuilder> logger;
    private readonly GenerationConfiguration config;
    private OpenApiDocument openApiDocument;

    public KiotaBuilder(ILogger<KiotaBuilder> logger, GenerationConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }
    private void CleanOutputDirectory()
    {
        if(config.CleanOutput && Directory.Exists(config.OutputPath))
        {
            logger.LogInformation("Cleaning output directory {path}", config.OutputPath);
            Directory.Delete(config.OutputPath, true);
        }
    }

    public async Task GenerateSDK(CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        // Step 1 - Read input stream
        string inputPath = config.OpenAPIFilePath;

        try {
            CleanOutputDirectory();
            // doing this verification at the beginning to give immediate feedback to the user
            Directory.CreateDirectory(config.OutputPath);
        } catch (Exception ex) {
            throw new InvalidOperationException($"Could not open/create output directory {config.OutputPath}, reason: {ex.Message}", ex);
        }
        
        sw.Start();
        using var input = await LoadStream(inputPath, cancellationToken);
        if(input == null)
            return;
        StopLogAndReset(sw, "step 1 - reading the stream - took");

        // Step 2 - Parse OpenAPI
        sw.Start();
        openApiDocument = CreateOpenApiDocument(input);
        StopLogAndReset(sw, "step 2 - parsing the document - took");

        SetApiRootUrl();

        modelNamespacePrefixToTrim = GetDeeperMostCommonNamespaceNameForModels(openApiDocument);

        // Step 3 - Create Uri Space of API
        sw.Start();
        var openApiTree = CreateUriSpace(openApiDocument);
        StopLogAndReset(sw, "step 3 - create uri space - took");

        // Step 4 - Create Source Model
        sw.Start();
        var generatedCode = CreateSourceModel(openApiTree);
        StopLogAndReset(sw, "step 4 - create source model - took");

        // Step 5 - RefineByLanguage
        sw.Start();
        ApplyLanguageRefinement(config, generatedCode);
        StopLogAndReset(sw, "step 5 - refine by language - took");

        // Step 6 - Write language source 
        sw.Start();
        await CreateLanguageSourceFilesAsync(config.Language, generatedCode, cancellationToken);
        StopLogAndReset(sw, "step 6 - writing files - took");
    }
    private void SetApiRootUrl() {
        config.ApiRootUrl = openApiDocument.Servers.FirstOrDefault()?.Url.TrimEnd('/');
        if(string.IsNullOrEmpty(config.ApiRootUrl))
            throw new InvalidOperationException("A servers entry (v3) or host + basePath + schems properties (v2) must be present in the OpenAPI description.");
    }
    private void StopLogAndReset(Stopwatch sw, string prefix) {
        sw.Stop();
        logger.LogDebug("{prefix} {swElapsed}", prefix, sw.Elapsed);
        sw.Reset();
    }


    private async Task<Stream> LoadStream(string inputPath, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        Stream input;
        if (inputPath.StartsWith("http"))
            try {
                using var httpClient = new HttpClient();
                input = await httpClient.GetStreamAsync(inputPath, cancellationToken);
            } catch (HttpRequestException ex) {
                throw new InvalidOperationException($"Could not download the file at {inputPath}, reason: {ex.Message}", ex);
            }
        else
            try {
                input = new FileStream(inputPath, FileMode.Open);
            } catch (Exception ex) when (ex is FileNotFoundException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is NotSupportedException) {
                throw new InvalidOperationException($"Could not open the file at {inputPath}, reason: {ex.Message}", ex);
            }
        stopwatch.Stop();
        logger.LogTrace("{timestamp}ms: Read OpenAPI file {file}", stopwatch.ElapsedMilliseconds, inputPath);
        return input;
    }

    public OpenApiDocument CreateOpenApiDocument(Stream input)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        logger.LogTrace("Parsing OpenAPI file");
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            ExtensionParsers = new()
            {
                {
                    OpenApiPagingExtension.Name,
                    (i, _) => OpenApiPagingExtension.Parse(i)
                },
                {
                    OpenApiEnumValuesDescriptionExtension.Name,
                    static (i, _ ) => OpenApiEnumValuesDescriptionExtension.Parse(i)
                },
            }
        });
        var doc = reader.Read(input, out var diag);
        stopwatch.Stop();
        if (diag.Errors.Count > 0)
        {
            logger.LogTrace("{timestamp}ms: Parsed OpenAPI with errors. {count} paths found.", stopwatch.ElapsedMilliseconds, doc?.Paths?.Count ?? 0);
            foreach(var parsingError in diag.Errors)
            {
                logger.LogError("OpenApi Parsing error: {message}", parsingError.ToString());
            }
        }
        else
        {
            logger.LogTrace("{timestamp}ms: Parsed OpenAPI successfully. {count} paths found.", stopwatch.ElapsedMilliseconds, doc?.Paths?.Count ?? 0);
        }

        return doc;
    }
    public static string GetDeeperMostCommonNamespaceNameForModels(OpenApiDocument document)
    {
        if(!(document?.Components?.Schemas?.Any() ?? false)) return string.Empty;
        var distinctKeys = document.Components
                                .Schemas
                                .Keys
                                .Select(x => string.Join(nsNameSeparator, x.Split(nsNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                                .SkipLast(1)))
                                .Where(x => !string.IsNullOrEmpty(x))
                                .Distinct()
                                .OrderByDescending(x => x.Count(y => y == nsNameSeparator));
        if(!distinctKeys.Any()) return string.Empty;
        var longestKey = distinctKeys.FirstOrDefault();
        var candidate = string.Empty;
        var longestKeySegments = longestKey.Split(nsNameSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach(var segment in longestKeySegments)
        {
            var testValue = (candidate + nsNameSeparator + segment).Trim(nsNameSeparator);
            if(distinctKeys.All(x => x.StartsWith(testValue, StringComparison.OrdinalIgnoreCase)))
                candidate = testValue;
            else
                break;
        }

        return candidate;
    }

    /// <summary>
    /// Translate OpenApi PathItems into a tree structure that will define the classes
    /// </summary>
    /// <param name="doc">OpenAPI Document of the API to be processed</param>
    /// <returns>Root node of the API URI space</returns>
    public OpenApiUrlTreeNode CreateUriSpace(OpenApiDocument doc)
    {
        if(doc == null) throw new ArgumentNullException(nameof(doc));
        if(openApiDocument == null) openApiDocument = doc;

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var node = OpenApiUrlTreeNode.Create(doc, Constants.DefaultOpenApiLabel);
        stopwatch.Stop();
        logger.LogTrace("{timestamp}ms: Created UriSpace tree", stopwatch.ElapsedMilliseconds);
        return node;
    }
    private CodeNamespace rootNamespace;
    private CodeNamespace modelsNamespace;
    private string modelNamespacePrefixToTrim;

    /// <summary>
    /// Convert UriSpace of OpenApiPathItems into conceptual SDK Code model 
    /// </summary>
    /// <param name="root">Root OpenApiUriSpaceNode of API to be generated</param>
    /// <returns></returns>
    public CodeNamespace CreateSourceModel(OpenApiUrlTreeNode root)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        rootNamespace = CodeNamespace.InitRootNamespace();
        var codeNamespace = rootNamespace.AddNamespace(config.ClientNamespaceName);
        modelsNamespace = rootNamespace.AddNamespace(config.ModelsNamespaceName);
        CreateRequestBuilderClass(codeNamespace, root, root);
        StopLogAndReset(stopwatch, $"{nameof(CreateRequestBuilderClass)}");
        stopwatch.Start();
        MapTypeDefinitions(codeNamespace);
        StopLogAndReset(stopwatch, $"{nameof(MapTypeDefinitions)}");

        logger.LogTrace("{timestamp}ms: Created source model with {count} classes", stopwatch.ElapsedMilliseconds, codeNamespace.GetChildElements(true).Count());

        return rootNamespace;
    }

    /// <summary>
    /// Manipulate CodeDOM for language specific issues
    /// </summary>
    /// <param name="config"></param>
    /// <param name="generatedCode"></param>
    public void ApplyLanguageRefinement(GenerationConfiguration config, CodeNamespace generatedCode)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        ILanguageRefiner.Refine(config, generatedCode);

        stopwatch.Stop();
        logger.LogDebug("{timestamp}ms: Language refinement applied", stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Iterate through Url Space and create request builder classes for each node in the tree
    /// </summary>
    /// <param name="root">Root node of URI space from the OpenAPI described API</param>
    /// <returns>A CodeNamespace object that contains request builder classes for the Uri Space</returns>

    public async Task CreateLanguageSourceFilesAsync(GenerationLanguage language, CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        var languageWriter = LanguageWriter.GetLanguageWriter(language, config.OutputPath, config.ClientNamespaceName, config.UsesBackingStore);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var codeRenderer = CodeRenderer.GetCodeRender(config);
        await codeRenderer.RenderCodeNamespaceToFilePerClassAsync(languageWriter, generatedCode, cancellationToken);
        stopwatch.Stop();
        logger.LogTrace("{timestamp}ms: Files written to {path}", stopwatch.ElapsedMilliseconds, config.OutputPath);
    }
    private static readonly string requestBuilderSuffix = "RequestBuilder";
    private static readonly string itemRequestBuilderSuffix = "ItemRequestBuilder";
    private static readonly string voidType = "void";
    private static readonly string coreInterfaceType = "IRequestAdapter";
    private static readonly string requestAdapterParameterName = "requestAdapter";
    private static readonly string constructorMethodName = "constructor";
    /// <summary>
    /// Create a CodeClass instance that is a request builder class for the OpenApiUrlTreeNode
    /// </summary>
    private void CreateRequestBuilderClass(CodeNamespace currentNamespace, OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode rootNode)
    {
        // Determine Class Name
        CodeClass codeClass;
        var isApiClientClass = currentNode == rootNode;
        if (isApiClientClass)
            codeClass = currentNamespace.AddClass(new CodeClass { 
            Name = config.ClientClassName,
            Kind = CodeClassKind.RequestBuilder,
            Description = "The main entry point of the SDK, exposes the configuration and the fluent API."
        }).First();
        else
        {
            var targetNS = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNamespace.EnsureItemNamespace() : currentNamespace;
            var className = currentNode.DoesNodeBelongToItemSubnamespace() ? currentNode.GetClassName(config.StructuredMimeTypes, itemRequestBuilderSuffix) :currentNode.GetClassName(config.StructuredMimeTypes, requestBuilderSuffix);
            codeClass = targetNS.AddClass(new CodeClass {
                Name = className.CleanupSymbolName(), 
                Kind = CodeClassKind.RequestBuilder,
                Description = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
            }).First();
        }

        logger.LogTrace("Creating class {class}", codeClass.Name);

        // Add properties for children
        foreach (var child in currentNode.Children)
        {
            var propIdentifier = child.Value.GetClassName(config.StructuredMimeTypes);
            var propType = child.Value.DoesNodeBelongToItemSubnamespace() ? propIdentifier + itemRequestBuilderSuffix : propIdentifier + requestBuilderSuffix;
            if (child.Value.IsPathSegmentWithSingleSimpleParameter())
            {
                var prop = CreateIndexer($"{propIdentifier}-indexer", propType, child.Value, currentNode);
                codeClass.SetIndexer(prop);
            }
            else if (child.Value.IsComplexPathWithAnyNumberOfParameters())
            {
                CreateMethod(propIdentifier, propType, codeClass, child.Value);
            }
            else
            {
                var prop = CreateProperty(propIdentifier, propType, kind: CodePropertyKind.RequestBuilder); // we should add the type definition here but we can't as it might not have been generated yet
                codeClass.AddProperty(prop);
            }
        }

        // Add methods for Operations
        if (currentNode.HasOperations(Constants.DefaultOpenApiLabel))
        {
            foreach(var operation in currentNode
                                    .PathItems[Constants.DefaultOpenApiLabel]
                                    .Operations)
                CreateOperationMethods(currentNode, operation.Key, operation.Value, codeClass);
        }
        CreateUrlManagement(codeClass, currentNode, isApiClientClass);
        
        Parallel.ForEach(currentNode.Children.Values, childNode =>
        {
            var targetNamespaceName = childNode.GetNodeNamespaceFromPath(config.ClientNamespaceName);
            var targetNamespace = rootNamespace.FindNamespaceByName(targetNamespaceName) ?? rootNamespace.AddNamespace(targetNamespaceName);
            CreateRequestBuilderClass(targetNamespace, childNode, rootNode);
        });
    }
    private static void CreateMethod(string propIdentifier, string propType, CodeClass codeClass, OpenApiUrlTreeNode currentNode)
    {
        var methodToAdd = new CodeMethod {
            Name = propIdentifier.CleanupSymbolName(),
            Kind = CodeMethodKind.RequestBuilderWithParameters,
            Description = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel, $"Builds and executes requests for operations under {currentNode.Path}"),
            Access = AccessModifier.Public,
            IsAsync = false,
            IsStatic = false,
        };
        methodToAdd.ReturnType = new CodeType {
            Name = propType,
            ActionOf = false,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None,
            IsExternal = false,
            IsNullable = false,
        };
        AddPathParametersToMethod(currentNode, methodToAdd, false);
        codeClass.AddMethod(methodToAdd);
    }
    private static void AddPathParametersToMethod(OpenApiUrlTreeNode currentNode, CodeMethod methodToAdd, bool asOptional) {
        foreach(var parameter in currentNode.GetPathParametersForCurrentSegment()) {
            var codeName = parameter.Name.SanitizeParameterNameForCodeSymbols();
            var mParameter = new CodeParameter {
                Name = codeName,
                Optional = asOptional,
                Description = parameter.Description.CleanupDescription(),
                Kind = CodeParameterKind.Path,
                SerializationName = parameter.Name.Equals(codeName) ? default : parameter.Name.SanitizeParameterNameForUrlTemplate(),
            };
            mParameter.Type = GetPrimitiveType(parameter.Schema ?? parameter.Content.Values.FirstOrDefault()?.Schema);
            mParameter.Type.CollectionKind = parameter.Schema.IsArray() ? CodeType.CodeTypeCollectionKind.Array : default;
            // not using the content schema as RFC6570 will serialize arrays as CSVs and content expects a JSON array, we failsafe to opaque string, it could be improved by involving the serialization layers.
            methodToAdd.AddParameter(mParameter);
        }
    }
    private static readonly string PathParametersParameterName = "pathParameters";
    private void CreateUrlManagement(CodeClass currentClass, OpenApiUrlTreeNode currentNode, bool isApiClientClass) {
        var pathProperty = new CodeProperty {
            Access = AccessModifier.Private,
            Name = "urlTemplate",
            DefaultValue = $"\"{currentNode.GetUrlTemplate()}\"",
            ReadOnly = true,
            Description = "Url template to use to build the URL for the current request builder",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType {
                Name = "string",
                IsNullable = false,
                IsExternal = true,
            },
        };
        currentClass.AddProperty(pathProperty);

        var requestAdapterProperty = new CodeProperty {
            Name = requestAdapterParameterName,
            Description = "The request adapter to use to execute the requests.",
            Kind = CodePropertyKind.RequestAdapter,
            Access = AccessModifier.Private,
            ReadOnly = true,
        };
        requestAdapterProperty.Type = new CodeType {
            Name = coreInterfaceType,
            IsExternal = true,
            IsNullable = false,
        };
        currentClass.AddProperty(requestAdapterProperty);
        var constructor = currentClass.AddMethod(new CodeMethod {
            Name = constructorMethodName,
            Kind = isApiClientClass ? CodeMethodKind.ClientConstructor : CodeMethodKind.Constructor,
            IsAsync = false,
            IsStatic = false,
            Description = $"Instantiates a new {currentClass.Name.ToFirstCharacterUpperCase()} and sets the default values.",
            Access = AccessModifier.Public,
        }).First();
        constructor.ReturnType = new CodeType { Name = voidType, IsExternal = true };
        var pathParametersProperty = new CodeProperty {
            Name = PathParametersParameterName,
            Description = "Path parameters for the request",
            Kind = CodePropertyKind.PathParameters,
            Access = AccessModifier.Private,
            ReadOnly = true,
            Type = new CodeType {
                Name = "Dictionary<string, object>",
                IsExternal = true,
                IsNullable = false,
            },
        };
        currentClass.AddProperty(pathParametersProperty);
        if(isApiClientClass) {
            constructor.SerializerModules = config.Serializers;
            constructor.DeserializerModules = config.Deserializers;
            constructor.BaseUrl = config.ApiRootUrl;
            pathParametersProperty.DefaultValue = $"new {pathParametersProperty.Type.Name}()";
        } else {
            constructor.AddParameter(new CodeParameter {
                Name = PathParametersParameterName,
                Type = pathParametersProperty.Type,
                Optional = false,
                Description = pathParametersProperty.Description,
                Kind = CodeParameterKind.PathParameters,
            });
            AddPathParametersToMethod(currentNode, constructor, true);
        }
        constructor.AddParameter(new CodeParameter {
            Name = requestAdapterParameterName,
            Type = requestAdapterProperty.Type,
            Optional = false,
            Description = requestAdapterProperty.Description,
            Kind = CodeParameterKind.RequestAdapter,
        });
        if(isApiClientClass && config.UsesBackingStore) {
            var factoryInterfaceName = $"{BackingStoreInterface}Factory";
            var backingStoreParam = new CodeParameter {
                Name = "backingStore",
                Optional = true,
                Description = "The backing store to use for the models.",
                Kind = CodeParameterKind.BackingStore,
                Type = new CodeType {
                    Name = factoryInterfaceName,
                    IsNullable = true,
                }
            };
            constructor.AddParameter(backingStoreParam);
        }
    }
    private static readonly Func<CodeClass, int> shortestNamespaceOrder = (x) => x.GetNamespaceDepth();
    /// <summary>
    /// Remaps definitions to custom types so they can be used later in generation or in refiners
    /// </summary>
    private void MapTypeDefinitions(CodeElement codeElement) {
        var unmappedTypes = GetUnmappedTypeDefinitions(codeElement).Distinct();
        
        var unmappedTypesWithNoName = unmappedTypes.Where(x => string.IsNullOrEmpty(x.Name)).ToList();
        
        unmappedTypesWithNoName.ForEach(x => {
            logger.LogWarning("Type with empty name and parent {ParentName}", x.Parent.Name);
        });

        var unmappedTypesWithName = unmappedTypes.Except(unmappedTypesWithNoName);

        var unmappedRequestBuilderTypes = unmappedTypesWithName
                                .Where(x => 
                                x.Parent is CodeProperty property && property.IsOfKind(CodePropertyKind.RequestBuilder) ||
                                x.Parent is CodeIndexer ||
                                x.Parent is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestBuilderWithParameters))
                                .ToList();
        
        Parallel.ForEach(unmappedRequestBuilderTypes, x => {
            var parentNS = x.Parent.Parent.Parent as CodeNamespace;
            x.TypeDefinition = parentNS.FindChildrenByName<CodeClass>(x.Name)
                                        .OrderBy(shortestNamespaceOrder)
                                        .FirstOrDefault();
            // searching down first because most request builder properties on a request builder are just sub paths on the API
            if(x.TypeDefinition == null) {
                parentNS = parentNS.Parent as CodeNamespace;
                x.TypeDefinition = parentNS
                    .FindNamespaceByName($"{parentNS.Name}.{x.Name.Substring(0, x.Name.Length - requestBuilderSuffix.Length).ToFirstCharacterLowerCase()}".TrimEnd(nsNameSeparator))
                    ?.FindChildrenByName<CodeClass>(x.Name)
                    ?.OrderBy(shortestNamespaceOrder)
                    ?.FirstOrDefault();
                // in case of the .item namespace, going to the parent and then down to the target by convention
                // this avoid getting the wrong request builder in case we have multiple request builders with the same name in the parent branch
                // in both cases we always take the uppermost item (smaller numbers of segments in the namespace name)
            }
        });

        Parallel.ForEach(unmappedTypesWithName.Where(x => x.TypeDefinition == null).GroupBy(x => x.Name), x => {
            if (rootNamespace.FindChildByName<ITypeDefinition>(x.First().Name) is CodeElement definition)
                foreach (var type in x)
                {
                    type.TypeDefinition = definition;
                    logger.LogWarning("Mapped type {typeName} for {ParentName} using the fallback approach.", type.Name, type.Parent.Name);
                }
        });
    }
    private static readonly char nsNameSeparator = '.';
    private static IEnumerable<CodeType> filterUnmappedTypeDefitions(IEnumerable<CodeTypeBase> source) =>
    source.OfType<CodeType>()
            .Union(source
                    .OfType<CodeComposedTypeBase>()
                    .SelectMany(x => x.Types))
            .Where(x => !x.IsExternal && x.TypeDefinition == null);
    private IEnumerable<CodeType> GetUnmappedTypeDefinitions(CodeElement codeElement) {
        var childElementsUnmappedTypes = codeElement.GetChildElements(true).SelectMany(x => GetUnmappedTypeDefinitions(x));
        return codeElement switch
        {
            CodeMethod method => filterUnmappedTypeDefitions(method.Parameters.Select(x => x.Type).Union(new CodeTypeBase[] { method.ReturnType })).Union(childElementsUnmappedTypes),
            CodeProperty property => filterUnmappedTypeDefitions(new CodeTypeBase[] { property.Type }).Union(childElementsUnmappedTypes),
            CodeIndexer indexer => filterUnmappedTypeDefitions(new CodeTypeBase[] { indexer.ReturnType }).Union(childElementsUnmappedTypes),
            _ => childElementsUnmappedTypes,
        };
    }
    private CodeIndexer CreateIndexer(string childIdentifier, string childType, OpenApiUrlTreeNode currentNode, OpenApiUrlTreeNode parentNode)
    {
        logger.LogTrace("Creating indexer {name}", childIdentifier);
        return new CodeIndexer
        {
            Name = childIdentifier,
            Description = $"Gets an item from the {currentNode.GetNodeNamespaceFromPath(config.ClientNamespaceName)} collection",
            IndexType = new CodeType { Name = "string", IsExternal = true, },
            ReturnType = new CodeType { Name = childType },
            SerializationName = currentNode.Segment.SanitizeParameterNameForUrlTemplate(),
            PathSegment = parentNode.GetNodeNamespaceFromPath(string.Empty).Split('.').Last(),
        };
    }

    private CodeProperty CreateProperty(string childIdentifier, string childType, OpenApiSchema typeSchema = null, CodeTypeBase existingType = null, CodePropertyKind kind = CodePropertyKind.Custom)
    {
        var propertyName = childIdentifier.CleanupSymbolName();
        var prop = new CodeProperty
        {
            Name = propertyName,
            Kind = kind,
            Description = typeSchema?.Description.CleanupDescription() ?? $"The {propertyName} property",
        };
        if(propertyName != childIdentifier)
            prop.SerializationName = childIdentifier;
        if(kind == CodePropertyKind.Custom &&
            typeSchema?.Default is OpenApiString stringDefaultValue &&
            !string.IsNullOrEmpty(stringDefaultValue.Value))
            prop.DefaultValue = $"\"{stringDefaultValue.Value}\"";
        
        if (existingType != null)
            prop.Type = existingType;
        else {
            prop.Type = GetPrimitiveType(typeSchema, childType);
            prop.Type.CollectionKind = typeSchema.IsArray() ? CodeType.CodeTypeCollectionKind.Complex : default;
            logger.LogTrace("Creating property {name} of {type}", prop.Name, prop.Type.Name);
        }
        return prop;
    }
    private static readonly HashSet<string> typeNamesToSkip = new(StringComparer.OrdinalIgnoreCase) {"object", "array"};
    private static CodeType GetPrimitiveType(OpenApiSchema typeSchema, string childType = default) {
        var typeNames = new List<string>{typeSchema?.Items?.Type, childType, typeSchema?.Type};
        if(typeSchema?.AnyOf?.Any() ?? false)
            typeNames.AddRange(typeSchema.AnyOf.Select(x => x.Type)); // double is sometimes an anyof string, number and enum
        // first value that's not null, and not "object" for primitive collections, the items type matters
        var typeName = typeNames.FirstOrDefault(static x => !string.IsNullOrEmpty(x) && !typeNamesToSkip.Contains(x));
        
        var isExternal = false;
        if (typeSchema?.Items?.Enum?.Any() ?? false)
            typeName = childType;
        else {
            var format = typeSchema?.Format ?? typeSchema?.Items?.Format;
            var primitiveTypeName = (typeName?.ToLowerInvariant(), format?.ToLowerInvariant()) switch {
                ("string", "base64url") => "binary",
                ("file", _) => "binary",
                ("string", "duration") => "TimeSpan",
                ("string", "time") => "TimeOnly",
                ("string", "date") => "DateOnly",
                ("string", "date-time") => "DateTimeOffset",
                ("string", _) => "string", // covers commonmark and html
                ("number", "double" or "float" or "decimal") => format.ToLowerInvariant(),
                ("number" or "integer", "int8") => "sbyte",
                ("number" or "integer", "uint8") => "byte",
                ("number" or "integer", "int64") => "int64",
                ("number", "int32") => "integer",
                ("number", _) => "int64",
                ("integer", _) => "integer",
                ("boolean", _) => "boolean",
                (_, "byte" or "binary") => "binary",
                (_, _) => string.Empty,
            };
            if(primitiveTypeName != string.Empty) {
                typeName = primitiveTypeName;
                isExternal = true;
            }
        }
        return new CodeType {
            Name = typeName,
            IsExternal = isExternal,
        };
    }
    private const string RequestBodyPlainTextContentType = "text/plain";
    private static readonly HashSet<string> noContentStatusCodes = new() { "201", "202", "204" };
    private static readonly HashSet<string> errorStatusCodes = new(Enumerable.Range(400, 599).Select(x => x.ToString())
                                                                                 .Concat(new[] { "4XX", "5XX" }), StringComparer.OrdinalIgnoreCase);

    private void AddErrorMappingsForExecutorMethod(OpenApiUrlTreeNode currentNode, OpenApiOperation operation, CodeMethod executorMethod) {
        foreach(var response in operation.Responses.Where(x => errorStatusCodes.Contains(x.Key))) {
            var errorCode = response.Key.ToUpperInvariant();
            var errorSchema = response.Value.GetResponseSchema(config.StructuredMimeTypes);
            if(errorSchema != null) {
                var parentElement = string.IsNullOrEmpty(response.Value.Reference?.Id) && string.IsNullOrEmpty(errorSchema?.Reference?.Id)
                    ? executorMethod as CodeElement
                    : modelsNamespace;
                var errorType = CreateModelDeclarations(currentNode, errorSchema, operation, parentElement, $"{errorCode}Error", response: response.Value);
                if (errorType is CodeType codeType && 
                    codeType.TypeDefinition is CodeClass codeClass &&
                    !codeClass.IsErrorDefinition)
                {
                    codeClass.IsErrorDefinition = true;
                }
                executorMethod.AddErrorMapping(errorCode, errorType);
            }
        }
    }
    private void CreateOperationMethods(OpenApiUrlTreeNode currentNode, OperationType operationType, OpenApiOperation operation, CodeClass parentClass)
    {
        var parameterClass = CreateOperationParameterClass(currentNode, operationType, operation, parentClass);
        var requestConfigClass = parentClass.AddInnerClass(new CodeClass {
            Name = $"{parentClass.Name}{operationType}RequestConfiguration",
            Kind = CodeClassKind.RequestConfiguration,
            Description = "Configuration for the request such as headers, query parameters, and middleware options.",
        }).First();

        var schema = operation.GetResponseSchema(config.StructuredMimeTypes);
        var method = (HttpMethod)Enum.Parse(typeof(HttpMethod), operationType.ToString());
        var executorMethod = parentClass.AddMethod(new CodeMethod {
            Name = operationType.ToString(),
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method,
            Description = (operation.Description ?? operation.Summary).CleanupDescription(),
        }).FirstOrDefault();

        if (operation.Extensions.TryGetValue(OpenApiPagingExtension.Name, out var extension) && extension is OpenApiPagingExtension pagingExtension)
        {
            executorMethod.PagingInformation = new PagingInformation
            {
                ItemName = pagingExtension.ItemName,
                NextLinkName = pagingExtension.NextLinkName,
                OperationName = pagingExtension.OperationName,
            };
        }

        AddErrorMappingsForExecutorMethod(currentNode, operation, executorMethod);
        if (schema != null)
        {
            var returnType = CreateModelDeclarations(currentNode, schema, operation, executorMethod, "Response");
            executorMethod.ReturnType = returnType ?? throw new InvalidOperationException("Could not resolve return type for operation");
        } else {
            string returnType;
            if(operation.Responses.Any(x => noContentStatusCodes.Contains(x.Key)))
                returnType = voidType;
            else if (operation.Responses.Any(x => x.Value.Content.ContainsKey(RequestBodyPlainTextContentType)))
                returnType = "string";
            else
                returnType = "binary";
            executorMethod.ReturnType = new CodeType { Name = returnType, IsExternal = true, };
        }

        
        AddRequestBuilderMethodParameters(currentNode, operationType, operation, parameterClass, requestConfigClass, executorMethod);

        var handlerParam = new CodeParameter {
            Name = "responseHandler",
            Optional = true,
            Kind = CodeParameterKind.ResponseHandler,
            Description = "Response handler to use in place of the default response handling provided by the core service",
            Type = new CodeType { Name = "IResponseHandler", IsExternal = true },
        };
        executorMethod.AddParameter(handlerParam);// Add response handler parameter

        var cancellationParam = new CodeParameter{
            Name = "cancellationToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Description = "Cancellation token to use when cancelling requests",
            Type = new CodeType { Name = "CancellationToken", IsExternal = true },
        };
        executorMethod.AddParameter(cancellationParam);// Add cancellation token parameter
        logger.LogTrace("Creating method {name} of {type}", executorMethod.Name, executorMethod.ReturnType);

        var generatorMethod = parentClass.AddMethod(new CodeMethod {
            Name = $"Create{operationType.ToString().ToFirstCharacterUpperCase()}RequestInformation",
            Kind = CodeMethodKind.RequestGenerator,
            IsAsync = false,
            HttpMethod = method,
            Description = (operation.Description ?? operation.Summary).CleanupDescription(),
            ReturnType = new CodeType { Name = "RequestInformation", IsNullable = false, IsExternal = true},
        }).FirstOrDefault();
        if (schema != null) {
            var mediaType = operation.Responses.Values.SelectMany(static x => x.Content).First(x => x.Value.Schema == schema).Key;
            generatorMethod.AcceptedResponseTypes.Add(mediaType);
        }
        if (config.Language == GenerationLanguage.Shell)
            SetPathAndQueryParameters(generatorMethod, currentNode, operation);
        AddRequestBuilderMethodParameters(currentNode, operationType, operation, parameterClass, requestConfigClass, generatorMethod);
        logger.LogTrace("Creating method {name} of {type}", generatorMethod.Name, generatorMethod.ReturnType);
    }
    private static readonly Func<OpenApiParameter, CodeParameter> GetCodeParameterFromApiParameter = x => {
        var codeName = x.Name.SanitizeParameterNameForCodeSymbols();
        return new CodeParameter
        {
            Name = codeName,
            SerializationName = codeName.Equals(x.Name) ? default : x.Name,
            Type = GetQueryParameterType(x.Schema),
            Description = x.Description.CleanupDescription(),
            Kind = x.In switch
                {
                    ParameterLocation.Query => CodeParameterKind.QueryParameter,
                    ParameterLocation.Header => CodeParameterKind.Headers,
                    ParameterLocation.Path => CodeParameterKind.Path,
                    _ => throw new NotSupportedException($"No matching parameter kind is supported for parameters in {x.In}"),
                },
            Optional = !x.Required
        };
    };
    private static readonly Func<OpenApiParameter, bool> ParametersFilter = x => x.In == ParameterLocation.Path || x.In == ParameterLocation.Query || x.In == ParameterLocation.Header;
    private static void SetPathAndQueryParameters(CodeMethod target, OpenApiUrlTreeNode currentNode, OpenApiOperation operation)
    {
        var pathAndQueryParameters = currentNode
            .PathItems[Constants.DefaultOpenApiLabel]
            .Parameters
            .Where(ParametersFilter)
            .Select(GetCodeParameterFromApiParameter)
            .Union(operation
                    .Parameters
                    .Where(ParametersFilter)
                    .Select(GetCodeParameterFromApiParameter))
            .ToArray();
        target.AddPathQueryOrHeaderParameter(pathAndQueryParameters);
    }

    private void AddRequestBuilderMethodParameters(OpenApiUrlTreeNode currentNode, OperationType operationType, OpenApiOperation operation, CodeClass parameterClass, CodeClass requestConfigClass, CodeMethod method) {
        if (operation.RequestBody?.Content?.GetValidSchemas(config.StructuredMimeTypes)?.FirstOrDefault() is OpenApiSchema requestBodySchema)
        {
            var requestBodyType = CreateModelDeclarations(currentNode, requestBodySchema, operation, method, $"{operationType}RequestBody");
            method.AddParameter(new CodeParameter {
                Name = "body",
                Type = requestBodyType,
                Optional = false,
                Kind = CodeParameterKind.RequestBody,
                Description = requestBodySchema.Description.CleanupDescription()
            });
            method.RequestBodyContentType = operation.RequestBody.Content.First(x => x.Value.Schema == requestBodySchema).Key;
        } else if (operation.RequestBody?.Content?.Any() ?? false) {
            var nParam = new CodeParameter {
                Name = "body",
                Optional = false,
                Kind = CodeParameterKind.RequestBody,
                Description = $"Binary request body",
                Type = new CodeType {
                    Name = "binary",
                    IsExternal = true,
                    IsNullable = false,
                },
            };
            method.AddParameter(nParam);
        }
        method.AddParameter(new CodeParameter {
            Name = "requestConfiguration",
            Optional = true,
            Type = new CodeType { Name = requestConfigClass.Name, TypeDefinition = requestConfigClass, ActionOf = true },
            Kind = CodeParameterKind.RequestConfiguration,
            Description = "Configuration for the request such as headers, query parameters, and middleware options.",
        });
        if(parameterClass != null) {
            requestConfigClass.AddProperty(new CodeProperty
            {
                Name = "queryParameters",
                Kind = CodePropertyKind.QueryParameters,
                Description = "Request query parameters",
                Type = new CodeType { Name = parameterClass.Name, TypeDefinition = parameterClass },
            });
        }
        requestConfigClass.AddProperty(new CodeProperty {
            Name = "headers",
            Kind = CodePropertyKind.Headers,
            Description = "Request headers",
            Type = new CodeType { Name = "IDictionary<string, string>", IsExternal = true },
        },
        new CodeProperty {
            Name = "options",
            Kind = CodePropertyKind.Options,
            Description = "Request options",
            Type = new CodeType { Name = "IList<IRequestOption>", IsExternal = true },
        });
    }
    private string GetModelsNamespaceNameFromReferenceId(string referenceId) {
        if (string.IsNullOrEmpty(referenceId)) return referenceId;
        if(referenceId.StartsWith(config.ClientClassName, StringComparison.OrdinalIgnoreCase)) // the client class having a namespace segment name can be problematic in some languages
            referenceId = referenceId[config.ClientClassName.Length..];
        referenceId = referenceId.Trim(nsNameSeparator);
        if(!string.IsNullOrEmpty(modelNamespacePrefixToTrim) && referenceId.StartsWith(modelNamespacePrefixToTrim, StringComparison.OrdinalIgnoreCase))
            referenceId = referenceId[modelNamespacePrefixToTrim.Length..];
        referenceId = referenceId.Trim(nsNameSeparator);
        var lastDotIndex = referenceId.LastIndexOf(nsNameSeparator);
        var namespaceSuffix = lastDotIndex != -1 ? $".{referenceId[..lastDotIndex]}" : string.Empty;
        return $"{modelsNamespace.Name}{namespaceSuffix}";
    }
    private CodeType CreateModelDeclarationAndType(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation operation, CodeNamespace codeNamespace, string classNameSuffix = "", OpenApiResponse response = default, string typeNameForInlineSchema = "") {
        var className = string.IsNullOrEmpty(typeNameForInlineSchema) ? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: classNameSuffix, response: response, schema: schema).CleanupSymbolName() : typeNameForInlineSchema;
        var codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, schema, className, codeNamespace);
        return new CodeType {
            TypeDefinition = codeDeclaration,
            Name = className,
        };
    }
    private CodeTypeBase CreateInheritedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation operation, CodeNamespace codeNamespace) {
        var allOfs = schema.AllOf.FlattenEmptyEntries(x => x.AllOf);
        CodeElement codeDeclaration = null;
        var className = string.Empty;
        var codeNamespaceFromParent = GetShortestNamespace(codeNamespace,schema);
        foreach(var currentSchema in allOfs) {
            var referenceId = GetReferenceIdFromOriginalSchema(currentSchema, schema);
            var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(referenceId);
            var shortestNamespace = string.IsNullOrEmpty(referenceId) ? codeNamespaceFromParent : rootNamespace.FindNamespaceByName(shortestNamespaceName);
            if(shortestNamespace == null)
                shortestNamespace = rootNamespace.AddNamespace(shortestNamespaceName);
            className = (currentSchema.GetSchemaName() ?? currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, schema: schema)).CleanupSymbolName();
            codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, currentSchema, className, shortestNamespace, codeDeclaration as CodeClass);
        }

        return new CodeType {
            TypeDefinition = codeDeclaration,
            Name = className,
        };
    }
    private static string GetReferenceIdFromOriginalSchema(OpenApiSchema schema, OpenApiSchema parentSchema) {
        var title = schema.Title;
        if(!string.IsNullOrEmpty(schema.Reference?.Id)) return schema.Reference.Id;
        else if (string.IsNullOrEmpty(title)) return string.Empty;
        if(parentSchema.Reference?.Id?.EndsWith(title, StringComparison.OrdinalIgnoreCase) ?? false) return parentSchema.Reference.Id;
        if(parentSchema.Items?.Reference?.Id?.EndsWith(title, StringComparison.OrdinalIgnoreCase) ?? false) return parentSchema.Items.Reference.Id;
        return (parentSchema.
                        AllOf
                        .FirstOrDefault(x => x.Reference?.Id?.EndsWith(title, StringComparison.OrdinalIgnoreCase) ?? false) ??
                parentSchema.
                        AnyOf
                        .FirstOrDefault(x => x.Reference?.Id?.EndsWith(title, StringComparison.OrdinalIgnoreCase) ?? false) ??
                parentSchema.
                        OneOf
                        .FirstOrDefault(x => x.Reference?.Id?.EndsWith(title, StringComparison.OrdinalIgnoreCase) ?? false))
            ?.Reference?.Id;
    }
    private CodeTypeBase CreateComposedModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation operation, string suffixForInlineSchema, CodeNamespace codeNamespace) {
        var typeName = currentNode.GetClassName(config.StructuredMimeTypes, operation: operation, suffix: suffixForInlineSchema, schema: schema).CleanupSymbolName();
        var typesCount = schema.AnyOf?.Count ?? schema.OneOf?.Count ?? 0;
        if ((typesCount == 1 && schema.Nullable && schema.IsAnyOf()) || // nullable on the root schema outside of anyOf
            typesCount == 2 && schema.AnyOf.Any(static x => // nullable on a schema in the anyOf
                                                        x.Nullable &&
                                                        !x.Properties.Any() &&
                                                        !x.IsOneOf() &&
                                                        !x.IsAnyOf() &&
                                                        !x.IsAllOf() &&
                                                        !x.IsArray() &&
                                                        !x.IsReferencedSchema())) { // once openAPI 3.1 is supported, there will be a third case oneOf with Ref and type null.
            var targetSchema = schema.AnyOf.First(static x => !string.IsNullOrEmpty(x.GetSchemaName()));
            var className = targetSchema.GetSchemaName().CleanupSymbolName();
            var shortestNamespace = GetShortestNamespace(codeNamespace, targetSchema);
            return new CodeType {
                TypeDefinition = AddModelDeclarationIfDoesntExist(currentNode, targetSchema, className, shortestNamespace),
                Name = className,
            };// so we don't create unnecessary union types when anyOf was used only for nullable.
        }
        var (unionType, schemas) = (schema.IsOneOf(), schema.IsAnyOf()) switch {
            (true, false) => (new CodeExclusionType {
                Name = typeName,
            } as CodeComposedTypeBase, schema.OneOf),
            (false, true) => (new CodeUnionType {
                Name = typeName,
            }, schema.AnyOf),
            (_, _) => throw new InvalidOperationException("Schema is not oneOf nor anyOf"),
        };
        var membersWithNoName = 0;
        foreach(var currentSchema in schemas) {
            var shortestNamespace = GetShortestNamespace(codeNamespace,currentSchema);
            var className = currentSchema.GetSchemaName().CleanupSymbolName();
            if (string.IsNullOrEmpty(className))
                if(GetPrimitiveType(currentSchema) is CodeType primitiveType && !string.IsNullOrEmpty(primitiveType.Name)) {
                    unionType.AddType(primitiveType);
                    continue;
                } else
                    className = $"{unionType.Name}Member{++membersWithNoName}";
            var codeDeclaration = AddModelDeclarationIfDoesntExist(currentNode, currentSchema, className, shortestNamespace);
            unionType.AddType(new CodeType {
                TypeDefinition = codeDeclaration,
                Name = className,
            });
        }
        return unionType;
    }
    private CodeTypeBase CreateModelDeclarations(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation operation, CodeElement parentElement, string suffixForInlineSchema, OpenApiResponse response = default, string typeNameForInlineSchema = default)
    {
        var codeNamespace = parentElement.GetImmediateParentOfType<CodeNamespace>();

        if (!schema.IsReferencedSchema() && schema.Properties.Any()) { // Inline schema, i.e. specific to the Operation
            return CreateModelDeclarationAndType(currentNode, schema, operation, codeNamespace, suffixForInlineSchema, typeNameForInlineSchema: typeNameForInlineSchema);
        } else if(schema.IsAllOf()) {
            return CreateInheritedModelDeclaration(currentNode, schema, operation, codeNamespace);
        } else if((schema.IsAnyOf() || schema.IsOneOf()) && string.IsNullOrEmpty(schema.Format)) {
            return CreateComposedModelDeclaration(currentNode, schema, operation, suffixForInlineSchema, codeNamespace);
        } else if(schema.IsObject() || schema.Properties.Any() || schema.Enum.Any()) {
            // referenced schema, no inheritance or union type
            var targetNamespace = GetShortestNamespace(codeNamespace, schema);
            return CreateModelDeclarationAndType(currentNode, schema, operation, targetNamespace, response: response, typeNameForInlineSchema: typeNameForInlineSchema);
        } else if (schema.IsArray()) {
            // collections at root
            return CreateCollectionModelDeclaration(currentNode, schema, operation, codeNamespace, typeNameForInlineSchema);
        } else if(!string.IsNullOrEmpty(schema.Type) || !string.IsNullOrEmpty(schema.Format))
            return GetPrimitiveType(schema, string.Empty);
        else if(schema.AnyOf.Any() || schema.OneOf.Any() || schema.AllOf.Any()) // we have an empty node because of some local override for schema properties and need to unwrap it.
            return CreateModelDeclarations(currentNode, schema.AnyOf.FirstOrDefault() ?? schema.OneOf.FirstOrDefault() ?? schema.AllOf.FirstOrDefault(), operation, parentElement, suffixForInlineSchema, response, typeNameForInlineSchema);
        else throw new InvalidSchemaException("unhandled case, might be object type or array type");
    }
    private CodeTypeBase CreateCollectionModelDeclaration(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, OpenApiOperation operation, CodeNamespace codeNamespace, string typeNameForInlineSchema = default)
    {
        CodeTypeBase type = GetPrimitiveType(schema?.Items, string.Empty);
        if (type == null || string.IsNullOrEmpty(type.Name))
        {
            var targetNamespace = schema?.Items == null ? codeNamespace : GetShortestNamespace(codeNamespace, schema.Items);
            type = CreateModelDeclarations(currentNode, schema?.Items, operation, targetNamespace, default , typeNameForInlineSchema: typeNameForInlineSchema);
        }
        type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
        return type;
    }
    private CodeElement GetExistingDeclaration(CodeNamespace currentNamespace, OpenApiUrlTreeNode currentNode, string declarationName) {
        var localNameSpace = GetSearchNamespace(currentNode, currentNamespace);
        return localNameSpace.FindChildByName<ITypeDefinition>(declarationName, false) as CodeElement;
    }
    private CodeNamespace GetSearchNamespace(OpenApiUrlTreeNode currentNode, CodeNamespace currentNamespace) {
        if (currentNode.DoesNodeBelongToItemSubnamespace() && !currentNamespace.Name.Contains(modelsNamespace.Name))
            return currentNamespace.EnsureItemNamespace();
        else
            return currentNamespace;
    }
    private CodeElement AddModelDeclarationIfDoesntExist(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, string declarationName, CodeNamespace currentNamespace, CodeClass inheritsFrom = null) {
        var existingDeclaration = GetExistingDeclaration(currentNamespace, currentNode, declarationName);
        if(existingDeclaration == null) // we can find it in the components
        {
            if(schema.Enum.Any()) {
                var newEnum = new CodeEnum { 
                    Name = declarationName,//TODO set the flag property
                    Description = currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel),
                };
                SetEnumOptions(schema, newEnum);
                return currentNamespace.AddEnum(newEnum).First();
            } else 
                return AddModelClass(currentNode, schema, declarationName, currentNamespace, inheritsFrom);
        } else
            return existingDeclaration;
    }
    private static void SetEnumOptions(OpenApiSchema schema, CodeEnum target) {
        OpenApiEnumValuesDescriptionExtension extensionInformation = null;
        if (schema.Extensions.TryGetValue(OpenApiEnumValuesDescriptionExtension.Name, out var rawExtension) && rawExtension is OpenApiEnumValuesDescriptionExtension localExtInfo)
            extensionInformation = localExtInfo;
        var entries = schema.Enum.OfType<OpenApiString>().Where(static x => !x.Value.Equals("null", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(x.Value)).Select(static x => x.Value);
        foreach(var enumValue in entries) {
            var optionDescription = extensionInformation?.ValuesDescriptions.FirstOrDefault(x => x.Value.Equals(enumValue, StringComparison.OrdinalIgnoreCase));
            var newOption = new CodeEnumOption {
                Name = (optionDescription?.Name ?? enumValue).CleanupSymbolName(),
                SerializationName = !string.IsNullOrEmpty(optionDescription?.Name) ? enumValue : null,
                Description = optionDescription?.Description,
            };
            if(!string.IsNullOrEmpty(newOption.Name))
                target.AddOption(newOption);
        }
    }
    private CodeNamespace GetShortestNamespace(CodeNamespace currentNamespace, OpenApiSchema currentSchema) {
        if(!string.IsNullOrEmpty(currentSchema.Reference?.Id)) {
            var parentClassNamespaceName = GetModelsNamespaceNameFromReferenceId(currentSchema.Reference.Id);
            return rootNamespace.AddNamespace(parentClassNamespaceName);
        }
        return currentNamespace;
    }
    private CodeClass AddModelClass(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, string declarationName, CodeNamespace currentNamespace, CodeClass inheritsFrom = null) {
        var referencedAllOfs = schema.AllOf.Where(x => x.Reference != null);
        if(inheritsFrom == null && referencedAllOfs.Any()) {// any non-reference would be the current class in some description styles
            var parentSchema = referencedAllOfs.FirstOrDefault();
            if(parentSchema != null) {
                var parentClassNamespace = GetShortestNamespace(currentNamespace, parentSchema);
                inheritsFrom = AddModelDeclarationIfDoesntExist(currentNode, parentSchema, parentSchema.GetSchemaName().CleanupSymbolName(), parentClassNamespace) as CodeClass;
            }
        }
        var newClass = currentNamespace.AddClass(new CodeClass {
            Name = declarationName,
            Kind = CodeClassKind.Model,
            Description = schema.Description.CleanupDescription() ?? (string.IsNullOrEmpty(schema.Reference?.Id) ? 
                                                    currentNode.GetPathItemDescription(Constants.DefaultOpenApiLabel) :
                                                    null),// if it's a referenced component, we shouldn't use the path item description as it makes it indeterministic
        }).First();
        if(inheritsFrom != null)
            newClass.StartBlock.Inherits = new CodeType { TypeDefinition = inheritsFrom, Name = inheritsFrom.Name };

        // Find the correct discriminator instance to use
        OpenApiDiscriminator discriminator = null;
        if (schema.Discriminator?.Mapping?.Any() ?? false) 
            discriminator = schema.Discriminator; // use the discriminator directly in the schema  
        else if(schema.AllOf?.LastOrDefault(x => x.IsObject())?.Discriminator?.Mapping?.Any() ?? false)  
            discriminator = schema.AllOf.Last(x => x.IsObject()).Discriminator; // discriminator mapping in the last AllOf object representation

        var factoryMethod = AddDiscriminatorMethod(newClass, discriminator?.PropertyName);
        
        CreatePropertiesForModelClass(currentNode, schema, currentNamespace, newClass); // order matters since we might be recursively generating ancestors for discriminator mappings and duplicating additional data/backing store properties
        
        if (discriminator?.Mapping?.Any() ?? false)
            discriminator.Mapping
                .Where(x => !x.Key.TrimStart('#').Equals(schema.Reference?.Id, StringComparison.OrdinalIgnoreCase))
                .Select(x => (x.Key, GetCodeTypeForMapping(currentNode, x.Value, currentNamespace, newClass, schema)))
                .Where(x => x.Item2 != null)
                .ToList()
                .ForEach(x => factoryMethod.AddDiscriminatorMapping(x.Key, x.Item2));

        return newClass;
    }
    public static CodeMethod AddDiscriminatorMethod(CodeClass newClass, string discriminatorPropertyName) {
        var factoryMethod = newClass.AddMethod(new CodeMethod {
            Name = "CreateFromDiscriminatorValue",
            Description = "Creates a new instance of the appropriate class based on discriminator value",
            ReturnType = new CodeType { TypeDefinition = newClass, Name = newClass.Name, IsNullable = false },
            Kind = CodeMethodKind.Factory,
            IsStatic = true,
            IsAsync = false,
        }).First();
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Description = "The parse node to use to read the discriminator value and create the object",
            Optional = false,
            Type = new CodeType { Name = ParseNodeInterface, IsExternal = true },
        });
        factoryMethod.DiscriminatorPropertyName = discriminatorPropertyName;
        return factoryMethod;
    }
    private CodeTypeBase GetCodeTypeForMapping(OpenApiUrlTreeNode currentNode, string referenceId, CodeNamespace currentNamespace, CodeClass currentClass, OpenApiSchema currentSchema) {
        var componentKey = referenceId.Replace("#/components/schemas/", string.Empty);
        if(!openApiDocument.Components.Schemas.TryGetValue(componentKey, out var discriminatorSchema)) {
            logger.LogWarning("Discriminator {componentKey} not found in the OpenAPI document.", componentKey);
            return null;
        }
        var className = currentNode.GetClassName(config.StructuredMimeTypes, schema: discriminatorSchema).CleanupSymbolName();
        var shouldInherit = discriminatorSchema.AllOf.Any(x => currentSchema.Reference?.Id.Equals(x.Reference?.Id, StringComparison.OrdinalIgnoreCase) ?? false);
        var codeClass = AddModelDeclarationIfDoesntExist(currentNode, discriminatorSchema, className, GetShortestNamespace(currentNamespace, discriminatorSchema), shouldInherit ? currentClass : null);
        return new CodeType {
            Name = codeClass.Name,
            TypeDefinition = codeClass,
        };
    }
    private void CreatePropertiesForModelClass(OpenApiUrlTreeNode currentNode, OpenApiSchema schema, CodeNamespace ns, CodeClass model) {

        var includeAdditionalDataProperties = config.IncludeAdditionalData &&
            (schema?.AdditionalPropertiesAllowed ?? false);

        AddSerializationMembers(model, includeAdditionalDataProperties, config.UsesBackingStore);
        if(schema?.Properties?.Any() ?? false)
        {
            model.AddProperty(schema
                                .Properties
                                .Select(x => {
                                    var propertySchema = x.Value;
                                    var className = propertySchema.GetSchemaName().CleanupSymbolName();
                                    if(string.IsNullOrEmpty(className))
                                        className = $"{model.Name}_{x.Key}";
                                    var shortestNamespaceName = GetModelsNamespaceNameFromReferenceId(propertySchema.Reference?.Id);
                                    var targetNamespace = string.IsNullOrEmpty(shortestNamespaceName) ? ns : 
                                                            (rootNamespace.FindNamespaceByName(shortestNamespaceName) ?? rootNamespace.AddNamespace(shortestNamespaceName));
                                    #if RELEASE
                                    try {
                                    #endif
                                        var definition = CreateModelDeclarations(currentNode, propertySchema, default, targetNamespace, default, typeNameForInlineSchema: className);
                                        return CreateProperty(x.Key, definition.Name, typeSchema: propertySchema, existingType: definition);
                                    #if RELEASE
                                    } catch (InvalidSchemaException ex) {
                                        throw new InvalidOperationException($"Error creating property {x.Key} for model {model.Name} in API path {currentNode.Path}, the schema is invalid.", ex);
                                    }
                                    #endif
                                })
                                .ToArray());
        }
        else if(schema?.AllOf?.Any(x => x.IsObject()) ?? false)
            CreatePropertiesForModelClass(currentNode, schema.AllOf.Last(x => x.IsObject()), ns, model);
    }
    private const string FieldDeserializersMethodName = "GetFieldDeserializers";
    private const string SerializeMethodName = "Serialize";
    private const string AdditionalDataPropName = "AdditionalData";
    private const string BackingStorePropertyName = "BackingStore";
    private const string BackingStoreInterface = "IBackingStore";
    private const string BackedModelInterface = "IBackedModel";
    private const string ParseNodeInterface = "IParseNode";
    internal const string AdditionalHolderInterface = "IAdditionalDataHolder";
    internal static void AddSerializationMembers(CodeClass model, bool includeAdditionalProperties, bool usesBackingStore) {
        var serializationPropsType = $"IDictionary<string, Action<{ParseNodeInterface}>>";
        if(!model.ContainsMember(FieldDeserializersMethodName)) {
            var deserializeProp = new CodeMethod {
                Name = FieldDeserializersMethodName,
                Kind = CodeMethodKind.Deserializer,
                Access = AccessModifier.Public,
                Description = "The deserialization information for the current model",
                IsAsync = false,
                ReturnType = new CodeType {
                    Name = serializationPropsType,
                    IsNullable = false,
                    IsExternal = true,
                },
            };
            model.AddMethod(deserializeProp);
        }
        if(!model.ContainsMember(SerializeMethodName)) {
            var serializeMethod = new CodeMethod {
                Name = SerializeMethodName,
                Kind = CodeMethodKind.Serializer,
                IsAsync = false,
                Description = $"Serializes information the current object",
                ReturnType = new CodeType { Name = voidType, IsNullable = false, IsExternal = true },
            };
            var parameter = new CodeParameter {
                Name = "writer",
                Description = "Serialization writer to use to serialize this model",
                Kind = CodeParameterKind.Serializer,
                Type = new CodeType { Name = "ISerializationWriter", IsExternal = true, IsNullable = false },
            };
            serializeMethod.AddParameter(parameter);
            
            model.AddMethod(serializeMethod);
        }
        if(!model.ContainsMember(AdditionalDataPropName) &&
            includeAdditionalProperties && 
            !(model.GetGreatestGrandparent(model)?.ContainsMember(AdditionalDataPropName) ?? false)) {
            // we don't want to add the property if the parent already has it
            var additionalDataProp = new CodeProperty {
                Name = AdditionalDataPropName,
                Access = AccessModifier.Public,
                DefaultValue = "new Dictionary<string, object>()",
                Kind = CodePropertyKind.AdditionalData,
                Description = "Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.",
                Type = new CodeType {
                    Name = "IDictionary<string, object>",
                    IsNullable = false,
                    IsExternal = true,
                },
            };
            model.AddProperty(additionalDataProp);
            model.StartBlock.AddImplements(new CodeType {
                Name = AdditionalHolderInterface,
                IsExternal = true,
            });
        }
        if(!model.ContainsMember(BackingStorePropertyName) &&
            usesBackingStore &&
            !(model.GetGreatestGrandparent(model)?.ContainsMember(BackingStorePropertyName) ?? false)) {
            var backingStoreProperty = new CodeProperty {
                Name = BackingStorePropertyName,
                Access = AccessModifier.Public,
                DefaultValue = $"BackingStoreFactorySingleton.Instance.CreateBackingStore()",
                Kind = CodePropertyKind.BackingStore,
                Description = "Stores model information.",
                ReadOnly = true,
                Type = new CodeType {
                    Name = BackingStoreInterface,
                    IsNullable = false,
                    IsExternal = true,
                },
            };
            model.AddProperty(backingStoreProperty);
            model.StartBlock.AddImplements(new CodeType {
                Name = BackedModelInterface,
                IsExternal = true,
            });
        }
    }
    private CodeClass CreateOperationParameterClass(OpenApiUrlTreeNode node, OperationType operationType, OpenApiOperation operation, CodeClass parentClass)
    {
        var parameters = node.PathItems[Constants.DefaultOpenApiLabel].Parameters.Union(operation.Parameters).Where(p => p.In == ParameterLocation.Query);
        if(parameters.Any()) {
            var parameterClass = parentClass.AddInnerClass(new CodeClass
            {
                Name = $"{parentClass.Name}{operationType}QueryParameters",
                Kind = CodeClassKind.QueryParameters,
                Description = (operation.Description ?? operation.Summary).CleanupDescription(),
            }).First();
            foreach (var parameter in parameters)
                AddPropertyForParameter(parameter, parameterClass);
                
            return parameterClass;
        } else return null;
    }
    private void AddPropertyForParameter(OpenApiParameter parameter, CodeClass parameterClass) {
        var prop = new CodeProperty
        {
            Name = parameter.Name.SanitizeParameterNameForCodeSymbols(),
            Description = parameter.Description.CleanupDescription(),
            Kind = CodePropertyKind.QueryParameter,
            Type = GetPrimitiveType(parameter.Schema),
        };
        prop.Type.CollectionKind = parameter.Schema.IsArray() ? CodeTypeBase.CodeTypeCollectionKind.Array : default;
        if(string.IsNullOrEmpty(prop.Type.Name) && prop.Type is CodeType parameterType) {
            // since its a query parameter default to string if there is no schema
            // it also be an object type, but we'd need to create the model in that case and there's no standard on how to serialize those as query parameters
            parameterType.Name = "string";
            parameterType.IsExternal = true;
        }

        if(!parameter.Name.Equals(prop.Name))
        {
            prop.SerializationName = parameter.Name.SanitizeParameterNameForUrlTemplate();
        }

        if (!parameterClass.ContainsMember(parameter.Name))
        {
            parameterClass.AddProperty(prop);
        }
        else
        {
            logger.LogWarning("Ignoring duplicate parameter {name}", parameter.Name);
        }
    }
    private static CodeType GetQueryParameterType(OpenApiSchema schema) =>
        new()
        {
            IsExternal = true,
            Name = schema.Items?.Type ?? schema.Type,
            CollectionKind = schema.IsArray() ? CodeType.CodeTypeCollectionKind.Array : default,
        };
}
