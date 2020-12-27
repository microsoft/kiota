using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using kiota.core.CodeDOM;
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
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            string inputPath = config.OpenAPIFilePath;
            logger.LogInformation("{timestamp}ms: Reading OpenAPI file {file}", stopWatch.ElapsedMilliseconds, inputPath);

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


            // Parse OpenAPI Input
            logger.LogInformation("{timestamp}ms: Parsing OpenAPI file {file}", stopWatch.ElapsedMilliseconds, inputPath);
            var reader = new OpenApiStreamReader();
            var doc = reader.Read(input, out var diag);
            if (diag.Errors.Count > 0)
            {
                logger.LogError("{timestamp}ms: OpenApi Parsing errors", stopWatch.ElapsedMilliseconds, String.Join(Environment.NewLine,diag.Errors.Select(e => e.Message)));
            }
            else
            {
                logger.LogInformation("{timestamp}ms: Parsed OpenAPI successfully. {count} paths found.", stopWatch.ElapsedMilliseconds, doc.Paths.Count );
            }

            input?.Close();

            
            // Translate OpenApi PathItems into a tree structure that will define the classes
            var openApiTree = OpenApiUrlSpaceNode.Create(doc);
            var generatedCode = GenerateCode(openApiTree);
            logger.LogInformation("{timestamp}ms: Source code generated", stopWatch.ElapsedMilliseconds);

            // RefineByLanguage
            ApplyLanguageRefinement(GenerationLanguage.CSharp, generatedCode);

            // Render source output
            CodeRenderer.RenderCodeNamespaceToFilePerClass(new CSharpWriter(), generatedCode, config.OutputPath);
            logger.LogInformation("{timestamp}ms: Files written to {path}", stopWatch.ElapsedMilliseconds, config.OutputPath);
        }

        /// <summary>
        /// Manipulate CodeDOM for language specific issues
        /// </summary>
        /// <param name="language"></param>
        /// <param name="generatedCode"></param>
        private void ApplyLanguageRefinement(GenerationLanguage language, CodeNamespace generatedCode)
        {
            // TODO: Refactor into something more scaleable
            switch (language)
            {
                case GenerationLanguage.CSharp:
                    generatedCode.AddUsing(new CodeUsing() { Name = "System" });
                    generatedCode.AddUsing(new CodeUsing() { Name = "System.Threading.Tasks" });
                    break;
                default:
                    break; //Do nothing
            }
        }

        /// <summary>
        /// Iterate through Url Space and create request builder classes for each node in the tree
        /// </summary>
        /// <param name="root">Root node of URI space from the OpenAPI described API</param>
        /// <returns>A CodeNamespace object that contains request builder classes for the Uri Space</returns>
        private CodeNamespace GenerateCode(OpenApiUrlSpaceNode root)
        {
            var codeNamespace = new CodeNamespace() { Name = this.config.ClientClassName };
            
            CreateClass(codeNamespace,root);
            
            return codeNamespace;
        }

        /// <summary>
        /// Create a CodeClass instance that is a request builder class for the OpenApiUrlSpaceNode
        /// </summary>
        private void CreateClass(CodeNamespace codeNamespace, OpenApiUrlSpaceNode node)
        {
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
                var childIdentifier = FixPathIdentifier(child.Value.Identifier);
                var childType = childIdentifier + "RequestBuilder" + (this.AddSuffix ? "_" + child.Value.Hash() : "");
                if (child.Value.IsParameter())
                {
                    logger.LogDebug("Creating indexer {name}", childIdentifier);
                    var prop = new CodeIndexer()
                    {
                        Name = childIdentifier,
                        IndexType = new CodeType() { Name = "string" },
                        ReturnType = new CodeType()
                        {
                            Name = childType
                        }
                    };
                    codeClass.SetIndexer(prop);
                }
                else if (child.Value.IsFunction())
                {
                    // Don't support functions for the moment
                }
                else
                {
                    var prop = new CodeProperty()
                    {
                        Name = childIdentifier,
                        Type = new CodeType() { Name = childType }
                    };
                    logger.LogDebug("Creating property {name} of {type}", prop.Name,prop.Type.Name);
                    codeClass.AddProperty(prop);
                }
            }

            // Add methods for Operations
            if (node.HasOperations())
            {
                foreach(var operation in node.PathItem.Operations)
                {
                    var parameterClass = CreateOperationParameter(node, operation);

                    codeClass.AddInnerClass(parameterClass);

                    var method = CreateOperationMethod(operation.Key, codeClass, parameterClass);
                    logger.LogDebug("Creating method {name} of {type}", method.Name, method.ReturnType);
                    codeClass.AddMethod(method);
                }
            }

            codeNamespace.AddClass(codeClass);

            foreach (var childNode in node.Children.Values)
            {
                CreateClass(codeNamespace, childNode);
            }
        }

        private static CodeMethod CreateOperationMethod(OperationType operationType, CodeClass parentClass, CodeClass parameterClass)
        {
            var method = new CodeMethod() {
                Name = operationType.ToString() + "Async",
                ReturnType = new CodeType() { Name = "object"}
            };
            var methodParameter = new CodeParameter()
            {
                Name = "q",
                Type = new CodeType() { Name = parameterClass.Name, ActionOf = true }
            };
            method.AddParameter(methodParameter);
            return method;
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

            var identifier = parameter.Name.Replace("$","");
            return IdentifierUtils.ToCamelCase(identifier);
        }

        private static string FixPathIdentifier(string identifier)
        {
            // Replace with regexes pulled from settings that are API specific
            if(identifier.Contains("$value"))
            {
                identifier = identifier.Replace("$value", "Content");
            }
            return IdentifierUtils.ToCamelCase(identifier);
        }
    }
}
