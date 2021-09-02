using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Xunit;

namespace Kiota.Builder.Extensions.Tests {
    public class OpenApiUrlTreeNodeExtensionsTests {
        [Fact]
        public void Defensive() {
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetIdentifier(null));
            Assert.False(OpenApiUrlTreeNodeExtensions.IsComplexPathWithAnyNumberOfParameters(null));
            Assert.False(OpenApiUrlTreeNodeExtensions.IsPathWithSingleSimpleParamter(null));
            Assert.False(OpenApiUrlTreeNodeExtensions.DoesNodeBelongToItemSubnamespace(null));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(null, null));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(null, Label));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(OpenApiUrlTreeNode.Create(), null));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(OpenApiUrlTreeNode.Create(), Label));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, null));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, Label));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(OpenApiUrlTreeNode.Create(), null));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(OpenApiUrlTreeNode.Create(), Label));
        }
        private const string Label = "default";
        [Fact]
        public void GetsDescription() {
            var node = OpenApiUrlTreeNode.Create();
            node.PathItems.Add(Label, new() {
                Description = "description",
                Summary = "summary"
            });
            Assert.Equal(Label, OpenApiUrlTreeNode.Create().GetPathItemDescription(Label, Label));
            Assert.Equal("description", node.GetPathItemDescription(Label, Label));
            node.PathItems[Label].Description = null;
            Assert.Equal("summary", node.GetPathItemDescription(Label, Label));
        }
        [Fact]
        public void IsComplexPathWithAnyNumberOfParameters() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("function()", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, Label);
            Assert.False(node.IsComplexPathWithAnyNumberOfParameters());
            Assert.True(node.Children.First().Value.IsComplexPathWithAnyNumberOfParameters());
        }
        [Fact]
        public void IsPathWithSingleSimpleParamter() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("{param}", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, Label);
            Assert.False(node.IsPathWithSingleSimpleParamter());
            Assert.True(node.Children.First().Value.IsPathWithSingleSimpleParamter());
        }
        [Fact]
        public void DoesNodeBelongToItemSubnamespace() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("{param}", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, Label);
            Assert.False(node.DoesNodeBelongToItemSubnamespace());
            Assert.True(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

            doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("param}", new() {});
            node = OpenApiUrlTreeNode.Create(doc, Label);
            Assert.False(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

            doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("{param", new() {});
            node = OpenApiUrlTreeNode.Create(doc, Label);
            Assert.False(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());
        }
        [Fact]
        public void GetIdentifier() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("function(parm1)", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, Label);
            Assert.DoesNotContain("(", node.Children.First().Value.GetIdentifier());
        }
        [Fact]
        public void GetNodeNamespaceFromPath() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("\\users\\messages", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, Label);
            Assert.Equal("graph.users.messages", node.Children.First().Value.GetNodeNamespaceFromPath("graph"));
            Assert.Equal("users.messages", node.Children.First().Value.GetNodeNamespaceFromPath(null));
        }
    }
}
