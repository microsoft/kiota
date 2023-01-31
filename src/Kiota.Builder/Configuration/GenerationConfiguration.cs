﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Configuration;
public class GenerationConfiguration : ICloneable
{
    public string OpenAPIFilePath { get; set; } = "openapi.yaml";
    public string OutputPath { get; set; } = "./output";
    public string ClientClassName { get; set; } = "ApiClient";
    public string ClientNamespaceName { get; set; } = "ApiSdk";
    public string NamespaceNameSeparator { get; set; } = ".";
    public string ModelsNamespaceName
    {
        get => $"{ClientNamespaceName}{NamespaceNameSeparator}models";
    }
    public GenerationLanguage Language { get; set; } = GenerationLanguage.CSharp;
    public string? ApiRootUrl
    {
        get; set;
    }
    public bool UsesBackingStore
    {
        get; set;
    }
    public bool IncludeAdditionalData { get; set; } = true;
    public HashSet<string> Serializers
    {
        get; set;
    } = new(2, StringComparer.OrdinalIgnoreCase){
        "Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory",
        "Microsoft.Kiota.Serialization.Text.TextSerializationWriterFactory",
        "Microsoft.Kiota.Serialization.Form.FormSerializationWriterFactory",
    };
    public HashSet<string> Deserializers
    {
        get; set;
    } = new(2, StringComparer.OrdinalIgnoreCase) {
        "Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory",
        "Microsoft.Kiota.Serialization.Text.TextParseNodeFactory",
        "Microsoft.Kiota.Serialization.Form.FormParseNodeFactory",
    };
    public bool ShouldWriteNamespaceIndices
    {
        get
        {
            return BarreledLanguages.Contains(Language);
        }
    }
    public bool ShouldWriteBarrelsIfClassExists
    {
        get
        {
            return BarreledLanguagesWithConstantFileName.Contains(Language);
        }
    }
    public bool ShouldRenderMethodsOutsideOfClasses
    {
        get
        {
            return MethodOutsideOfClassesLanguages.Contains(Language);
        }
    }
    private static readonly HashSet<GenerationLanguage> MethodOutsideOfClassesLanguages = new(1) {
        GenerationLanguage.Go,
    };
    private static readonly HashSet<GenerationLanguage> BarreledLanguages = new(3) {
        GenerationLanguage.Ruby,
        GenerationLanguage.TypeScript,
        GenerationLanguage.Swift,
    };
    private static readonly HashSet<GenerationLanguage> BarreledLanguagesWithConstantFileName = new(1) {
        GenerationLanguage.TypeScript
    };
    public bool CleanOutput
    {
        get; set;
    }
    public HashSet<string> StructuredMimeTypes
    {
        get; set;
    } = new(5, StringComparer.OrdinalIgnoreCase) {
        "application/json",
        "text/plain",
        "application/x-www-form-urlencoded",
    };
    public HashSet<string> IncludePatterns { get; set; } = new(0, StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExcludePatterns { get; set; } = new(0, StringComparer.OrdinalIgnoreCase);
    public bool ClearCache
    {
        get; set;
    }
    public HashSet<string> DisabledValidationRules { get; set; } = new(0, StringComparer.OrdinalIgnoreCase);
    public object Clone()
    {
        return new GenerationConfiguration
        {
            OpenAPIFilePath = OpenAPIFilePath,
            OutputPath = OutputPath,
            ClientClassName = ClientClassName,
            ClientNamespaceName = ClientNamespaceName,
            NamespaceNameSeparator = NamespaceNameSeparator,
            Language = Language,
            ApiRootUrl = ApiRootUrl,
            UsesBackingStore = UsesBackingStore,
            IncludeAdditionalData = IncludeAdditionalData,
            Serializers = new(Serializers ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            Deserializers = new(Deserializers ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            CleanOutput = CleanOutput,
            StructuredMimeTypes = new(StructuredMimeTypes ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            IncludePatterns = new(IncludePatterns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            ExcludePatterns = new(ExcludePatterns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            ClearCache = ClearCache,
            DisabledValidationRules = new(DisabledValidationRules ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
        };
    }
}
