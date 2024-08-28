using System;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Configuration;

/// <summary>
/// Auth information used in generated plugin manifest
/// </summary>
public class PluginAuthConfiguration
{
    /// <summary>
    /// Auth information used in generated plugin manifest
    /// </summary>
    public PluginAuthConfiguration(string referenceId)
    {
        if (string.IsNullOrWhiteSpace(referenceId)) throw new ArgumentException("Plugin authentication's referenceId is required.", nameof(referenceId));
        ReferenceId = referenceId;
    }

    /// <summary>
    /// The Teams Toolkit compatible plugin auth type.
    /// </summary>
    public PluginAuthType AuthType { get; set; }

    /// <summary>
    /// The Teams Toolkit plugin auth reference id
    /// </summary>
    public string ReferenceId { get; set; }

    internal Auth ToPluginManifestAuth()
    {
        return AuthType switch
        {
            PluginAuthType.OAuthPluginVault => new OAuthPluginVault {ReferenceId = ReferenceId},
            PluginAuthType.ApiKeyPluginVault => new ApiKeyPluginVault { ReferenceId = ReferenceId},
            _ => throw new ArgumentOutOfRangeException(nameof(AuthType), $"Unknown plugin auth type '{AuthType}'")
        };
    }
}
