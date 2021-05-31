using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Xunit;

namespace Kiota.Builder.Extensions.Tests {
    public class OpenApiUrlTreeNodeExtensionsTests {
        [Fact]
        public void Defensive() {
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetIdentifier(null));
            Assert.False(OpenApiUrlTreeNodeExtensions.IsFunction(null));
            Assert.False(OpenApiUrlTreeNodeExtensions.IsParameter(null));
            Assert.False(OpenApiUrlTreeNodeExtensions.DoesNodeBelongToItemSubnamespace(null));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(null, null));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(null, label));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(OpenApiUrlTreeNode.Create(), null));
            Assert.Empty(OpenApiUrlTreeNodeExtensions.GetComponentsReferenceIndex(OpenApiUrlTreeNode.Create(), label));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, null));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(null, label));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(OpenApiUrlTreeNode.Create(), null));
            Assert.Null(OpenApiUrlTreeNodeExtensions.GetPathItemDescription(OpenApiUrlTreeNode.Create(), label));
        }
        private const string label = "default";
        [Fact]
        public void GetsDescription() {
            var node = OpenApiUrlTreeNode.Create();
            node.PathItems.Add(label, new() {
                Description = "description",
                Summary = "summary"
            });
            Assert.Equal(label, OpenApiUrlTreeNode.Create().GetPathItemDescription(label, label));
            Assert.Equal("description", node.GetPathItemDescription(label, label));
            node.PathItems[label].Description = null;
            Assert.Equal("summary", node.GetPathItemDescription(label, label));
        }
        [Fact]
        public void IsFunction() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("function()", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, label);
            Assert.False(node.IsFunction());
            Assert.True(node.Children.First().Value.IsFunction());
        }
        [Fact]
        public void IsParameter() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("{param}", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, label);
            Assert.False(node.IsParameter());
            Assert.True(node.Children.First().Value.IsParameter());
        }
        [Fact]
        public void DoesNodeBelongToItemSubnamespace() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("{param}", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, label);
            Assert.False(node.DoesNodeBelongToItemSubnamespace());
            Assert.True(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

            doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("param}", new() {});
            node = OpenApiUrlTreeNode.Create(doc, label);
            Assert.False(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());

            doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("{param", new() {});
            node = OpenApiUrlTreeNode.Create(doc, label);
            Assert.False(node.Children.First().Value.DoesNodeBelongToItemSubnamespace());
        }
        [Fact]
        public void GetIdentifier() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("function(parm1)", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, label);
            Assert.DoesNotContain("(", node.Children.First().Value.GetIdentifier());
        }
        [Fact]
        public void GetNodeNamespaceFromPath() {
            var doc = new OpenApiDocument {
                Paths = new(),
            };
            doc.Paths.Add("\\users\\messages", new() {});
            var node = OpenApiUrlTreeNode.Create(doc, label);
            Assert.Equal("graph.users.messages", node.Children.First().Value.GetNodeNamespaceFromPath("graph"));
            Assert.Equal("users.messages", node.Children.First().Value.GetNodeNamespaceFromPath(null));
        }
    }
}
