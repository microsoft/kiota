using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi.Readers;

namespace Kiota.Builder.Extensions;
public static class OpenApiSettingsExtensions
{
    /// <summary>
    /// Adds the OpenAPI extensions used for plugins generation.
    /// </summary>
    public static void AddPluginsExtensions(this OpenApiReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.ExtensionParsers.TryAdd(OpenApiLogoExtension.Name, static (i, _) => OpenApiLogoExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiDescriptionForModelExtension.Name, static (i, _) => OpenApiDescriptionForModelExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiPrivacyPolicyUrlExtension.Name, static (i, _) => OpenApiPrivacyPolicyUrlExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiLegalInfoUrlExtension.Name, static (i, _) => OpenApiLegalInfoUrlExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiAiReasoningInstructionsExtension.Name, static (i, _) => OpenApiAiReasoningInstructionsExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiAiRespondingInstructionsExtension.Name, static (i, _) => OpenApiAiRespondingInstructionsExtension.Parse(i));
    }

    public static void AddGenerationExtensions(this OpenApiReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.AddMicrosoftExtensionParsers();
        settings.ExtensionParsers.TryAdd(OpenApiKiotaExtension.Name, static (i, _) => OpenApiKiotaExtension.Parse(i));
    }

    public static HashSet<string> KiotaSupportedExtensions()
    {
        var dummySettings = new OpenApiReaderSettings();
        dummySettings.AddGenerationExtensions();
        dummySettings.AddPluginsExtensions();

        var supportedExtensions = dummySettings.ExtensionParsers.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        supportedExtensions.Add("x-openai-isConsequential");// add extension we don't parse to the list 

        return supportedExtensions;
    }
}
