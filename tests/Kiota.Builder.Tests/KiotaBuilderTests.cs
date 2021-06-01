using Microsoft.OpenApi.Services;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests
{
    public class KiotaBuilderTests
    {
        [Fact]
        public void Single_root_node_creates_single_request_builder_class()
        {
            var node = OpenApiUrlTreeNode.Create();
            var mockLogger = new Mock<ILogger<KiotaBuilder>>();
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
            var codeModel = builder.CreateSourceModel(node);

            Assert.Single(codeModel.GetChildElements(true));
        }
        [Fact]
        public void Single_path_with_get_collection()
        {
            var node = OpenApiUrlTreeNode.Create();
            node.Attach("tasks", new OpenApiPathItem() {
                Operations = {
                    [OperationType.Get] = new OpenApiOperation() { 
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Content =
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = "array",
                                            Items = new OpenApiSchema
                                            {
                                                Type = "int"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                } 
            }, "default");
            var mockLogger = new Mock<ILogger<KiotaBuilder>>();
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
            var codeModel = builder.CreateSourceModel(node);

            var rootNamespace = codeModel.GetChildElements(true).Single();
            var rootBuilder = rootNamespace.GetChildElements(true).Single(e => e.Name == "Graph");
            var tasksProperty = rootBuilder.GetChildElements(true).OfType<CodeProperty>().Single(e => e.Name == "Tasks");
            var tasksRequestBuilder = tasksProperty.Type as CodeType;
            Assert.NotNull(tasksRequestBuilder);
            var getMethod = tasksRequestBuilder.TypeDefinition.GetChildElements(true).OfType<CodeMethod>().Single(e => e.Name == "Get");
            var returnType = getMethod.ReturnType;
            Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Array, returnType.CollectionKind);
        }
    }
}
