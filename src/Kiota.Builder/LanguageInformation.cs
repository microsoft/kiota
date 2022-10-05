using System;
using System.Collections.Generic;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder;

public record LanguageInformation: IOpenApiSerializable {
    public LanguageMaturityLevel MaturityLevel {get; set;}
    public List<LanguageDependency> Dependencies {get; set;} = new();
    public string DependencyInstallCommand {get; set;}

    public void SerializeAsV2(IOpenApiWriter writer) => SerializeAsV3(writer);
    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteProperty(nameof(MaturityLevel).ToFirstCharacterLowerCase(), MaturityLevel.ToString());
        writer.WriteProperty(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), DependencyInstallCommand);
        writer.WriteOptionalCollection(nameof(Dependencies).ToFirstCharacterLowerCase(), Dependencies, (w, x) => x.SerializeAsV3(w));
        writer.WriteEndObject();
    }
    public static LanguageInformation Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguageInformation();
        if (rawObject.TryGetValue(nameof(Dependencies).ToFirstCharacterLowerCase(), out var dependencies) && dependencies is OpenApiArray arrayValue) {
            foreach(var entry in arrayValue)
                extension.Dependencies.Add(LanguageDependency.Parse(entry));
        }
        if (rawObject.TryGetValue(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), out var installCommand) && installCommand is OpenApiString stringValue) {
            extension.DependencyInstallCommand = stringValue.Value;
        }
        if (rawObject.TryGetValue(nameof(MaturityLevel).ToFirstCharacterLowerCase(), out var matLevel) && matLevel is OpenApiString matLevelStr &&
            Enum.TryParse<LanguageMaturityLevel>(matLevelStr.Value, out var matLevelValue)) {
            extension.MaturityLevel = matLevelValue;
        }
        return extension;
    }
}
public record LanguageDependency: IOpenApiSerializable {
    public string Name {get; set;}
    public string Version {get; set;}
    public void SerializeAsV2(IOpenApiWriter writer) => SerializeAsV3(writer);
    public void SerializeAsV3(IOpenApiWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteProperty(nameof(Name).ToFirstCharacterLowerCase(), Name);
        writer.WriteProperty(nameof(Version).ToFirstCharacterLowerCase(), Version);
        writer.WriteEndObject();
    }
    public static LanguageDependency Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new LanguageDependency();
        if (rawObject.TryGetValue(nameof(Name).ToFirstCharacterLowerCase(), out var name) && name is OpenApiString stringValue) {
            extension.Name = stringValue.Value;
        }
        if (rawObject.TryGetValue(nameof(Version).ToFirstCharacterLowerCase(), out var version) && version is OpenApiString versionValue) {
            extension.Version = versionValue.Value;
        }
        return extension;
    }
}

public enum LanguageMaturityLevel {
    Experimental,
    Preview,
    Stable
}
