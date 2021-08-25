using System;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.Kiota.Serialization.Json.Tests
{
    public class JsonParseNodeFactoryTests
    {
        private readonly JsonParseNodeFactory _jsonParseNodeFactory;
        private const string TestJsonString = "{\"key\":\"value\"}";

        public JsonParseNodeFactoryTests()
        {
            _jsonParseNodeFactory = new JsonParseNodeFactory();
        }

        [Fact]
        public void GetsWriterForJsonContentType()
        {
            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(TestJsonString));
            var jsonWriter = _jsonParseNodeFactory.GetRootParseNode(_jsonParseNodeFactory.ValidContentType,jsonStream);

            // Assert
            Assert.NotNull(jsonWriter);
            Assert.IsAssignableFrom<JsonParseNode>(jsonWriter);
        }

        [Fact]
        public void ThrowsArgumentOutOfRangeExceptionForInvalidContentType()
        {
            var streamContentType = "application/octet-stream";
            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(TestJsonString));
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _jsonParseNodeFactory.GetRootParseNode(streamContentType,jsonStream));

            // Assert
            Assert.NotNull(exception);
            Assert.Equal($"expected a {_jsonParseNodeFactory.ValidContentType} content type", exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ThrowsArgumentNullExceptionForNoContentType(string contentType)
        {
            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(TestJsonString));
            var exception = Assert.Throws<ArgumentNullException>(() => _jsonParseNodeFactory.GetRootParseNode(contentType,jsonStream));

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("contentType", exception.ParamName);
        }
    }
}
