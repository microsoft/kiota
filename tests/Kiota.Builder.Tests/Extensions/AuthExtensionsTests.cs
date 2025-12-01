using Kiota.Builder.Extensions;
using Microsoft.DeclarativeAgents.Manifest;
using Xunit;

namespace Kiota.Builder.Tests.Extensions;

public class AuthExtensionsTests
{
    private class UnmanagedAuth : Auth
    {
        public string ReferenceId
        {
            get; set;
        }
    }

    [Fact]
    public void GetReferenceId_FromNull()
    {
        var auth = null as Auth;
        var actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromOAuthPluginVault_WithValidReferenceId()
    {
        var auth = new OAuthPluginVault()
        {
            ReferenceId = "test_refid"
        };
        var actual = auth.GetReferenceId();
        Assert.Equal("test_refid", actual);
    }

    [Fact]
    public void GetReferenceId_FromOAuthPluginVault_WithNullReferenceId()
    {
        var auth = new OAuthPluginVault()
        {
            ReferenceId = null
        };
        var actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromApiKeyPluginVault_WithValidReferenceId()
    {
        var auth = new OAuthPluginVault()
        {
            ReferenceId = "test_refid"
        };
        var actual = auth.GetReferenceId();
        Assert.Equal("test_refid", actual);
    }

    [Fact]
    public void GetReferenceId_FromApiKeyPluginVault_WithNullReferenceId()
    {
        var auth = new ApiKeyPluginVault()
        {
            ReferenceId = null
        };
        var actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromUnmanagedAuth_WithValidReferenceId()
    {
        var auth = new UnmanagedAuth()
        {
            ReferenceId = "test_refid"
        };
        var actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromUnmanagedAuth_WithNullReferenceId()
    {
        var auth = new UnmanagedAuth()
        {
            ReferenceId = null
        };
        var actual = auth.GetReferenceId();
        Assert.Null(actual);
    }
}
