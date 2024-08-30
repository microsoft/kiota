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
}
