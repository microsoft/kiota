using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Azure.Core;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Authentication.Azure.Tests;
public class AzureIdentityAuthenticationProviderTests
{
    [Fact]
    public void ConstructorThrowsArgumentNullExceptionOnNullTokenCredential()
    {
        // Arrange
        var exception = Assert.Throws<ArgumentNullException>(() => new AzureIdentityAccessTokenProvider(null, null));

        // Assert
        Assert.Equal("credential", exception.ParamName);
    }

    [Theory]
    [InlineData("https://localhost", "")]
    [InlineData("https://graph.microsoft.com", "token")]
    [InlineData("https://graph.microsoft.com/v1.0/me", "token")]
    public async Task GetAuthorizationTokenAsyncGetsToken(string url, string expectedToken)
    {
        // Arrange
        var mockTokenCredential = new Mock<TokenCredential>();
        mockTokenCredential.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(new AccessToken(expectedToken, DateTimeOffset.Now)));
        var azureIdentityAuthenticationProvider = new AzureIdentityAccessTokenProvider(mockTokenCredential.Object, null);

        // Act
        var token = await azureIdentityAuthenticationProvider.GetAuthorizationTokenAsync(new Uri(url));

        // Assert
        Assert.Equal(expectedToken, token);
    }

    [Theory]
    [InlineData("https://localhost", "")]
    [InlineData("https://graph.microsoft.com", "token")]
    [InlineData("https://graph.microsoft.com/v1.0/me", "token")]
    public async Task AuthenticateRequestAsyncSetsBearerHeader(string url, string expectedToken)
    {
        // Arrange
        var mockTokenCredential = new Mock<TokenCredential>();
        mockTokenCredential.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(new AccessToken(expectedToken, DateTimeOffset.Now)));
        var azureIdentityAuthenticationProvider = new AzureIdentityAuthenticationProvider(mockTokenCredential.Object, null ,"User.Read");
        var testRequest = new RequestInformation()
        {
            HttpMethod = Method.GET,
            URI = new Uri(url)
        };
        Assert.Empty(testRequest.Headers); // header collection is empty

        // Act
        await azureIdentityAuthenticationProvider.AuthenticateRequestAsync(testRequest);

        // Assert
        if(string.IsNullOrEmpty(expectedToken))
        {
            Assert.Empty(testRequest.Headers); // header collection is still empty
        }
        else
        {
            Assert.NotEmpty(testRequest.Headers); // header collection is no longer empty
            Assert.Equal("Authorization", testRequest.Headers.First().Key); // First element is Auth header
            Assert.Equal($"Bearer {expectedToken}", testRequest.Headers.First().Value); // First element is Auth header
        }
    }

    [Fact]
    public async Task GetAuthorizationTokenAsyncThrowsExcpetionForNonHTTPsUrl()
    {
        // Arrange
        var mockTokenCredential = new Mock<TokenCredential>();
        mockTokenCredential.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(new AccessToken(string.Empty, DateTimeOffset.Now)));
        var azureIdentityAuthenticationProvider = new AzureIdentityAccessTokenProvider(mockTokenCredential.Object, null);

        var nonHttpsUrl = "http://graph.microsoft.com";

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await azureIdentityAuthenticationProvider.GetAuthorizationTokenAsync(new Uri(nonHttpsUrl)));
        Assert.Equal("Only https is supported", exception.Message);
    }
}
