using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kiota.Builder.Plugins.Models;

internal class AppManifestModel
{
    public AppManifestModel()
    {
        // empty constructor to not mess with deserializers
    }

    public AppManifestModel(string pluginName, string documentName, string documentDescription)
    {
        PackageName = $"com.microsoft.kiota.plugin.{pluginName}";
        Name = new(pluginName, documentName);
        Description = new(documentDescription, documentName);
    }

    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://developer.microsoft.com/json-schemas/teams/vDevPreview/MicrosoftTeams.schema.json";
    public string ManifestVersion { get; set; } = "devPreview";
    public string Version { get; set; } = "1.0.0";
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Developer Developer { get; init; } = new();
    public string? PackageName
    {
        get; set;
    }
    public Name Name { get; set; } = new();
    public Description Description { get; set; } = new();
    public Icons Icons { get; set; } = new();
    public string AccentColor { get; set; } = "#FFFFFF";
    public CopilotExtensions CopilotExtensions { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, Object> AdditionalData { get; set; } = new();
}

[JsonSerializable(typeof(AppManifestModel))]
[JsonSerializable(typeof(JsonElement))]
internal partial class AppManifestModelGenerationContext : JsonSerializerContext
{
}

#pragma warning disable CA1054
internal class Developer(string? name = null, string? websiteUrl = null, string? privacyUrl = null, string? termsOfUseUrl = null)
#pragma warning restore CA1054
{
    public string Name { get; set; } = !string.IsNullOrEmpty(name) ? name : "Kiota Generator, Inc.";
#pragma warning disable CA1056
    public string WebsiteUrl { get; set; } = !string.IsNullOrEmpty(websiteUrl) ? websiteUrl : "https://www.example.com/contact/";
    public string PrivacyUrl { get; set; } = !string.IsNullOrEmpty(privacyUrl) ? privacyUrl : "https://www.example.com/privacy/";
    public string TermsOfUseUrl { get; set; } = !string.IsNullOrEmpty(termsOfUseUrl) ? termsOfUseUrl : "https://www.example.com/terms/";
#pragma warning restore CA1056

    [JsonExtensionData]
    public Dictionary<string, Object> AdditionalData { get; set; } = new();
}

internal class Name
{
    public Name()
    {
        // empty constructor to not mess with deserializers
    }

    public Name(string pluginName, string documentName)
    {
        ShortName = pluginName;
        FullName = $"API Plugin {pluginName} for {documentName}";
    }

    [JsonPropertyName("short")]
    public string ShortName { get; set; } = string.Empty;
    [JsonPropertyName("full")]
    public string FullName { get; set; } = string.Empty;
}

internal class Description
{
    public Description()
    {
        // empty constructor to not mess with deserializers
    }

    public Description(string description, string documentName)
    {
        ShortName = !string.IsNullOrEmpty(description) ? $"API Plugin for {description}." : documentName;
        FullName = !string.IsNullOrEmpty(description) ? $"API Plugin for {description}." : documentName;
    }

    [JsonPropertyName("short")]
    public string ShortName { get; set; } = string.Empty;
    [JsonPropertyName("full")]
    public string FullName { get; set; } = string.Empty;
}

internal class Icons
{
    public string Color { get; set; } = "color.png";
    public string Outline { get; set; } = "outline.png";
}

internal class CopilotExtensions
{
    public IList<Plugin> Plugins { get; set; } = new List<Plugin>();
}

internal class Plugin
{
    public Plugin()
    {
        // empty constructor to not mess with deserializers
    }

    public Plugin(string pluginName, string fileName)
    {
        Id = pluginName;
        File = fileName;
    }

    public string Id { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
}
