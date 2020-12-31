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

            var codeNamespace = new CodeNamespace(null) { Name = this.config.ClientNamespaceName };
            codeNamespace.AddNamespace(new CodeNamespace(codeNamespace){
                Name = $"{this.config.ClientNamespaceName}.requests",
                IsRequestsNamespace = true
            });
            CreateRequestBuilderClass(codeNamespace, root);

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


        /// <summary>
        /// Create a CodeClass instance that is a request builder class for the OpenApiUrlSpaceNode
        /// </summary>
        private void CreateRequestBuilderClass(CodeNamespace codeNamespace, OpenApiUrlSpaceNode node)
        {
            // Determine Class Name
            CodeClass codeClass;
            if (String.IsNullOrEmpty(node.Identifier))
            {
                codeClass = new CodeClass(codeNamespace) { Name = this.config.ClientClassName };
            }
            else
            {
                var className = FixPathIdentifier(node.Identifier) + "RequestBuilder" + (this.AddSuffix ? "_" + node.Hash() : "");
                codeClass = new CodeClass(codeNamespace.RequestsNamespace) { Name = className };
            }

            logger.LogDebug("Creating class {class}", codeClass.Name);

            // Add properties for children
            foreach (var child in node.Children)
            {
                var propIdentifier = FixPathIdentifier(child.Value.Identifier);
                var propType = propIdentifier + "RequestBuilder" + (this.AddSuffix ? "_" + child.Value.Hash() : "");
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
                    var prop = CreateProperty(propIdentifier, propType, codeClass);
                    codeClass.AddProperty(prop);
                }
            }

            // Add methods for Operations
            if (node.HasOperations())
            {
                foreach(var operation in node.PathItem.Operations)
                {
                    var parameterClass = CreateOperationParameter(node, operation, codeClass);

                    var method = CreateOperationMethod(operation.Key, operation.Value, parameterClass, codeClass);
                    logger.LogDebug("Creating method {name} of {type}", method.Name, method.ReturnType);
                    codeClass.AddMethod(method);
                }
            }

            (string.IsNullOrEmpty(node.Identifier) ? codeNamespace : codeNamespace.RequestsNamespace).AddClass(codeClass);

            foreach (var childNode in node.Children.Values)
            {
                CreateRequestBuilderClass(codeNamespace, childNode);
            }
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

        private CodeProperty CreateProperty(string childIdentifier, string childType, CodeClass codeClass, OpenApiSchema typeSchema = null)
        {
            var prop = new CodeProperty(codeClass)
            {
                Name = childIdentifier,
            };
            prop.Type = new CodeType(prop) { Name = childType, Schema = typeSchema };
            logger.LogDebug("Creating property {name} of {type}", prop.Name, prop.Type.Name);
            return prop;
        }

        private CodeMethod CreateOperationMethod(OperationType operationType, OpenApiOperation operation, CodeClass parameterClass, CodeClass parentClass)
        {
            var schema = GetResponseSchema(operation);
            var method = new CodeMethod(parentClass) {
                Name = operationType.ToString(),
            };
            if (schema != null)
            {
                var returnType = CreateModelClasses(schema, operation, method);
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
            var qsParam = new CodeParameter(method)
            {
                Name = "q",
                Optional = true,
                IsQueryParameter = true,
            };
            qsParam.Type = new CodeType(qsParam) { Name = parameterClass.Name, ActionOf = true, TypeDefinition = parameterClass };
            method.AddParameter(qsParam);
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
        private CodeType CreateModelClasses(OpenApiSchema schema, OpenApiOperation operation, CodeMethod parentMethod)
        {
            var originalReferenceId = schema?.Reference?.Id;
            var originalReference = schema?.Reference;
            var codeNamespace = parentMethod.GetImmediateParentOfType<CodeNamespace>();
            if(schema?.AllOf?.Any() ?? false)
                schema = schema.AllOf.Last();
            // object type
            // array of object
            // all of object
            // one of object
            // any of object

            if (originalReference == null)  // Inline schema, i.e. specific to the Operation
            {//TODO
                var codeClass = new CodeClass(codeNamespace) { Name = operation.OperationId + "Response" };
            } else  // Reused schema from components
            {
                var targetNamespace = codeNamespace.GetRootNamespace().GetNamespace(GetNamespaceNameFromReferenceId(originalReferenceId));
                //TODO create namespace if we can't find it, and create the sub requests namespace
                var className = GetClassNameFromReferenceId(originalReferenceId);
                var existingClass = targetNamespace.InnerChildElements.OfType<CodeClass>().FirstOrDefault(x => x.Name?.Equals(className) ?? false);
                if(existingClass == null) // we can find it in the components
                {
                    existingClass = new CodeClass(targetNamespace) { Name = className };
                    if(schema.Properties.Any())//TODO handle collections
                        existingClass.AddProperty(schema
                                                    .Properties
                                                    .Select(x => CreateProperty(x.Key, x.Value.Type, existingClass, x.Value))
                                                    .ToArray());
                    targetNamespace.AddClass(existingClass);
                }
                return new CodeType(parentMethod) {
                    TypeDefinition = existingClass,
                    Name = className,
                    Schema = schema
                };
            }
            return null;
            // Add codeClass to model namespace in workspace
        }
        private string GetClassNameFromReferenceId(string referenceId) {
            if(string.IsNullOrEmpty(referenceId)) 
                return referenceId;
            
            var truncatedNamespaceName = referenceId.Replace(this.config.SchemaRootNamespaceName, string.Empty);
            if(truncatedNamespaceName.Contains('.')) 
                return truncatedNamespaceName.Substring(truncatedNamespaceName.LastIndexOf('.') + 1);
            else 
                return truncatedNamespaceName;
        }
        private string GetNamespaceNameFromReferenceId(string referenceId) {
            if(string.IsNullOrEmpty(referenceId)) 
                return referenceId;
            
            var truncatedNamespaceName = referenceId.Replace(this.config.SchemaRootNamespaceName, string.Empty);
            if(truncatedNamespaceName.Contains('.')) 
                return this.config.ClientNamespaceName + truncatedNamespaceName.Substring(0, truncatedNamespaceName.LastIndexOf('.'));
            else 
                return truncatedNamespaceName;
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

        private CodeClass CreateOperationParameter(OpenApiUrlSpaceNode node, KeyValuePair<OperationType, OpenApiOperation> operation, CodeClass parentClass)
        {
            var parameterClass = new CodeClass(parentClass)
            {
                Name = operation.Key.ToString() + "QueryParameters"
            };
            var parameters = node.PathItem.Parameters.Union(operation.Value.Parameters).Where(p => p.In == ParameterLocation.Query);
            foreach (var parameter in parameters)
            {
                var prop = new CodeProperty(parameterClass)
                {
                    Name = FixQueryParameterIdentifier(parameter),
                };
                prop.Type = new CodeType(prop)
                    {
                        Name = parameter.Schema.Type,
                        Schema = parameter.Schema
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
    }
}
