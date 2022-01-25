using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests;
public class AuthenticationTests
{
    [Fact]
    public async Task AnonymousAuthenticationProviderReturnsSameRequestAsync()
    {
        // Arrange
        var anonymousAuthenticationProvider = new AnonymousAuthenticationProvider();
        var testRequest = new RequestInformation()
        {
            HttpMethod = Method.GET,
            URI = new Uri("http://localhost")
        };
        Assert.Empty(testRequest.Headers); // header collection is empty

        // Act
        await anonymousAuthenticationProvider.AuthenticateRequestAsync(testRequest);

        // Assert
        Assert.Empty(testRequest.Headers); // header collection is still empty

    }

    [Fact]
    public async Task BaseBearerTokenAuthenticationProviderSetsBearerHeader()
    {
        // Arrange
        var expectedToken = "token";
        var mockAccessTokenProvider = new Mock<IAccessTokenProvider>();
        mockAccessTokenProvider.Setup(authProvider => authProvider.GetAuthorizationTokenAsync(It.IsAny<Uri>(),It.IsAny<CancellationToken>())).Returns(Task.FromResult(expectedToken));
        var testAuthProvider = new BaseBearerTokenAuthenticationProvider(mockAccessTokenProvider.Object);
        var testRequest = new RequestInformation()
        {
            HttpMethod = Method.GET,
            URI = new Uri("http://localhost")
        };
        Assert.Empty(testRequest.Headers); // header collection is empty

        // Act
        await testAuthProvider.AuthenticateRequestAsync(testRequest);

        // Assert
        Assert.NotEmpty(testRequest.Headers); // header collection is longer empty
        Assert.Equal("Authorization", testRequest.Headers.First().Key); // First element is Auth header
        Assert.Equal($"Bearer {expectedToken}", testRequest.Headers.First().Value); // First element is Auth header
    }
}
