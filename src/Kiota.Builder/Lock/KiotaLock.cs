using System;
using System.Collections.Generic;
using System.Reflection;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Lock;

/// <summary>
/// A class that represents a lock file for a Kiota project.
/// </summary>
public class KiotaLock {
    /// <summary>
    /// The OpenAPI description hash that generated this client.
    /// </summary>
    public string DescriptionHash { get; set; }
    /// <summary>
    /// The location of the OpenAPI description file.
    /// </summary>
    public string DescriptionLocation { get; set; }
    /// <summary>
    /// The version of the lock file schema.
    /// </summary>
    public string LockFileVersion { get; set; } = "1.0.0";
    /// <summary>
    /// The version of the Kiota generator that generated this client.
    /// </summary>
    public string KiotaVersion { get; set; } = Assembly.GetEntryAssembly().GetName().Version.ToString();
    /// <summary>
    /// The main class name for this client.
    /// </summary>
    public string ClientClassName { get; set; }
    /// <summary>
    /// The main namespace for this client.
    /// </summary>
    public string ClientNamespaceName { get; set; }
    /// <summary>
    /// The language for this client.
    /// </summary>
    public string Language { get; set; }
    /// <summary>
    /// Whether the backing store was used for this client.
    /// </summary>
    public bool UsesBackingStore { get; set; }
    /// <summary>
    /// Whether additional data was used for this client.
    /// </summary>
    public bool IncludeAdditionalData { get; set; }
    /// <summary>
    /// The serializers used for this client.
    /// </summary>
    public HashSet<string> Serializers { get; set; } = new();
    /// <summary>
    /// The deserializers used for this client.
    /// </summary>
    public HashSet<string> Deserializers { get; set; } = new();
    /// <summary>
    /// The structured mime types used for this client.
    /// </summary>
    public HashSet<string> StructuredMimeTypes { get; set; } = new();
    /// <summary>
    /// The path patterns for API endpoints to include for this client.
    /// </summary>
    public HashSet<string> IncludePatterns { get; set; } = new();
    /// <summary>
    /// The path patterns for API endpoints to exclude for this client.
    /// </summary>
    public HashSet<string> ExcludePatterns { get; set; } = new();
    /// <summary>
    /// Updates the passed configuration with the values from the lock file.
    /// </summary>
    /// <param name="config">The configuration to update.</param>
    public void UpdateGenerationConfigurationFromLock(GenerationConfiguration config) {
        config.ClientClassName = ClientClassName;
        config.ClientNamespaceName = ClientNamespaceName;
        if(Enum.TryParse<GenerationLanguage>(Language, out var parsedLanguage))
            config.Language = parsedLanguage;
        config.UsesBackingStore = UsesBackingStore;
        config.IncludeAdditionalData = IncludeAdditionalData;
        config.Serializers = Serializers;
        config.Deserializers = Deserializers;
        config.StructuredMimeTypes = StructuredMimeTypes;
        config.IncludePatterns = IncludePatterns;
        config.ExcludePatterns = ExcludePatterns;
        config.OpenAPIFilePath = DescriptionLocation;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="KiotaLock"/> class.
    /// </summary>
    public KiotaLock() { }
    /// <summary>
    /// Initializes a new instance of the <see cref="KiotaLock"/> class from the passed configuration.
    /// </summary>
    /// <param name="config">The configuration to use.</param>
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
