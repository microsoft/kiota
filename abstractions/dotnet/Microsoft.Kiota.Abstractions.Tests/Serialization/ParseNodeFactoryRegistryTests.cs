using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Serialization;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests.serialization
{
    public class ParseNodeFactoryRegistryTests
    {
        private readonly ParseNodeFactoryRegistry _parseNodeFactoryRegistry;
        public ParseNodeFactoryRegistryTests()
        {
            _parseNodeFactoryRegistry = new ParseNodeFactoryRegistry();
        }

        [Fact]
        public void ParseNodeFactoryRegistryDoesNotStickToOneContentType()
        {
            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => _parseNodeFactoryRegistry.ValidContentType);
        }

        [Fact]
        public void ReturnsExpectedRootNodeForRegisteredContentType()
        {
            // Arrange
            var streamContentType = "application/octet-stream";
            using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("test input"));
            var mockParseNodeFactory = new Mock<IParseNodeFactory>();
            var mockParseNode = new Mock<IParseNode>();
            mockParseNodeFactory.Setup(parseNodeFactory => parseNodeFactory.GetRootParseNode(streamContentType, It.IsAny<Stream>())).Returns(mockParseNode.Object);
            _parseNodeFactoryRegistry.ContentTypeAssociatedFactories.Add(streamContentType, mockParseNodeFactory.Object);
            // Act
            var rootParseNode = _parseNodeFactoryRegistry.GetRootParseNode(streamContentType, testStream);
            // Assert
            Assert.NotNull(rootParseNode);
            Assert.Equal(mockParseNode.Object, rootParseNode);
        }

        [Fact]
        public void ThrowsInvalidOperationExceptionForUnregisteredContentType()
        {
            // Arrange
            var streamContentType = "application/octet-stream";
            using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("test input"));
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => _parseNodeFactoryRegistry.GetRootParseNode(streamContentType, testStream));
            // Assert
            Assert.NotNull(exception);
            Assert.Equal($"Content type {streamContentType} does not have a factory registered to be parsed", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ThrowsArgumentNullExceptionForNoContentType(string contentType)
        {
            // Arrange
            using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("test input"));
            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => _parseNodeFactoryRegistry.GetRootParseNode(contentType, testStream));
            // Assert
            Assert.NotNull(exception);
            Assert.Equal("contentType", exception.ParamName);
        }
    }
}
