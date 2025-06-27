using System;
using Kiota.Builder.Configuration;
using Kiota.Builder.Plugins;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests.Plugins;

public class PluginAuthConfigurationTests
{
    [Fact]
    public void ThrowsExceptionIfReferenceIdIsNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new PluginAuthConfiguration(null);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new PluginAuthConfiguration(string.Empty);
        });
    }

    [Fact]
    public void ThrowsExceptionWhenToPluginManifestAuthEncountersUnsupportedAuthType()
    {
        var auth = new PluginAuthConfiguration("reference")
        {
            AuthType = (PluginAuthType)10
        };
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            auth.ToPluginManifestAuth();
        });
    }

    [Fact]
    public void AddCoverageOnUnsupportedException()
    {
        _ = new UnsupportedSecuritySchemeException();
        _ = new UnsupportedSecuritySchemeException("msg");
        _ = new UnsupportedSecuritySchemeException("msg", new Exception());
        var a = new UnsupportedSecuritySchemeException(["t0"], "msg");
        Assert.NotEmpty(a.SupportedTypes);
        var b = new UnsupportedSecuritySchemeException(["t0"], "msg", new Exception());
        Assert.NotEmpty(b.SupportedTypes);
    }

    [Fact]
    public void FromParametersThrowArgumentNullExceptionIfSecuritySchemeTypeHasNoValue()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = PluginAuthConfiguration.FromParameters(null, "reference");
        });
    }

    [Fact]
    public void FromParametersThrowsExceptionIfSecuritySchemeTypeIsUnknown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = PluginAuthConfiguration.FromParameters((SecuritySchemeType)10, "reference");
        });
    }

    [Fact]
    public void FromParametersReturnsCorrectPluginAuthConfiguration()
    {
        var pluginAuthConfig = PluginAuthConfiguration.FromParameters(SecuritySchemeType.ApiKey, "reference");
        Assert.Equal(PluginAuthType.ApiKeyPluginVault, pluginAuthConfig.AuthType);
        Assert.Equal("reference", pluginAuthConfig.ReferenceId);

        pluginAuthConfig = PluginAuthConfiguration.FromParameters(SecuritySchemeType.Http, "reference");
        Assert.Equal(PluginAuthType.ApiKeyPluginVault, pluginAuthConfig.AuthType);
        Assert.Equal("reference", pluginAuthConfig.ReferenceId);

        pluginAuthConfig = PluginAuthConfiguration.FromParameters(SecuritySchemeType.OpenIdConnect, "reference");
        Assert.Equal(PluginAuthType.ApiKeyPluginVault, pluginAuthConfig.AuthType);
        Assert.Equal("reference", pluginAuthConfig.ReferenceId);

        pluginAuthConfig = PluginAuthConfiguration.FromParameters(SecuritySchemeType.OAuth2, "reference");
        Assert.Equal(PluginAuthType.OAuthPluginVault, pluginAuthConfig.AuthType);
        Assert.Equal("reference", pluginAuthConfig.ReferenceId);
    }
}
