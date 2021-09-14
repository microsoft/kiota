using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClient.Extensions;
using Xunit;
using HttpMethod = Microsoft.Kiota.Abstractions.HttpMethod;

namespace Microsoft.Kiota.Http.HttpClient.Tests.Extensions
{
    public class RequestInformationExtensionsTests
    {
        [Fact]
        public async Task SetsContentFromStringHttpContent()
        {
            // Arrange
            var testRequest = new RequestInformation
            {
                HttpMethod = HttpMethod.POST,
                URI = new Uri("http://localhost")
            };
            // Create an instance of MultipartContent
            var content = new StringContent("test input");

            // Act
            await testRequest.SetContentFromHttpContentAsync(content);

            // Assert
            Assert.NotNull(testRequest.Content); // ensure the stream is set
            Assert.NotEmpty(testRequest.Headers); // ensure a header is set
            Assert.Equal("Content-Type", testRequest.Headers.First().Key); // ensure that the content type header is copied across
            Assert.Equal("text/plain; charset=utf-8", testRequest.Headers.First().Value); // ensure that the content type header is copied across

            // Act again to ensure the stream is as expected
            using var streamReader = new StreamReader(testRequest.Content);
            var stringContent = await streamReader.ReadToEndAsync();

            // Assert again
            Assert.Equal("test input", stringContent); // Boundary exists
        }
        [Fact]
        public async Task SetsMultipartContentFromMultipartHttpContent()
        {
            // Arrange
            var testRequest = new RequestInformation
            {
                HttpMethod = HttpMethod.POST,
                URI = new Uri("http://localhost")
            };
            // Create an instance of MultipartContent
            var content = new MultipartContent("mixed", "customBoundary")
            {
                new StringContent("value1"), // string content
                JsonContent.Create(new { name = "Peter Pan"} ) // instance of JsonContent
            };

            // Act
            await testRequest.SetContentFromHttpContentAsync(content);

            // Assert
            Assert.NotNull(testRequest.Content); // ensure the stream is set
            Assert.NotEmpty(testRequest.Headers); // ensure a header is set
            Assert.Equal("Content-Type", testRequest.Headers.First().Key); // ensure that the content type header is copied across

            // Act again to ensure the stream is as expected
            using var streamReader = new StreamReader(testRequest.Content);
            var stringContent = await streamReader.ReadToEndAsync();

            // Assert again
            Assert.Contains("--customBoundary", stringContent); // Boundary exists
            Assert.Contains("text/plain;", stringContent); // Value1 content type exists
            Assert.Contains("value1", stringContent); // Value1 exists
            Assert.Contains("application/json;", stringContent); // JsonContent content type exists
            Assert.Contains("{\"name\":\"Peter Pan\"}", stringContent); // Value1 exists
        }
    }
}
