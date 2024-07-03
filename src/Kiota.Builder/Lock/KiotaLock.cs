﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Lock;

/// <summary>
/// A class that represents a lock file for a Kiota project.
/// </summary>
public class KiotaLock
{
    /// <summary>
    /// The OpenAPI description hash that generated this client.
    /// </summary>
    public string DescriptionHash { get; set; } = string.Empty;
    /// <summary>
    /// The location of the OpenAPI description file.
    /// </summary>
    public string DescriptionLocation { get; set; } = string.Empty;
    /// <summary>
    /// The version of the lock file schema.
    /// </summary>
    public string LockFileVersion { get; set; } = "1.0.0";
    /// <summary>
    /// The version of the Kiota generator that generated this client.
    /// </summary>
    public string KiotaVersion { get; set; } = Kiota.Generated.KiotaVersion.Current();
    /// <summary>
    /// The main class name for this client.
    /// </summary>
    public string ClientClassName { get; set; } = string.Empty;
    /// <summary>
    /// The main namespace for this client.
    /// </summary>
    public string ClientNamespaceName { get; set; } = string.Empty;
    /// <summary>
    /// The language for this client.
    /// </summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Whether the backing store was used for this client.
    /// </summary>
    public bool UsesBackingStore
    {
        get; set;
    }
    /// <summary>
    /// Whether backward compatible code was excluded for this client.
    /// </summary>
    public bool ExcludeBackwardCompatible
    {
        get; set;
    }
    /// <summary>
    /// Whether additional data was used for this client.
    /// </summary>
    public bool IncludeAdditionalData
    {
        get; set;
    }
    /// <summary>
    /// Whether SSL Validation was disabled for this client.
    /// </summary>
    public bool DisableSSLValidation
    {
        get; set;
    }
#pragma warning disable CA2227
    /// <summary>
    /// The serializers used for this client.
    /// </summary>
    public HashSet<string> Serializers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The deserializers used for this client.
    /// </summary>
    public HashSet<string> Deserializers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The structured mime types used for this client.
    /// </summary>
#pragma warning disable CA1002
    public List<string> StructuredMimeTypes { get; set; } = new();
#pragma warning restore CA1002
    /// <summary>
    /// The path patterns for API endpoints to include for this client.
    /// </summary>
    public HashSet<string> IncludePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The path patterns for API endpoints to exclude for this client.
    /// </summary>
    public HashSet<string> ExcludePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The OpenAPI validation rules to disable during the generation.
    /// </summary>
    public HashSet<string> DisabledValidationRules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227
    /// <summary>
    /// Updates the passed configuration with the values from the lock file.
    /// </summary>
    /// <param name="config">The configuration to update.</param>
    public void UpdateGenerationConfigurationFromLock(GenerationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.ClientClassName = ClientClassName;
        config.ClientNamespaceName = ClientNamespaceName;
        if (Enum.TryParse<GenerationLanguage>(Language, out var parsedLanguage))
            config.Language = parsedLanguage;
        config.UsesBackingStore = UsesBackingStore;
        config.ExcludeBackwardCompatible = ExcludeBackwardCompatible;
        config.IncludeAdditionalData = IncludeAdditionalData;
        config.Serializers = Serializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.Deserializers = Deserializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.StructuredMimeTypes = new(StructuredMimeTypes);
        config.IncludePatterns = IncludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.ExcludePatterns = ExcludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.OpenAPIFilePath = DescriptionLocation;
        config.DisabledValidationRules = DisabledValidationRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.DisableSSLValidation = DisableSSLValidation;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="KiotaLock"/> class.
    /// </summary>
    public KiotaLock()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="KiotaLock"/> class from the passed configuration.
    /// </summary>
    /// <param name="config">The configuration to use.</param>
    public KiotaLock(GenerationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Language = config.Language.ToString();
        ClientClassName = config.ClientClassName;
        ClientNamespaceName = config.ClientNamespaceName;
        UsesBackingStore = config.UsesBackingStore;
        ExcludeBackwardCompatible = config.ExcludeBackwardCompatible;
        IncludeAdditionalData = config.IncludeAdditionalData;
        Serializers = config.Serializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Deserializers = config.Deserializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        StructuredMimeTypes = config.StructuredMimeTypes.ToList();
        IncludePatterns = config.IncludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        ExcludePatterns = config.ExcludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        DescriptionLocation = config.OpenAPIFilePath;
        DisabledValidationRules = config.DisabledValidationRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        DisableSSLValidation = config.DisableSSLValidation;
    }
}
