using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;

namespace Kiota.Builder;

public record LanguageInformation : IOpenApiSerializable
{
    public LanguageMaturityLevel MaturityLevel
    {
        get; set;
    }
    public SupportExperience SupportExperience
    {
        get; set;
    }
#pragma warning disable CA2227
#pragma warning disable CA1002
    public List<LanguageDependency> Dependencies { get; set; } = [];
#pragma warning restore CA1002
#pragma warning restore CA2227
    public string DependencyInstallCommand { get; set; } = string.Empty;
    public string ClientClassName { get; set; } = string.Empty;
    public string ClientNamespaceName { get; set; } = string.Empty;
#pragma warning disable CA2227
    public HashSet<string> StructuredMimeTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227
    public void SerializeAsV2(IOpenApiWriter writer) => SerializeInternal(writer, static (w, x) => x.SerializeAsV2(w));
    public void SerializeAsV3(IOpenApiWriter writer) => SerializeInternal(writer, static (w, x) => x.SerializeAsV3(w));
    public void SerializeAsV31(IOpenApiWriter writer) => SerializeInternal(writer, static (w, x) => x.SerializeAsV31(w));
    public void SerializeAsV32(IOpenApiWriter writer) => SerializeInternal(writer, static (w, x) => x.SerializeAsV32(w));
    public void SerializeInternal(IOpenApiWriter writer, Action<IOpenApiWriter, LanguageDependency> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteProperty(nameof(MaturityLevel).ToFirstCharacterLowerCase(), MaturityLevel.ToString());
        writer.WriteProperty(nameof(SupportExperience).ToFirstCharacterLowerCase(), SupportExperience.ToString());
        writer.WriteProperty(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), DependencyInstallCommand);
        writer.WriteOptionalCollection(nameof(Dependencies).ToFirstCharacterLowerCase(), Dependencies, callback);
        writer.WriteProperty(nameof(ClientClassName).ToFirstCharacterLowerCase(), ClientClassName);
        writer.WriteProperty(nameof(ClientNamespaceName).ToFirstCharacterLowerCase(), ClientNamespaceName);
        writer.WriteOptionalCollection(nameof(StructuredMimeTypes).ToFirstCharacterLowerCase(), StructuredMimeTypes, static (w, x) => { if (!string.IsNullOrEmpty(x)) { w.WriteValue(x); } });
        writer.WriteEndObject();
    }
    public static LanguageInformation Parse(JsonNode source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetValueKind() is not JsonValueKind.Object ||
        source.AsObject() is not JsonObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguageInformation();
        if (rawObject.TryGetPropertyValue(nameof(Dependencies).ToFirstCharacterLowerCase(), out var dependencies) && dependencies is JsonArray arrayValue)
        {
            foreach (var entry in arrayValue)
                if (entry is not null)
                    extension.Dependencies.Add(LanguageDependency.Parse(entry));
        }
        if (rawObject.TryGetPropertyValue(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), out var installCommand) && installCommand is JsonValue stringValue)
        {
            extension.DependencyInstallCommand = stringValue.GetValue<string>();
        }
        // not parsing the maturity level on purpose, we don't want APIs to be able to change that
        if (rawObject.TryGetPropertyValue(nameof(ClientClassName).ToFirstCharacterLowerCase(), out var clientClassName) && clientClassName is JsonValue clientClassNameValue)
        {
            extension.ClientClassName = clientClassNameValue.GetValue<string>();
        }
        if (rawObject.TryGetPropertyValue(nameof(ClientNamespaceName).ToFirstCharacterLowerCase(), out var clientNamespaceName) && clientNamespaceName is JsonValue clientNamespaceNameValue)
        {
            extension.ClientNamespaceName = clientNamespaceNameValue.GetValue<string>();
        }
        if (rawObject.TryGetPropertyValue(nameof(StructuredMimeTypes).ToFirstCharacterLowerCase(), out var structuredMimeTypes) && structuredMimeTypes is JsonArray structuredMimeTypesValue)
        {
            foreach (var entry in structuredMimeTypesValue.OfType<JsonValue>())
                extension.StructuredMimeTypes.Add(entry.GetValue<string>());
        }
        if (rawObject.TryGetPropertyValue(nameof(MaturityLevel).ToFirstCharacterLowerCase(), out var maturityLevel) && maturityLevel is JsonValue maturityLevelValue && maturityLevelValue.GetValueKind() is JsonValueKind.String && Enum.TryParse<LanguageMaturityLevel>(maturityLevelValue.GetValue<string>(), true, out var parsedMaturityLevelValue))
        {
            extension.MaturityLevel = parsedMaturityLevelValue;
        }
        if (rawObject.TryGetPropertyValue(nameof(SupportExperience).ToFirstCharacterLowerCase(), out var supportExperience) && supportExperience is JsonValue supportExperienceValue && supportExperienceValue.GetValueKind() is JsonValueKind.String && Enum.TryParse<SupportExperience>(supportExperienceValue.GetValue<string>(), true, out var parsedSupportExperienceValue))
        {
            extension.SupportExperience = parsedSupportExperienceValue;
        }
        return extension;
    }
}
public record LanguageDependency : IOpenApiSerializable
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    [JsonPropertyName("Type")]
    public DependencyType? DependencyType
    {
        get; set;
    }
    private const string TypePropertyName = "type";
    public void SerializeAsV2(IOpenApiWriter writer) => SerializeInternal(writer);
    public void SerializeAsV3(IOpenApiWriter writer) => SerializeInternal(writer);
    public void SerializeAsV31(IOpenApiWriter writer) => SerializeInternal(writer);
    public void SerializeAsV32(IOpenApiWriter writer) => SerializeInternal(writer);
    public void SerializeInternal(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteProperty(nameof(Name).ToFirstCharacterLowerCase(), Name);
        writer.WriteProperty(nameof(Version).ToFirstCharacterLowerCase(), Version);
        if (DependencyType is not null)
        {
            writer.WriteProperty(TypePropertyName, DependencyType.ToString());
        }
        writer.WriteEndObject();
    }
    public static LanguageDependency Parse(JsonNode source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetValueKind() is not JsonValueKind.Object ||
        source.AsObject() is not JsonObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguageDependency();
        if (rawObject.TryGetPropertyValue(nameof(Name).ToFirstCharacterLowerCase(), out var nameNode) && nameNode is JsonValue nameJsonValue && nameJsonValue.TryGetValue<string>(out var nameValue))
        {
            extension.Name = nameValue;
        }
        if (rawObject.TryGetPropertyValue(nameof(Version).ToFirstCharacterLowerCase(), out var versionNode) && versionNode is JsonValue versionJsonValue && versionJsonValue.TryGetValue<string>(out var versionValue))
        {
            extension.Version = versionValue;
        }
        if (rawObject.TryGetPropertyValue(TypePropertyName, out var typeNode) && typeNode is JsonValue typeJsonValue && typeJsonValue.TryGetValue<string>(out var typeValue) && Enum.TryParse<DependencyType>(typeValue, true, out var parsedTypeValue))
        {
            extension.DependencyType = parsedTypeValue;
        }
        return extension;
    }
}

public enum LanguageMaturityLevel
{
    Experimental,
    Preview,
    Stable,
    Abandoned
}

public enum SupportExperience
{
    Microsoft,
    Community
}

public enum DependencyType
{
    Abstractions,
    Serialization,
    Authentication,
    Http,
    Bundle,
    Additional
}
