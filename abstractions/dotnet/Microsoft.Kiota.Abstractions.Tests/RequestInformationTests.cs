using System;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests
{
    public class RequestInformationTests
    {
        [Fact]
        public void SetUriAppendsUrlSegments()
        {
            // Arrange
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            // Act
            testRequest.SetURI(testRequest.URI.OriginalString,"/me",false);
            // Assert
            Assert.Equal("http://localhost/me", testRequest.URI.OriginalString);
        }

        [Fact]
        public void SetUriExtractsQueryParameters()
        {
            // Arrange
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            // Act
            testRequest.SetURI("http://localhost/me?foo=bar", "", true);
            // Assert
            Assert.Equal("http://localhost/me", testRequest.URI.OriginalString);
            Assert.NotEmpty(testRequest.QueryParameters);
            Assert.Equal("foo",testRequest.QueryParameters.First().Key);
            Assert.Equal("bar", testRequest.QueryParameters.First().Value.ToString());
        }


        [Fact]
        public void AddsAndRemovesMiddlewareOptions()
        {
            // Arrange
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            var testMiddleWareOption = new Mock<IMiddlewareOption>().Object;
            Assert.Empty(testRequest.MiddlewareOptions);
            // Act
            testRequest.AddMiddlewareOptions(testMiddleWareOption);
            // Assert
            Assert.NotEmpty(testRequest.MiddlewareOptions);
            Assert.Equal(testMiddleWareOption, testRequest.MiddlewareOptions.First());

            // Act by removing the option
            testRequest.RemoveMiddlewareOptions(testMiddleWareOption);
            Assert.Empty(testRequest.MiddlewareOptions);
        }
    }
}
