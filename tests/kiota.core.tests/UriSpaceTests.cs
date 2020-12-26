using System;
using Microsoft.OpenApi.Models;
using Xunit;

namespace kiota.core.tests
{
    public class UriSpaceTests
    {
        [Fact]
        public void CreateEmptyUrlSpace()
        {
            var doc = new OpenApiDocument() { };

            var rootNode = OpenApiUrlSpaceNode.Create(doc);

            Assert.Null(rootNode);
        }

        [Fact]
        public void Create_Single_Root()
        {
            var doc = new OpenApiDocument()
            {
                Paths = new OpenApiPaths()
                {
                    ["/"] = new OpenApiPathItem()
                }
            };

            var rootNode = OpenApiUrlSpaceNode.Create(doc);

            Assert.NotNull(rootNode);
            Assert.NotNull(rootNode.PathItem);
            Assert.Equal(0, rootNode.Children.Count);
        }

        [Fact]
        public void Create_a_path_without_a_root()
        {
            var doc = new OpenApiDocument()
            {
                Paths = new OpenApiPaths()
                {
                    ["/home"] = new OpenApiPathItem()
                }
            };

            var rootNode = OpenApiUrlSpaceNode.Create(doc);

            Assert.NotNull(rootNode);
            Assert.Null(rootNode.PathItem);
            Assert.Equal(1, rootNode.Children.Count);
            Assert.Equal("home", rootNode.Children["home"].Segment);
            Assert.NotNull(rootNode.Children["home"].PathItem);
        }

        [Fact]
        public void Create_multiple_paths()
        {
            var doc = new OpenApiDocument()
            {
                Paths = new OpenApiPaths()
                {
                    ["/"] = new OpenApiPathItem(),
                    ["/home"] = new OpenApiPathItem(),
                    ["/start"] = new OpenApiPathItem()
                }
            };

            var rootNode = OpenApiUrlSpaceNode.Create(doc);

            Assert.NotNull(rootNode);
            Assert.Equal(2, rootNode.Children.Count);
            Assert.Equal("home", rootNode.Children["home"].Segment);
            Assert.Equal("start", rootNode.Children["start"].Segment);
        }

        [Fact]
        public void Create_paths_with_many_levels()
        {
            var doc = new OpenApiDocument()
            {
                Paths = new OpenApiPaths()
                {
                    ["/"] = new OpenApiPathItem(),
                    ["/home/sweet/home"] = new OpenApiPathItem(),
                    ["/start/end"] = new OpenApiPathItem()
                }
            };

            var rootNode = OpenApiUrlSpaceNode.Create(doc);

            Assert.NotNull(rootNode);
            Assert.Equal(2, rootNode.Children.Count);
            Assert.NotNull(rootNode.Children["home"].Children["sweet"].Children["home"].PathItem);
            Assert.Equal("end", rootNode.Children["start"].Children["end"].Segment);
        }
    }
}
