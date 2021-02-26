using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace kiota.core
{
    public class KiotaBuilder
    {
        private ILogger<KiotaBuilder> logger;
        private GenerationConfiguration config;

        public KiotaBuilder(ILogger<KiotaBuilder> logger, GenerationConfiguration config)
        {
            this.logger = logger;
            this.config = config;
        }

        public async Task GenerateSDK()
        {
            // Step 1 - Read input stream
            string inputPath = config.OpenAPIFilePath;
            using var input = await LoadStream(inputPath);

            // Step 2 - Parse OpenAPI
            var doc = CreateOpenApiDocument(input);

            // Step 3 - Create Uri Space of API
            var openApiTree = CreateUriSpace(doc);

            // Step 4 - Create Source Model
            var generatedCode = CreateSourceModel(openApiTree);

            // Step 5 - RefineByLanguage
            ApplyLanguageRefinement(config.Language, generatedCode);

            // Step 5 - Write language source 
            await CreateLanguageSourceFilesAsync(config.Language, generatedCode);
        }


        private async Task<Stream> LoadStream(string inputPath)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Stream input;
            if (inputPath.StartsWith("http"))
            {
                using var httpClient = new HttpClient();
                input = await httpClient.GetStreamAsync(inputPath);
            }
            else
            {
                input = new FileStream(inputPath, FileMode.Open);
            }
            stopwatch.Stop();
            logger.LogInformation("{timestamp}ms: Read OpenAPI file {file}", stopwatch.ElapsedMilliseconds, inputPath);
            return input;
        }


        public OpenApiDocument CreateOpenApiDocument(Stream input)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            logger.LogInformation("Parsing OpenAPI file");
            var reader = new OpenApiStreamReader();
            var doc = reader.Read(input, out var diag);
            stopwatch.Stop();
            if (diag.Errors.Count > 0)
            {
                logger.LogError("{timestamp}ms: OpenApi Parsing errors", stopwatch.ElapsedMilliseconds, String.Join(Environment.NewLine, diag.Errors.Select(e => e.Message)));
            }
            else
            {
                logger.LogInformation("{timestamp}ms: Parsed OpenAPI successfully. {count} paths found.", stopwatch.ElapsedMilliseconds, doc.Paths.Count);
            }

            return doc;
        }

        /// <summary>
        /// Translate OpenApi PathItems into a tree structure that will define the classes
        /// </summary>
        /// <param name="doc">OpenAPI Document of the API to be processed</param>
        /// <returns>Root node of the API URI space</returns>
        public OpenApiUrlSpaceNode CreateUriSpace(OpenApiDocument doc)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var node = OpenApiUrlSpaceNode.Create(doc);

            stopwatch.Stop();
            logger.LogInformation("{timestamp}ms: Created UriSpace tree", stopwatch.ElapsedMilliseconds);
            return node;
        }

        /// <summary>
        /// Convert UriSpace of OpenApiPathItems into conceptual SDK Code model 
        /// </summary>
        /// <param name="root">Root OpenApiUriSpaceNode of API to be generated</param>
        /// <returns></returns>
        public CodeNamespace CreateSourceModel(OpenApiUrlSpaceNode root)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var rootPlaceholder = CodeNamespace.InitRootNamespace();
            var codeNamespace = rootPlaceholder.AddNamespace(this.config.ClientNamespaceName);
            CreateRequestBuilderClass(codeNamespace, root, root);
            MapTypeDefinitions(codeNamespace);

            stopwatch.Stop();
            logger.LogInformation("{timestamp}ms: Created source model with {count} classes", stopwatch.ElapsedMilliseconds, codeNamespace.InnerChildElements.Count);

            return rootPlaceholder;
        }

        /// <summary>
        /// Manipulate CodeDOM for language specific issues
        /// </summary>
        /// <param name="language"></param>
        /// <param name="generatedCode"></param>
        public void ApplyLanguageRefinement(GenerationLanguage language, CodeNamespace generatedCode)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ILanguageRefiner.Refine(language, generatedCode);

            stopwatch.Stop();
            logger.LogInformation("{timestamp}ms: Language refinement applied", stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Iterate through Url Space and create request builder classes for each node in the tree
        /// </summary>
        /// <param name="root">Root node of URI space from the OpenAPI described API</param>
        /// <returns>A CodeNamespace object that contains request builder classes for the Uri Space</returns>

        public async Task CreateLanguageSourceFilesAsync(GenerationLanguage language, CodeNamespace generatedCode)
        {
            LanguageWriter languageWriter;
            switch (language)
            {
                case GenerationLanguage.CSharp:
                    languageWriter = new CSharpWriter(this.config.OutputPath, this.config.ClientNamespaceName);
                    break;
                case GenerationLanguage.Java:
                    languageWriter = new JavaWriter(this.config.OutputPath, this.config.ClientNamespaceName);
                    break;
                case GenerationLanguage.TypeScript:
                    languageWriter = new TypeScriptWriter(this.config.OutputPath, this.config.ClientNamespaceName);
                    break;
                default:
                    throw new ArgumentException($"{language} language currently not supported.");
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await CodeRenderer.RenderCodeNamespaceToFilePerClassAsync(languageWriter, generatedCode);
            stopwatch.Stop();
            logger.LogInformation("{timestamp}ms: Files written to {path}", stopwatch.ElapsedMilliseconds, config.OutputPath);
        }
        private static readonly string requestBuilderSuffix = "RequestBuilder";

        /// <summary>
        /// Create a CodeClass instance that is a request builder class for the OpenApiUrlSpaceNode
        /// </summary>
        private void CreateRequestBuilderClass(CodeNamespace codeNamespace, OpenApiUrlSpaceNode currentNode, OpenApiUrlSpaceNode rootNode)
        {
            // Determine Class Name
            CodeClass codeClass;
            var isRootClientClass = String.IsNullOrEmpty(currentNode.Identifier);
            if (isRootClientClass)
            {
                codeClass = new CodeClass(codeNamespace) { Name = this.config.ClientClassName, ClassKind = CodeClassKind.RequestBuilder };
            }
            else
            {
                var className = currentNode.GetClassName(requestBuilderSuffix);
                codeClass = new CodeClass((currentNode.DoesNodeBelongToItemSubnamespace() ? codeNamespace.EnsureItemNamespace() : codeNamespace)) {
                    Name = className, 
                    ClassKind = CodeClassKind.RequestBuilder
                };
            }

            logger.LogDebug("Creating class {class}", codeClass.Name);

            // Add properties for children
            foreach (var child in currentNode.Children)
            {
                var propIdentifier = child.Value.GetClassName();
                var propType = propIdentifier + requestBuilderSuffix;
                if (child.Value.IsParameter())
                {
                    var prop = CreateIndexer(propIdentifier, propType, codeClass);
                    codeClass.SetIndexer(prop);
                }
                else if (child.Value.IsFunction())
                {
                    // Don't support functions for the moment
                }
                else
                {
                    var prop = CreateProperty(propIdentifier, propType, codeClass, kind: CodePropertyKind.RequestBuilder); // we should add the type definition here but we can't as it might not have been generated yet
                    codeClass.AddProperty(prop);
                }
            }

            // Add methods for Operations
            if (currentNode.HasOperations())
            {
                foreach(var operation in currentNode.PathItem.Operations)
                {
                    var parameterClass = CreateOperationParameter(currentNode, operation, codeClass);

                    var method = CreateOperationMethod(rootNode, currentNode, operation.Key, operation.Value, parameterClass, codeClass);
                    logger.LogDebug("Creating method {name} of {type}", method.Name, method.ReturnType);
                    codeClass.AddMethod(method);
                }

                CreateResponseHandler(codeClass);
            }
            CreatePathManagement(codeClass, currentNode, isRootClientClass);
           

            (currentNode.DoesNodeBelongToItemSubnamespace() ? codeNamespace.EnsureItemNamespace() : codeNamespace).AddClass(codeClass);

            var rootNamespace = codeNamespace.GetRootNamespace();
            foreach (var childNode in currentNode.Children.Values)
            {
                var targetNamespaceName = childNode.GetNodeNamespaceFromPath(this.config.ClientNamespaceName);
                var targetNamespace = rootNamespace.GetNamespace(targetNamespaceName) ?? rootNamespace.AddNamespace(targetNamespaceName);
                CreateRequestBuilderClass(targetNamespace, childNode, rootNode);
            }
        }

        private void CreatePathManagement(CodeClass currentClass, OpenApiUrlSpaceNode currentNode, bool isRootClientClass) {
            var pathProperty = new CodeProperty(currentClass) {
                Access = AccessModifier.Private,
                Name = "pathSegment",
                DefaultValue = isRootClientClass ? $"\"{this.config.ApiRootUrl}\"" : (currentNode.IsParameter() ? "\"\"" : $"\"/{currentNode.Segment}\""),
                ReadOnly = true,
            };
            pathProperty.Type = new CodeType(pathProperty) {
                Name = "string",
                IsNullable = false,
            };
            currentClass.AddProperty(pathProperty);

            var currentPathProperty = new CodeProperty(currentClass) {
                Name = "currentPath"
            };
            currentPathProperty.Type = new CodeType(currentPathProperty) {
                Name = "string"
            };
            currentClass.AddProperty(currentPathProperty);

            var httpCoreProperty = new CodeProperty(currentClass) {
                Name = "httpCore"
            };
            httpCoreProperty.Type = new CodeType(httpCoreProperty) {
                Name = "IHttpCore",
                IsExternal = true
            };
            currentClass.AddProperty(httpCoreProperty);
        }

        /// <summary>
        /// Remaps definitions to custom types so they can be used later in generation or in refiners
        /// </summary>
        private void MapTypeDefinitions(CodeElement codeElement) {
            switch(codeElement) {
                case CodeMethod method:
                    MapTypeDefinition(method.Parameters.Select(x => x.Type).ToArray());
                break;
                case CodeProperty property:
                    MapTypeDefinition(property.Type);
                break;
                case CodeIndexer indexer:
                    MapTypeDefinition(indexer.ReturnType);
                break;
                case CodeParameter parameter:
                    MapTypeDefinition(parameter.Type);
                break;
            }
            foreach(var childElement in codeElement.GetChildElements())
                MapTypeDefinitions(childElement);
        }
        private void MapTypeDefinition(params CodeType[] currentTypes) {
            foreach(var currentType in currentTypes.Where(x => x.TypeDefinition == null))
                currentType.TypeDefinition = currentType
                        .GetImmediateParentOfType<CodeNamespace>()
                        .GetRootNamespace()
                        .GetChildElementOfType<CodeClass>(x => x.Name == currentType.Name);
        }

        private CodeIndexer CreateIndexer(string childIdentifier, string childType, CodeClass codeClass)
        {
            var prop = new CodeIndexer(codeClass)
            {
                Name = childIdentifier,
            };
            prop.IndexType = new CodeType(prop) { Name = "string" };
            prop.ReturnType = new CodeType(prop)
            {
                Name = childType
            };
            logger.LogDebug("Creating indexer {name}", childIdentifier);
            return prop;
        }

        private CodeProperty CreateProperty(string childIdentifier, string childType, CodeClass codeClass, string defaultValue = null, OpenApiSchema typeSchema = null, CodeClass typeDefinition = null, CodePropertyKind kind = CodePropertyKind.Custom)
        {
            var isCollection = typeSchema?.Type?.Equals("array", StringComparison.CurrentCultureIgnoreCase) ?? false;
            var propertyName = childIdentifier;
            this.config.PropertiesPrefixToStrip.ForEach(x => propertyName = propertyName.Replace(x, string.Empty));
            var prop = new CodeProperty(codeClass)
            {
                Name = propertyName,
                DefaultValue = defaultValue,
                PropertyKind = kind,
            };
            prop.Type = new CodeType(prop) {
                Name = isCollection ? typeSchema?.Items?.Reference?.GetClassName() : childType,
                Schema = typeSchema,
                TypeDefinition = typeDefinition,
                CollectionKind = isCollection ? CodeType.CodeTypeCollectionKind.Complex : default
            };
            logger.LogDebug("Creating property {name} of {type}", prop.Name, prop.Type.Name);
            return prop;
        }

        private CodeMethod CreateOperationMethod(OpenApiUrlSpaceNode rootNode, OpenApiUrlSpaceNode currentNode, OperationType operationType, OpenApiOperation operation, CodeClass parameterClass, CodeClass parentClass)
        {
            var schema = operation.GetResponseSchema();
            var method = new CodeMethod(parentClass) {
                Name = operationType.ToString(),
                MethodKind = CodeMethodKind.RequestExecutor,
            };
            if (schema != null)
            {
                var returnType = CreateModelClasses(rootNode, currentNode, schema, operation, method);
                method.ReturnType = returnType ?? new CodeType(method) { Name = "object"}; //TODO remove this temporary default when the method above handles all cases
            } else 
                method.ReturnType = new CodeType(method) { Name = "object"};

            

            if (operation.RequestBody != null)
            {
                var requestBodyType = CreateRequestModelClasses(operation.RequestBody, operation);
                // method.AddParameter(new CodeParameter {
                //     Name = "body",
                //     Type = requestBodyType,
                //     Optional = false,
                // });
            }
            if(parameterClass != null) {
                var qsParam = new CodeParameter(method)
                {
                    Name = "q",
                    Optional = true,
                    ParameterKind = CodeParameterKind.QueryParameter
                };
                qsParam.Type = new CodeType(qsParam) { Name = parameterClass.Name, ActionOf = true, TypeDefinition = parameterClass };
                method.AddParameter(qsParam);
            }
            var headersParam = new CodeParameter(method) {
                Name = "h",
                Optional = true,
                ParameterKind = CodeParameterKind.Headers,
            };
            headersParam.Type = new CodeType(headersParam) { Name = "IDictionary<string, string>", ActionOf = true };
            method.AddParameter(headersParam);
            return method;
        }

        private CodeType CreateRequestModelClasses(OpenApiRequestBody requestBody, OpenApiOperation operation)
        {
            //TODO:
            //if has reference, go find the type with the same id
            //else insert inner declaration
            // if(requestBody.Reference == null) {
            //     return new CodeType {
            //       TypeDefinition = new CodeClass {

            //       }  
            //     };
            // }
            return null;
            // use the type declaration /reference for the operation parameter declaration
        }
        private IEnumerable<string> GetAllNamespaceNamesForModelByReferenceId(OpenApiUrlSpaceNode currentNode, string referenceId) {
            if(string.IsNullOrEmpty(referenceId)) throw new ArgumentNullException(nameof(referenceId));
            var currentNodePath = currentNode
                                    ?.PathItem
                                    ?.Operations
                                    ?.Values
                                    ?.Any(x => x?.Responses
                                                    ?.Values
                                                    ?.SelectMany(y => y.Content.Values)
                                                    ?.Select(y => y.Schema)
                                                    ?.Any(y => referenceId.Equals(y?.Reference?.Id)) ?? false) ?? false ?
                                                    new List<string>() { currentNode.GetNodeNamespaceFromPath(this.config.ClientNamespaceName) }:
                                                    Enumerable.Empty<string>();
            if(currentNode?.Children?.Any() ?? false)
                return currentNodePath
                        .Union(currentNode.Children.Values.SelectMany(x => GetAllNamespaceNamesForModelByReferenceId(x, referenceId)));
            else return currentNodePath;
        }
        private string GetNamespaceNameForModelByOperationId(string operationId) {
            if(string.IsNullOrEmpty(operationId)) throw new ArgumentNullException(nameof(operationId));
            var cleanOperationId = operationId.Split('_').First();
            return $"{this.config.ClientNamespaceName}.{cleanOperationId}";
        }
        private string GetShortestNamespaceNameForModelByReferenceId(OpenApiUrlSpaceNode rootNode, string referenceId) {
            return GetAllNamespaceNamesForModelByReferenceId(rootNode, referenceId)
                    .OrderBy(x => x.Count(y => y == '.'))
                    .FirstOrDefault();
        }
        private CodeType CreateModelClasses(OpenApiUrlSpaceNode rootNode, OpenApiUrlSpaceNode currentNode, OpenApiSchema schema, OpenApiOperation operation, CodeElement parentElement)
        {
            var originalReference = schema?.Reference;
            var originalReferenceId = originalReference?.Id;
            var codeNamespace = parentElement.GetImmediateParentOfType<CodeNamespace>();
            if(schema?.AllOf?.Any() ?? false)
                schema = schema.AllOf.Last();
            // object type
            // array of object
            // all of object
            // one of object
            // any of object

            
            if (originalReference == null)  // Inline schema, i.e. specific to the Operation
            {
                var namespaceName = GetNamespaceNameForModelByOperationId(operation.OperationId);
                var ns = codeNamespace.GetRootNamespace().GetNamespace(namespaceName);
                if(ns == null)
                    ns = codeNamespace.AddNamespace(namespaceName);
                var className = currentNode.GetClassName(operation: operation, suffix: "Response");
                // var propertiesSchemasFromReferences = schema.Properties.Select(x => x.Value)
                //                                             .Where(x => x.Items != null)
                //                                             .Select(x => x.Items)
                //                                             .Where(x => x.Reference != null);
                //                                             // .Select(x => x.Reference);
                var codeClass = AddModelClassIfDoesntExit(rootNode, currentNode, schema, operation, className, ns, parentElement);
                // foreach(var propertySchemasFromReferences in propertiesSchemasFromReferences)
                //     CreateModelClasses(rootNode, currentNode, propertySchemasFromReferences, operation, codeClass);
                return new CodeType(parentElement) {
                    TypeDefinition = codeClass,
                    Name = className,
                    Schema = schema
                };
            } else  // Reused schema from components
            {
                var className = currentNode.GetClassName(operation: operation);
                var shortestNamespaceName = GetShortestNamespaceNameForModelByReferenceId(rootNode, originalReferenceId);
                var shortestNamespace = codeNamespace.GetRootNamespace().GetNamespace(shortestNamespaceName);
                if(shortestNamespace == null)
                    shortestNamespace = codeNamespace.AddNamespace(shortestNamespaceName);
                var codeClass = AddModelClassIfDoesntExit(rootNode, currentNode, schema, operation, className, shortestNamespace, parentElement);
                return new CodeType(parentElement) {
                    TypeDefinition = codeClass,
                    Name = className,
                    Schema = schema
                };
            }
        }
        private CodeClass AddModelClassIfDoesntExit(OpenApiUrlSpaceNode rootNode, OpenApiUrlSpaceNode currentNode, OpenApiSchema schema, OpenApiOperation operation, string className, CodeNamespace shortestNamespace, CodeElement parentElement) {
            var existingClass = (currentNode.DoesNodeBelongToItemSubnamespace() ? shortestNamespace.EnsureItemNamespace() : shortestNamespace)
                                        .InnerChildElements
                                        ?.OfType<CodeClass>()
                                        ?.FirstOrDefault(x => x.Name?.Equals(className, StringComparison.InvariantCultureIgnoreCase) ?? false);
            if(existingClass == null) // we can find it in the components
            {
                existingClass = new CodeClass(shortestNamespace) { Name = className, ClassKind = CodeClassKind.Model };
                CreatePropertiesForModelClass(rootNode, currentNode, schema, operation, shortestNamespace, existingClass, parentElement);
                shortestNamespace.AddClass(existingClass);
            }
            return existingClass;
        }
        private void CreatePropertiesForModelClass(OpenApiUrlSpaceNode rootNode, OpenApiUrlSpaceNode currentNode, OpenApiSchema schema, OpenApiOperation operation, CodeNamespace ns, CodeClass model, CodeElement parent) {
            if(schema?.Properties?.Any() ?? false)
                model.AddProperty(schema
                                    .Properties
                                    .Select(x => {
                                        var className = x.Value.AnyOf.Any() || x.Value.AllOf.Any() || x.Value.OneOf.Any() ? string.Empty : (x.Value.Items?.Title ?? x.Value?.Title);
                                        // only create the class if it's a non-addressable component, otherwise it'll be created by operations
                                        // TODO deupude by looking in parent namespaces???
                                        var definition = string.IsNullOrEmpty(className) ? null : AddModelClassIfDoesntExit(rootNode, currentNode, x.Value.Items ?? x.Value, operation, className, ns, parent);
                                        return CreateProperty(x.Key, className ?? x.Value.Type, model, typeSchema: x.Value.Items ?? x.Value, typeDefinition: definition);
                                    })
                                    .ToArray());
        }
        private CodeClass CreateOperationParameter(OpenApiUrlSpaceNode node, KeyValuePair<OperationType, OpenApiOperation> operation, CodeClass parentClass)
        {
            var parameters = node.PathItem.Parameters.Union(operation.Value.Parameters).Where(p => p.In == ParameterLocation.Query);
            if(parameters.Any()) {
                var parameterClass = new CodeClass(parentClass)
                {
                    Name = operation.Key.ToString() + "QueryParameters",
                    ClassKind = CodeClassKind.QueryParameters,
                };
                foreach (var parameter in parameters)
                {
                    var prop = new CodeProperty(parameterClass)
                    {
                        Name = FixQueryParameterIdentifier(parameter),
                    };
                    prop.Type = new CodeType(prop)
                    {
                        Name = parameter.Schema.Items?.Type ?? parameter.Schema.Type,
                        Schema = parameter.Schema,
                        CollectionKind = parameter.Schema.Type.Equals("array", StringComparison.InvariantCultureIgnoreCase) ? CodeType.CodeTypeCollectionKind.Array : default
                    };

                    if (!parameterClass.ContainsMember(parameter.Name))
                    {
                        parameterClass.AddProperty(prop);
                    }
                    else
                    {
                        logger.LogWarning("Ignoring duplicate parameter {name}", parameter.Name);
                    }
                }

                return parameterClass;
            } else return null;
        }

        private static string FixQueryParameterIdentifier(OpenApiParameter parameter)
        {
            // Replace with regexes pulled from settings that are API specific

            return parameter.Name.Replace("$","").ToCamelCase();
        }
        private void CreateResponseHandler(CodeClass requestBuilder)
        {
            // Default ResponseHandler Implementation
            var responseHandlerImpl = new CodeMethod(requestBuilder) { Name = "DefaultResponseHandler", IsStatic = true, MethodKind = CodeMethodKind.ResponseHandler };
            var parameter = new CodeParameter(responseHandlerImpl) { Name = "response" };
            parameter.Type = new CodeType(parameter) { Name = "object" };
            responseHandlerImpl.AddParameter(parameter);  // replace native HTTP response object type in language refiner
            responseHandlerImpl.ReturnType = new CodeType(responseHandlerImpl) { Name = "object" };
            requestBuilder.AddMethod(responseHandlerImpl);

            // Property to allow replacing Response Handler
            var responseHandlerProperty = CreateProperty("ResponseHandler", "Func<object,object>", requestBuilder, "DefaultResponseHandlerAsync");
            responseHandlerProperty.PropertyKind = CodePropertyKind.ResponseHandler;
            responseHandlerProperty.ReadOnly = false;
            requestBuilder.AddProperty(responseHandlerProperty);  
        }
    }
}
