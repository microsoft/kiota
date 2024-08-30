﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi.ApiManifest;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.WorkspaceManagement;

#pragma warning disable CA2227 // Collection properties should be read only
public class ApiPluginConfiguration : BaseApiConsumerConfiguration, ICloneable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiPluginConfiguration"/> class.
    /// </summary>
    public ApiPluginConfiguration() : base()
    {

    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiPluginConfiguration"/> class from an existing <see cref="GenerationConfiguration"/>.
    /// </summary>
    /// <param name="config">The configuration to use to initialize the client configuration</param>
    public ApiPluginConfiguration(GenerationConfiguration config) : base(config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Types = config.PluginTypes.Select(x => x.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        AuthType = config.PluginAuthInformation?.AuthType.ToString();
        AuthReferenceId = config.PluginAuthInformation?.ReferenceId;
    }
    public HashSet<string> Types { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? AuthType
    {
        get;
        set;
    }

    public string? AuthReferenceId
    {
        get;
        set;
    }

    public object Clone()
    {
        var result = new ApiPluginConfiguration()
        {
            AuthType = AuthType,
            AuthReferenceId = AuthReferenceId,
            Types = new HashSet<string>(Types, StringComparer.OrdinalIgnoreCase)
        };
        CloneBase(result);
        return result;
    }
    /// <summary>
    /// Updates the passed configuration with the values from the config file.
    /// </summary>
    /// <param name="config">Generation configuration to update.</param>
    /// <param name="pluginName">Plugin name.</param>
    /// <param name="requests">The requests to use when updating an existing client.</param>
    public void UpdateGenerationConfigurationFromApiPluginConfiguration(GenerationConfiguration config, string pluginName, IList<RequestInfo>? requests = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(pluginName);
        config.PluginTypes = Types.Select(x => Enum.TryParse<PluginType>(x, true, out var result) ? result : (PluginType?)null).OfType<PluginType>().ToHashSet();
        if (AuthReferenceId is not null && Enum.TryParse<PluginAuthType>(AuthType, out var authType))
        {
            config.PluginAuthInformation = new PluginAuthConfiguration(AuthReferenceId)
            {
                AuthType = authType,
            };
        }
        UpdateGenerationConfigurationFromBase(config, pluginName, requests);
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
