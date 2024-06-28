using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kiota.Builder.Plugins.Models;

public class AppManifestModel(string pluginName, string documentName, string documentDescription)
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://developer.microsoft.com/json-schemas/teams/vDevPreview/MicrosoftTeams.schema.json";
    public string ManifestVersion { get; set; } = "devPreview";
    public string Version { get; set; } = "1.0.0";
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Developer Developer { get; init; } = new();
    public string PackageName { get; set; } = $"com.microsoft.kiota.plugin.{pluginName}";
    public Name Name { get; set; } = new(pluginName, documentName);
    public Description Description { get; set; } = new(documentDescription, documentName);
    public Icons Icons { get; set; } = new();
    public string AccentColor { get; set; } = "#FFFFFF";
    public CopilotExtensions CopilotExtensions { get; set; } = new();
}

[JsonSerializable(typeof(AppManifestModel))]
internal partial class AppManifestModelGenerationContext : JsonSerializerContext
{
}

#pragma warning disable CA1054
public class Developer(string? name = null, string? websiteUrl = null, string? privacyUrl = null, string? termsOfUseUrl = null)
#pragma warning restore CA1054
{
    public string Name { get; set; } = !string.IsNullOrEmpty(name) ? name : "Kiota Generator, Inc.";
#pragma warning disable CA1056
    public string WebsiteUrl { get; set; } = !string.IsNullOrEmpty(websiteUrl) ? websiteUrl : "https://www.example.com/contact/";
    public string PrivacyUrl { get; set; } = !string.IsNullOrEmpty(privacyUrl) ? privacyUrl : "https://www.example.com/privacy/";
    public string TermsOfUseUrl { get; set; } = !string.IsNullOrEmpty(termsOfUseUrl) ? termsOfUseUrl : "https://www.example.com/terms/";
#pragma warning restore CA1056
}

public class Name(string pluginName, string documentName)
{
    [JsonPropertyName("short")]
    public string ShortName { get; private set; } = pluginName;
    [JsonPropertyName("full")]
    public string FullName { get; private set; } = $"API Plugin {pluginName} for {documentName}";
}

public class Description(string description, string documentName)
{
    [JsonPropertyName("short")]
    public string ShortName { get; private set; } = !string.IsNullOrEmpty(description) ? $"API Plugin for {description}." : documentName;
    [JsonPropertyName("full")]
    public string FullName { get; private set; } = !string.IsNullOrEmpty(description) ? $"API Plugin for {description}." : documentName;
}

public class Icons
{
    public string Color { get; set; } = "color.png";
    public string Outline { get; set; } = "outline.png";
}

public class CopilotExtensions
{
    public IList<Plugin> Plugins { get; } = new List<Plugin>();
}

public class Plugin(string pluginName, string fileName)
{
    public string Id { get; set; } = pluginName;
    public string File { get; set; } = fileName;
}
