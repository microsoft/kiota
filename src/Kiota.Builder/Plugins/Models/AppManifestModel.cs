using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kiota.Builder.Plugins.Models;

internal class AppManifestModel
{
    private const string DefaultSchema = "https://developer.microsoft.com/json-schemas/teams/vDevPreview/MicrosoftTeams.schema.json";
    private const string DefaultManifestVersion = "devPreview";
    private const string DefaultVersion = "1.0.0";

    [JsonPropertyName("$schema")]
    public string? Schema
    {
        get;
        set;
    } = DefaultSchema;

    public string? ManifestVersion
    {
        get;
        set;
    } = DefaultManifestVersion;

    public string? Version
    {
        get;
        set;
    } = DefaultVersion;
    public string? Id
    {
        get; set;
    }
    public Developer? Developer
    {
        get; init;
    }
    public string? PackageName
    {
        get; set;
    }
    public Name? Name
    {
        get; set;
    }
    public Description? Description
    {
        get; set;
    }
    public Icons? Icons
    {
        get; set;
    }
    public string? AccentColor
    {
        get; set;
    }
    public CopilotExtensions? CopilotExtensions
    {
        get; set;
    }

    [JsonExtensionData]
    public Dictionary<string, Object> AdditionalData { get; set; } = new();
}

[JsonSerializable(typeof(AppManifestModel))]
[JsonSerializable(typeof(JsonElement))]
internal partial class AppManifestModelGenerationContext : JsonSerializerContext
{
}

#pragma warning disable CA1054
internal class Developer
#pragma warning restore CA1054
{
    public string? Name
    {
        get; set;
    }
#pragma warning disable CA1056
    public string? WebsiteUrl
    {
        get; set;
    }
    public string? PrivacyUrl
    {
        get; set;
    }
    public string? TermsOfUseUrl
    {
        get; set;
    }
#pragma warning restore CA1056

    [JsonExtensionData]
    public Dictionary<string, Object> AdditionalData { get; set; } = new();
}

internal class Name
{
    [JsonPropertyName("short")]
    public string? ShortName
    {
        get; set;
    }
    [JsonPropertyName("full")]
    public string? FullName
    {
        get; set;
    }
}

internal class Description
{
    [JsonPropertyName("short")]
    public string? ShortName
    {
        get; set;
    }
    [JsonPropertyName("full")]
    public string? FullName
    {
        get; set;
    }
}

internal class Icons
{
    public string? Color
    {
        get;
        set;
    } = "color.png";

    public string? Outline
    {
        get;
        set;
    } = "outline.png";
}

internal class CopilotExtensions
{
    public IList<Plugin>? Plugins
    {
        get; set;
    }
    public IList<DeclarativeCopilot>? DeclarativeCopilots
    {
        get; set;
    }
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = [];
}

internal class Plugin
{
    public string? Id
    {
        get; set;
    }
    public string? File
    {
        get; set;
    }
}

internal class DeclarativeCopilot
{
    public string? Id
    {
        get; set;
    }
    public string? File
    {
        get; set;
    }
}
