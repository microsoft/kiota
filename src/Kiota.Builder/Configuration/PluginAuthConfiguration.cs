using System;
using Microsoft.DeclarativeAgents.Manifest;
using Microsoft.OpenApi;


namespace Kiota.Builder.Configuration;

/// <summary>
/// Auth information used in generated plugin manifest
/// </summary>
public class PluginAuthConfiguration
{
    /// <summary>
    /// Auth information used in generated plugin manifest
    /// </summary>
    /// <param name="referenceId">The auth reference id</param>
    /// <exception cref="ArgumentException">If the reference id is null or contains only whitespaces.</exception>
    public PluginAuthConfiguration(string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);
        ReferenceId = referenceId;
    }

    /// <summary>
    /// The Teams Toolkit compatible plugin auth type.
    /// </summary>
    public PluginAuthType AuthType
    {
        get; set;
    }

    /// <summary>
    /// The Teams Toolkit plugin auth reference id
    /// </summary>
    public string ReferenceId
    {
        get; set;
    }

    internal Auth ToPluginManifestAuth()
    {
        return AuthType switch
        {
            PluginAuthType.OAuthPluginVault => new OAuthPluginVault { ReferenceId = ReferenceId },
            PluginAuthType.ApiKeyPluginVault => new ApiKeyPluginVault { ReferenceId = ReferenceId },
            _ => throw new ArgumentOutOfRangeException(nameof(AuthType), $"Unknown plugin auth type '{AuthType}'")
        };
    }

    /// <summary>
    /// Constructs a PluginAuthConfiguration object from SecuritySchemeType and reference id.
    /// </summary>
    /// <param name="pluginAuthType">The SecuritySchemeType.</param>
    /// <param name="pluginAuthRefId">The reference id.</param>
    /// <returns>A PluginAuthConfiguration object.</returns>
    /// <exception cref="ArgumentException">If the reference id is null or contains only whitespaces.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the SecuritySchemeType is unknown.</exception>
    public static PluginAuthConfiguration FromParameters(SecuritySchemeType? pluginAuthType, string pluginAuthRefId)
    {
        if (!pluginAuthType.HasValue)
        {
            throw new ArgumentNullException(nameof(pluginAuthType), "Missing plugin auth type");
        }

        var pluginAuthConfig = new PluginAuthConfiguration(pluginAuthRefId);
        switch (pluginAuthType)
        {
            case SecuritySchemeType.ApiKey:
            case SecuritySchemeType.Http:
            case SecuritySchemeType.OpenIdConnect:
                pluginAuthConfig.AuthType = PluginAuthType.ApiKeyPluginVault;
                break;
            case SecuritySchemeType.OAuth2:
                pluginAuthConfig.AuthType = PluginAuthType.OAuthPluginVault;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pluginAuthType), $"Unknown plugin auth type '{pluginAuthType}'");
        }

        return pluginAuthConfig;
    }
}
