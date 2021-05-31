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
            var node = new OpenApiUrlSpaceNode("");
            var mockLogger = new Mock<ILogger<KiotaBuilder>>();
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
            var codeModel = builder.CreateSourceModel(node);

            Assert.Single(codeModel.GetChildElements(true));
        }

        [Fact]
        public void Single_path_with_get_collection()
        {
            var node = new OpenApiUrlSpaceNode("");
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
            }, "");
            var mockLogger = new Mock<ILogger<KiotaBuilder>>();
            var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph" });
            var codeModel = builder.CreateSourceModel(node);

            var rootNamespace = codeModel.GetChildElements(true).SingleOrDefault();
            Assert.NotNull(rootNamespace);
            var rootBuilder = rootNamespace.GetChildElements(true).Where(e => e.Name == "Graph").SingleOrDefault();
            Assert.NotNull(rootBuilder);
            var tasksProperty = (CodeProperty)rootBuilder.GetChildElements(true).Where(e => e.Name == "Tasks").SingleOrDefault();
            Assert.NotNull(tasksProperty);
            var tasksRequestBuilder = (CodeType)tasksProperty.Type;
            Assert.NotNull(tasksRequestBuilder);
            var getMethod = (CodeMethod)tasksRequestBuilder.TypeDefinition.GetChildElements(true).Where(e => e.Name == "Get").SingleOrDefault();
            Assert.NotNull(getMethod);
            var returnType = getMethod.ReturnType;
            Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Array, returnType.CollectionKind);
        }
    }
}
