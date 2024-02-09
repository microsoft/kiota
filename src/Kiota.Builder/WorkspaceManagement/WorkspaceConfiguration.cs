using System;
using System.Collections.Generic;

namespace Kiota.Builder.WorkspaceManagement;

public class WorkspaceConfiguration
{
    /// <summary>
    /// The version of the configuration file schema.
    /// </summary>
    public string Version { get; set; } = "1.0.0";
#pragma warning disable CA2227 // Collection properties should be read only
    /// <summary>
    /// The clients to generate.
    /// </summary>
    public Dictionary<string, ApiClientConfiguration> Clients { get; set; } = new Dictionary<string, ApiClientConfiguration>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227 // Collection properties should be read only
}
