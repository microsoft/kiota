using System;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests
{
    public class RequestInformationTests
    {
        [Fact]
        public void SetUriExtractsQueryParameters()
        {
            // Arrange
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                UrlTemplate = "http://localhost/me?foo={foo}"
            };
            // Act
            testRequest.QueryParameters.Add("foo", "bar");
            // Assert
            Assert.Equal("http://localhost/me?me?foo=bar", testRequest.URI.ToString());
            Assert.NotEmpty(testRequest.QueryParameters);
            Assert.Equal("foo",testRequest.QueryParameters.First().Key);
            Assert.Equal("bar", testRequest.QueryParameters.First().Value.ToString());
        }


        [Fact]
        public void AddsAndRemovesRequestOptions()
        {
            // Arrange
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            var testRequestOption = new Mock<IRequestOption>().Object;
            Assert.Empty(testRequest.RequestOptions);
            // Act
            testRequest.AddRequestOptions(testRequestOption);
            // Assert
            Assert.NotEmpty(testRequest.RequestOptions);
            Assert.Equal(testRequestOption, testRequest.RequestOptions.First());

            // Act by removing the option
            testRequest.RemoveRequestOptions(testRequestOption);
            Assert.Empty(testRequest.RequestOptions);
        }
    }
}
