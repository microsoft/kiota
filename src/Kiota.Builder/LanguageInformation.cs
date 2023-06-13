using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder;

public record LanguageInformation : IOpenApiSerializable
{
    public LanguageMaturityLevel MaturityLevel
    {
        get; set;
    }
    public List<LanguageDependency> Dependencies { get; set; } = new();
    public string DependencyInstallCommand { get; set; } = string.Empty;
    public string ClientClassName { get; set; } = string.Empty;
    public string ClientNamespaceName { get; set; } = string.Empty;
    public HashSet<string> StructuredMimeTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public void SerializeAsV2(IOpenApiWriter writer) => SerializeAsV3(writer);
    public void SerializeAsV3(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteProperty(nameof(MaturityLevel).ToFirstCharacterLowerCase(), MaturityLevel.ToString());
        writer.WriteProperty(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), DependencyInstallCommand);
        writer.WriteOptionalCollection(nameof(Dependencies).ToFirstCharacterLowerCase(), Dependencies, (w, x) => x.SerializeAsV3(w));
        writer.WriteProperty(nameof(ClientClassName).ToFirstCharacterLowerCase(), ClientClassName);
        writer.WriteProperty(nameof(ClientNamespaceName).ToFirstCharacterLowerCase(), ClientNamespaceName);
        writer.WriteOptionalCollection(nameof(StructuredMimeTypes).ToFirstCharacterLowerCase(), StructuredMimeTypes, (w, x) => w.WriteValue(x));
        writer.WriteEndObject();
    }
    public static LanguageInformation Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguageInformation();
        if (rawObject.TryGetValue(nameof(Dependencies).ToFirstCharacterLowerCase(), out var dependencies) && dependencies is OpenApiArray arrayValue)
        {
            foreach (var entry in arrayValue)
                extension.Dependencies.Add(LanguageDependency.Parse(entry));
        }
        if (rawObject.TryGetValue(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), out var installCommand) && installCommand is OpenApiString stringValue)
        {
            extension.DependencyInstallCommand = stringValue.Value;
        }
        // not parsing the maturity level on purpose, we don't want APIs to be able to change that
        if (rawObject.TryGetValue(nameof(ClientClassName).ToFirstCharacterLowerCase(), out var clientClassName) && clientClassName is OpenApiString clientClassNameValue)
        {
            extension.ClientClassName = clientClassNameValue.Value;
        }
        if (rawObject.TryGetValue(nameof(ClientNamespaceName).ToFirstCharacterLowerCase(), out var clientNamespaceName) && clientNamespaceName is OpenApiString clientNamespaceNameValue)
        {
            extension.ClientNamespaceName = clientNamespaceNameValue.Value;
        }
        if (rawObject.TryGetValue(nameof(StructuredMimeTypes).ToFirstCharacterLowerCase(), out var structuredMimeTypes) && structuredMimeTypes is OpenApiArray structuredMimeTypesValue)
        {
            foreach (var entry in structuredMimeTypesValue.OfType<OpenApiString>())
                extension.StructuredMimeTypes.Add(entry.Value);
        }
        return extension;
    }
}
public record LanguageDependency : IOpenApiSerializable
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public void SerializeAsV2(IOpenApiWriter writer) => SerializeAsV3(writer);
    public void SerializeAsV3(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteProperty(nameof(Name).ToFirstCharacterLowerCase(), Name);
        writer.WriteProperty(nameof(Version).ToFirstCharacterLowerCase(), Version);
        writer.WriteEndObject();
    }
    public static LanguageDependency Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguageDependency();
        if (rawObject.TryGetValue(nameof(Name).ToFirstCharacterLowerCase(), out var name) && name is OpenApiString stringValue)
        {
            extension.Name = stringValue.Value;
        }
        if (rawObject.TryGetValue(nameof(Version).ToFirstCharacterLowerCase(), out var version) && version is OpenApiString versionValue)
        {
            extension.Version = versionValue.Value;
        }
        return extension;
    }
}

public enum LanguageMaturityLevel
{
    Experimental,
    Preview,
    Stable
}
