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
        public bool AddSuffix { get; set; } = true;
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

            var codeNamespace = new CodeNamespace() { Name = this.config.ClientNamespaceName };
            CreateClass(codeNamespace, root);

            stopwatch.Stop();
            logger.LogInformation("{timestamp}ms: Created source model with {count} classes", stopwatch.ElapsedMilliseconds, codeNamespace.InnerChildElements.Count);

            return codeNamespace;
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
                    languageWriter = new CSharpWriter();
                    break;
                case GenerationLanguage.Java:
                    languageWriter = new JavaWriter();
                    break;
                case GenerationLanguage.TypeScript:
                    languageWriter = new TypeScriptWriter();
                    break;
                default:
                    throw new ArgumentException($"{language} language currently not supported.");
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await CodeRenderer.RenderCodeNamespaceToFilePerClassAsync(languageWriter, generatedCode, config.OutputPath);
            stopwatch.Stop();
            logger.LogInformation("{timestamp}ms: Files written to {path}", stopwatch.ElapsedMilliseconds, config.OutputPath);
        }


        /// <summary>
        /// Create a CodeClass instance that is a request builder class for the OpenApiUrlSpaceNode
        /// </summary>
        private void CreateClass(CodeNamespace codeNamespace, OpenApiUrlSpaceNode node)
        {
            // Determine Class Name
            string className;
            if (String.IsNullOrEmpty(node.Identifier))
            {
                className = this.config.ClientClassName;
            }
            else
            {
                className = FixPathIdentifier(node.Identifier) + "RequestBuilder" + (this.AddSuffix ? "_" + node.Hash() : "");
            }

            var codeClass = new CodeClass() { Name = className };
            logger.LogDebug("Creating class {class}", codeClass.Name);

            // Add properties for children
            foreach (var child in node.Children)
            {
                var propIdentifier = FixPathIdentifier(child.Value.Identifier);
                var propType = propIdentifier + "RequestBuilder" + (this.AddSuffix ? "_" + child.Value.Hash() : "");
                if (child.Value.IsParameter())
                {
                    var prop = CreateIndexer(propIdentifier, propType);
                    codeClass.SetIndexer(prop);
                }
                else if (child.Value.IsFunction())
                {
                    // Don't support functions for the moment
                }
                else
                {
                    var prop = CreateProperty(propIdentifier, propType);
                    codeClass.AddProperty(prop);
                }
            }

            // Add methods for Operations
            if (node.HasOperations())
            {
                foreach(var operation in node.PathItem.Operations)
                {
                    var parameterClass = CreateOperationParameter(node, operation);

                    var method = CreateOperationMethod(operation.Key, operation.Value, parameterClass);
                    logger.LogDebug("Creating method {name} of {type}", method.Name, method.ReturnType);
                    codeClass.AddMethod(method);
                }

                CreateResponseHandler(codeClass);
            }
           

            codeNamespace.AddClass(codeClass);

            foreach (var childNode in node.Children.Values)
            {
                CreateClass(codeNamespace, childNode);
            }
        }

        private CodeIndexer CreateIndexer(string childIdentifier, string childType)
        {
            var prop = new CodeIndexer()
            {
                Name = childIdentifier,
                IndexType = new CodeType() { Name = "string" },
                ReturnType = new CodeType()
                {
                    Name = childType
                }
            };
            logger.LogDebug("Creating indexer {name}", childIdentifier);
            return prop;
        }

        private CodeProperty CreateProperty(string childIdentifier, string childType, string defaultValue = null)
        {
            var prop = new CodeProperty()
            {
                Name = childIdentifier,
                Type = new CodeType() { Name = childType }, 
                DefaultValue = defaultValue
            };
            logger.LogDebug("Creating property {name} of {type}", prop.Name, prop.Type.Name);
            return prop;
        }

        private CodeMethod CreateOperationMethod(OperationType operationType, OpenApiOperation operation, CodeClass parameterClass)
        {
            var schema = GetResponseSchema(operation);
            if (schema != null)
            {
                CreateModelClasses(schema, operation);
            }

            if (operation.RequestBody != null)
            {
                CreateRequestModelClasses(operation.RequestBody, operation);
            }

            var method = new CodeMethod() {
                Name = operationType.ToString(),
                ReturnType = new CodeType() { Name = "object"}
            };
            var methodParameter = new CodeParameter
            {
                Name = "q",
                Type = new CodeType() { Name = parameterClass.Name, ActionOf = true, TypeDefinition = parameterClass },
                Optional = true,
                IsQueryParameter = true,
            };
            method.AddParameter(methodParameter);
            return method;
        }

        private void CreateRequestModelClasses(OpenApiRequestBody requestBody, OpenApiOperation operation)
        {
            //TODO:
            //if has reference, go find the type with the same id
            //else insert inner declaration

            // use the type declaration /reference for the operation parameter declaration
        }

        private void CreateModelClasses(OpenApiSchema schema, OpenApiOperation operation)
        {
            // object type
            // array of object
            // all of object
            // one of object
            // any of object
            CodeClass codeClass;

            if (schema.Reference == null)  // Inline schema, i.e. specific to the Operation
            {
                codeClass = new CodeClass() { Name = operation.OperationId + "Response" };
            } else  // Reused schema from components
            {
                if(false) // we can find it in the components
                {
                    
                }
                else // we can't find it so new it 
                codeClass = new CodeClass() { Name = schema.Reference.Id };   // Id will contain "microsoft.Graph.foo"
            }
            // Add codeClass to model namespace in workspace
        }

        private OpenApiSchema GetResponseSchema(OpenApiOperation operation)
        {
            // Return Schema that represents all the possible success responses!
            // For the moment assume 200s and application/json
            // TODO: figure out how to create types that accurately correspond to HTTP responses!
            var schemas = operation.Responses.Where(r => r.Key == "200" || r.Key == "201")
                                .SelectMany(re => re.Value.Content)
                                .Where(c => c.Key == "application/json")
                                .Select(co => co.Value.Schema)
                                .Where(s => s is not null);

            return schemas.FirstOrDefault();
        }

        private CodeClass CreateOperationParameter(OpenApiUrlSpaceNode node, KeyValuePair<OperationType, OpenApiOperation> operation)
        {
            var parameterClass = new CodeClass()
            {
                Name = operation.Key.ToString() + "QueryParameters"
            };
            var parameters = node.PathItem.Parameters.Union(operation.Value.Parameters).Where(p => p.In == ParameterLocation.Query);
            foreach (var parameter in parameters)
            {
                var prop = new CodeProperty()
                {
                    Name = FixQueryParameterIdentifier(parameter),
                    Type = new CodeType()
                    {
                        Name = parameter.Schema.Type,
                        Schema = parameter.Schema
                    }
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
        }

        private static string FixQueryParameterIdentifier(OpenApiParameter parameter)
        {
            // Replace with regexes pulled from settings that are API specific

            return parameter.Name.Replace("$","").ToCamelCase();
        }

        private static string FixPathIdentifier(string identifier)
        {
            // Replace with regexes pulled from settings that are API specific
            if(identifier.Contains("$value"))
            {
                identifier = identifier.Replace("$value", "Content");
            }
            return identifier.ToCamelCase();
        }

        private void CreateResponseHandler(CodeClass requestBuilder)
        {
            // Default ResponseHandler Implementation
            var responseHandlerImpl = new CodeMethod { Name = "DefaultResponseHandler", IsStatic = true };
            responseHandlerImpl.AddParameter(new CodeParameter { Name = "response", Type = new CodeType { Name = "object" } });  // replace native HTTP response object type in language refiner
            responseHandlerImpl.ReturnType = new CodeType { Name = "object" };
            requestBuilder.AddMethod(responseHandlerImpl);

            // Property to allow replacing Response Handler
            var responseHandlerProperty = CreateProperty("ResponseHandler", "Func<object,object>", "DefaultResponseHandler"); // HttpResponseMessage, model
            responseHandlerProperty.ReadOnly = false;
            requestBuilder.AddProperty(responseHandlerProperty);  
        }
    }
}
