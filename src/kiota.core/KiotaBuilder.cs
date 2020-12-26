using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
            var generatedNamespace = GenerateCode(openApiTree);
            logger.LogInformation("{timestamp}ms: Source code generated", stopWatch.ElapsedMilliseconds);

            // Render source output
            CodeRenderer.RenderCodeNamespaceToFilePerClass(new CSharpWriter(), generatedNamespace, config.OutputPath);
            logger.LogInformation("{timestamp}ms: Files written to {path}", stopWatch.ElapsedMilliseconds, config.OutputPath);
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
                className = node.Identifier + "RequestBuilder" + (this.AddSuffix ? "_" + node.Hash() : "");
            }
            var codeClass = new CodeClass() { Name = className };

            logger.LogDebug("Creating class {class}", codeClass.Name);

            // Add properties for children

            foreach (var child in node.Children)
            {
                var childType = child.Value.Identifier + "RequestBuilder" + (this.AddSuffix ? "_" + child.Value.Hash() : "");
                if (child.Value.IsParameter())
                {
                    logger.LogDebug("Creating indexer {name}", child.Value.Identifier);
                    var prop = new CodeIndexer() {
                        IndexType = "string",
                        ReturnType = childType
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
                        Name = child.Value.Identifier,
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
                ReturnType = parameterClass.Name
            };
            var methodParameter = new CodeParameter()
            {
                Name = "q",
                Type = new CodeType() { Name = parameterClass.Name }
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
            var parameters = node.PathItem.Parameters.Union(operation.Value.Parameters);
            foreach (var parameter in parameters)
            {
                var prop = new CodeProperty()
                {
                    Name = parameter.Name,
                    Type = new CodeType()
                    {
                        Name = parameter.Schema.Type,
                        Schema = parameter.Schema
                    }
                };

                if (!parameterClass.ContainsMember(parameter.Name))
                {
                    parameterClass.AddProperty(prop);
                } else
                {
                    logger.LogWarning("Ignoring duplicate parameter {name}", parameter.Name);
                }
            }

            return parameterClass;
        }
    }
}
