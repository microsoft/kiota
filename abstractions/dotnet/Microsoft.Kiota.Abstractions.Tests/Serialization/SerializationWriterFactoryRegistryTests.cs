using System;
using System.IO;
using System.Text;
using Microsoft.Kiota.Abstractions.Serialization;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests.Serialization
{
    public class SerializationWriterFactoryRegistryTests
    {
        private readonly SerializationWriterFactoryRegistry _serializationWriterFactoryRegistry;
        public SerializationWriterFactoryRegistryTests()
        {
            _serializationWriterFactoryRegistry = new SerializationWriterFactoryRegistry();
        }

        [Fact]
        public void ParseNodeFactoryRegistryDoesNotStickToOneContentType()
        {
            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => _serializationWriterFactoryRegistry.ValidContentType);
        }

        [Fact]
        public void ReturnsExpectedRootNodeForRegisteredContentType()
        {
            // Arrange
            var streamContentType = "application/octet-stream";
            using var testStream = new MemoryStream(Encoding.UTF8.GetBytes("test input"));
            var mockSerializationWriterFactory = new Mock<ISerializationWriterFactory>();
            var mockSerializationWriter = new Mock<ISerializationWriter>();
            mockSerializationWriterFactory.Setup(serializationWriterFactory => serializationWriterFactory.GetSerializationWriter(streamContentType)).Returns(mockSerializationWriter.Object);
            _serializationWriterFactoryRegistry.ContentTypeAssociatedFactories.Add(streamContentType, mockSerializationWriterFactory.Object);
            // Act
            var serializationWriter = _serializationWriterFactoryRegistry.GetSerializationWriter(streamContentType);
            // Assert
            Assert.NotNull(serializationWriter);
            Assert.Equal(mockSerializationWriter.Object, serializationWriter);
        }

        [Fact]
        public void ThrowsInvalidOperationExceptionForUnregisteredContentType()
        {
            // Arrange
            var streamContentType = "application/octet-stream";
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => _serializationWriterFactoryRegistry.GetSerializationWriter(streamContentType));
            // Assert
            Assert.NotNull(exception);
            Assert.Equal($"Content type {streamContentType} does not have a factory registered to be parsed", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ThrowsArgumentNullExceptionForNoContentType(string contentType)
        {
            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => _serializationWriterFactoryRegistry.GetSerializationWriter(contentType));
            // Assert
            Assert.NotNull(exception);
            Assert.Equal("contentType", exception.ParamName);
        }
    }
}
