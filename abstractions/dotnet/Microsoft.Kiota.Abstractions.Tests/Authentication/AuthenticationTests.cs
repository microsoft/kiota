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

    [Theory]
    [InlineData("https://graph.microsoft.com",true)]// PASS
    [InlineData("https://graph.microsoft.us/v1.0/me",true)]// PASS as we don't look at the path segment
    [InlineData("https://test.microsoft.com",false)]// Fail
    [InlineData("https://grAph.MicrosofT.com",true)] // PASS since we don't care about case
    [InlineData("https://developer.microsoft.com",false)] // Failed
    public void AllowedHostValidatorValidatesUrls(string urlToTest, bool expectedResult)
    {
        // Test through the constructor
        // Arrange
        var whiteList = new[] { "graph.microsoft.com", "graph.microsoft.us"};
        var validator = new AllowedHostsValidator(whiteList);

        // Act 
        var validationResult = validator.IsUrlHostValid(new Uri(urlToTest));

        // Assert
        Assert.Equal(expectedResult, validationResult);
        Assert.Contains(whiteList[0], validator.AllowedHosts);
        Assert.Contains(whiteList[1], validator.AllowedHosts);


        // Test through the setter
        // Arrange
        var emptyValidator = new AllowedHostsValidator
        {
            AllowedHosts = whiteList // set the validator through the property
        };

        // Act 
        var emptyValidatorResult = emptyValidator.IsUrlHostValid(new Uri(urlToTest));

        // Assert
        Assert.Equal(emptyValidatorResult, validationResult);
        Assert.Contains(whiteList[0], emptyValidator.AllowedHosts);
        Assert.Contains(whiteList[1], emptyValidator.AllowedHosts);
    }


    [Theory]
    [InlineData("https://graph.microsoft.com")]// PASS
    [InlineData("https://graph.microsoft.us/v1.0/me")]// PASS
    [InlineData("https://test.microsoft.com")]// PASS
    [InlineData("https://grAph.MicrosofT.com")] // PASS
    [InlineData("https://developer.microsoft.com")] // PASS
    public void AllowedHostValidatorAllowsAllUrls(string urlToTest)
    {
        // Test through the constructor
        // Arrange
        var validator = new AllowedHostsValidator();

        // Act 
        var validationResult = validator.IsUrlHostValid(new Uri(urlToTest));

        // Assert
        Assert.True(validationResult);
        Assert.Empty(validator.AllowedHosts);
    }
}
