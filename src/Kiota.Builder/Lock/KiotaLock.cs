using System;
using System.Collections.Generic;
using System.Reflection;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Lock;

public class KiotaLock {
    public string DescriptionHash { get; set; }
    public string DescriptionLocation { get; set; }
    public string LockFileVersion { get; set; } = "1.0.0";
    public string KiotaVersion { get; set; } = Assembly.GetEntryAssembly().GetName().Version.ToString();
    public string ClientClassName { get; set; }
    public string ClientNamespaceName { get; set; }
    public string Language { get; set; }
    public bool UsesBackingStore { get; set; }
    public bool IncludeAdditionalData { get; set; }
    public HashSet<string> Serializers { get; set; } = new();
    public HashSet<string> Deserializers { get; set; } = new();
    public HashSet<string> StructuredMimeTypes { get; set; } = new();
    public HashSet<string> IncludePatterns { get; set; } = new();
    public HashSet<string> ExcludePatterns { get; set; } = new();
    public void UpdateGenerationConfigurationFromLock(GenerationConfiguration config) {
        config.ClientClassName = ClientClassName;
        config.ClientNamespaceName = ClientNamespaceName;
        config.Language = Enum.Parse<GenerationLanguage>(Language);
        config.UsesBackingStore = UsesBackingStore;
        config.IncludeAdditionalData = IncludeAdditionalData;
        config.Serializers = Serializers;
        config.Deserializers = Deserializers;
        config.StructuredMimeTypes = StructuredMimeTypes;
        config.IncludePatterns = IncludePatterns;
        config.ExcludePatterns = ExcludePatterns;
        config.OpenAPIFilePath = DescriptionLocation;
    }
    public KiotaLock() { }
    public KiotaLock(GenerationConfiguration config)
    {
        Language = config.Language.ToString();
        ClientClassName = config.ClientClassName;
        ClientNamespaceName = config.ClientNamespaceName;
        UsesBackingStore = config.UsesBackingStore;
        IncludeAdditionalData = config.IncludeAdditionalData;
        Serializers = config.Serializers;
        Deserializers = config.Deserializers;
        StructuredMimeTypes = config.StructuredMimeTypes;
        IncludePatterns = config.IncludePatterns;
        ExcludePatterns = config.ExcludePatterns;
        DescriptionLocation = config.OpenAPIFilePath;
    }
}
