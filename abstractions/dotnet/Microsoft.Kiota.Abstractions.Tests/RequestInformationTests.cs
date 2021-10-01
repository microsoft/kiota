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
        public void AddsAndRemovesRequestOptions()
        {
            // Arrange
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            var testMiddleWareOption = new Mock<IRequestOption>().Object;
            Assert.Empty(testRequest.RequestOptions);
            // Act
            testRequest.AddRequestOptions(testMiddleWareOption);
            // Assert
            Assert.NotEmpty(testRequest.RequestOptions);
            Assert.Equal(testMiddleWareOption, testRequest.RequestOptions.First());

            // Act by removing the option
            testRequest.RemoveRequestOptions(testMiddleWareOption);
            Assert.Empty(testRequest.RequestOptions);
        }
    }
}
