using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Azure.Core;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Authentication.Azure.Tests
{
    public class AzureIdentityAuthenticationProviderTests
    {
        [Fact]
        public void ConstructorThrowsArgumentNullExceptionOnNullTokenCredential()
        {
            // Arrange
            var exception = Assert.Throws<ArgumentNullException>(() => new AzureIdentityAuthenticationProvider(null, null));

            // Assert
            Assert.Equal("credentials", exception.ParamName);
        }

        [Fact]
        public async Task GetAuthorizationTokenAsyncGetsToken()
        {
            // Arrange
            var expectedToken = "token";
            var mockTokenCredential = new Mock<TokenCredential>();
            mockTokenCredential.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(new AccessToken(expectedToken, DateTimeOffset.Now)));
            var azureIdentityAuthenticationProvider = new AzureIdentityAuthenticationProvider(mockTokenCredential.Object, null);
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };

            // Act
            var token = await azureIdentityAuthenticationProvider.GetAuthorizationTokenAsync(testRequest);

            // Assert
            Assert.Equal(expectedToken, token);
        }

        [Fact]
        public async Task AuthenticateRequestAsyncSetsBearerHeader()
        {
            // Arrange
            var expectedToken = "token";
            var mockTokenCredential = new Mock<TokenCredential>();
            mockTokenCredential.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(new AccessToken(expectedToken, DateTimeOffset.Now)));
            var azureIdentityAuthenticationProvider = new AzureIdentityAuthenticationProvider(mockTokenCredential.Object,"User.Read");
            var testRequest = new RequestInformation()
            {
                HttpMethod = HttpMethod.GET,
                URI = new Uri("http://localhost")
            };
            Assert.Empty(testRequest.Headers); // header collection is empty

            // Act
            await azureIdentityAuthenticationProvider.AuthenticateRequestAsync(testRequest);

            // Assert
            Assert.NotEmpty(testRequest.Headers); // header collection is longer empty
            Assert.Equal("Authorization", testRequest.Headers.First().Key); // First element is Auth header
            Assert.Equal($"Bearer {expectedToken}", testRequest.Headers.First().Value); // First element is Auth header
        }
    }
}
