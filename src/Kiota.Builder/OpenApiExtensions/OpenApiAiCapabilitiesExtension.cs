using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiAiCapabilitiesExtension : IOpenApiExtension
{
    public static string Name => "x-ai-capabilities";
    public ExtensionConfirmation? Confirmation
    {
        get; set;
    }
    public ExtensionResponseSemantics? ResponseSemantics
    {
        get; set;
    }
    public ExtensionSecurityInfo? SecurityInfo
    {
        get; set;
    }

    public static OpenApiAiCapabilitiesExtension Parse(JsonNode source)
    {
        if (source is not JsonObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiAiCapabilitiesExtension();
        if (rawObject.TryGetPropertyValue(nameof(Confirmation).ToFirstCharacterLowerCase(), out var confirmation) && confirmation is JsonObject confirmationObject)
        {
            extension.Confirmation = ParseConfirmation(confirmationObject);
        }
        if (rawObject.TryGetPropertyValue(nameof(ResponseSemantics).ToFirstCharacterLowerCase().ToSnakeCase(), out var responseSemantics) && responseSemantics is JsonObject responseSemanticsObject)
        {
            extension.ResponseSemantics = ParseResponseSemantics(responseSemanticsObject);
        }
        if (rawObject.TryGetPropertyValue(nameof(SecurityInfo).ToFirstCharacterLowerCase().ToSnakeCase(), out var securityInfo) && securityInfo is JsonObject securityInfoObject)
        {
            extension.SecurityInfo = ParseSecurityInfo(securityInfoObject);
        }
        return extension;
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        if (ResponseSemantics != null || Confirmation != null || SecurityInfo != null)
        {

            if (ResponseSemantics is not null)
            {
                writer.WritePropertyName(nameof(ResponseSemantics).ToFirstCharacterLowerCase().ToSnakeCase());
                WriteResponseSemantics(writer, ResponseSemantics);
            }
            if (Confirmation is not null)
            {
                writer.WritePropertyName(nameof(Confirmation).ToFirstCharacterLowerCase());
                WriteConfirmation(writer, Confirmation);
            }
            if (SecurityInfo is not null)
            {
                writer.WritePropertyName(nameof(SecurityInfo).ToFirstCharacterLowerCase().ToSnakeCase());
                WriteSecurityInfo(writer, SecurityInfo);
            }
        }
        writer.WriteEndObject();
    }

    private static ExtensionConfirmation ParseConfirmation(JsonObject source)
    {
        var confirmation = new ExtensionConfirmation();
        if (source.TryGetPropertyValue(nameof(Confirmation.Type).ToFirstCharacterLowerCase(), out var type) &&
            type is JsonValue typeValue && typeValue.GetValueKind() is JsonValueKind.String &&
            typeValue.TryGetValue<string>(out var typeStrValue))
        {
            confirmation.Type = typeStrValue;
        }

        if (source.TryGetPropertyValue(nameof(Confirmation.Title).ToFirstCharacterLowerCase(), out var title) &&
            title is JsonValue titleValue && titleValue.GetValueKind() is JsonValueKind.String &&
            titleValue.TryGetValue<string>(out var titleStrValue))
        {
            confirmation.Title = titleStrValue;
        }

        if (source.TryGetPropertyValue(nameof(Confirmation.Body).ToFirstCharacterLowerCase(), out var body) &&
            body is JsonValue bodyValue && bodyValue.GetValueKind() is JsonValueKind.String &&
            bodyValue.TryGetValue<string>(out var bodyStrValue))
        {
            confirmation.Body = bodyStrValue;
        }

        return confirmation;
    }

    private void WriteConfirmation(IOpenApiWriter writer, ExtensionConfirmation confirmation)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(confirmation.Type))
        {
            writer.WritePropertyName(nameof(Confirmation.Type).ToFirstCharacterLowerCase());
            writer.WriteValue(confirmation.Type);
        }

        if (!string.IsNullOrEmpty(confirmation.Title))
        {
            writer.WritePropertyName(nameof(Confirmation.Title).ToFirstCharacterLowerCase());
            writer.WriteValue(confirmation.Title);
        }

        if (!string.IsNullOrEmpty(confirmation.Body))
        {
            writer.WritePropertyName(nameof(Confirmation.Body).ToFirstCharacterLowerCase());
            writer.WriteValue(confirmation.Body);
        }

        writer.WriteEndObject();
    }

    private static ExtensionResponseSemantics ParseResponseSemantics(JsonObject source)
    {
        var responseSemantics = new ExtensionResponseSemantics();

        if (source.TryGetPropertyValue(nameof(ResponseSemantics.DataPath).ToFirstCharacterLowerCase().ToSnakeCase(), out var dataPath) &&
            dataPath is JsonValue dataPathValue && dataPathValue.GetValueKind() is JsonValueKind.String &&
            dataPathValue.TryGetValue<string>(out var dataPathStrValue))
        {
            responseSemantics.DataPath = dataPathStrValue;
        }
        if (source.TryGetPropertyValue(nameof(ResponseSemantics.StaticTemplate).ToFirstCharacterLowerCase().ToSnakeCase(), out var staticTemplate) &&
            staticTemplate is JsonObject staticTemplateObj)
        {
            responseSemantics.StaticTemplate = staticTemplateObj;
        }

        if (source.TryGetPropertyValue(nameof(ResponseSemantics.Properties).ToFirstCharacterLowerCase(), out var properties) &&
            properties is JsonObject propertiesObj)
        {
            responseSemantics.Properties = ParseResponseSemanticsProperties(propertiesObj);
        }

        if (source.TryGetPropertyValue(nameof(ResponseSemantics.OauthCardPath).ToFirstCharacterLowerCase().ToSnakeCase(), out var OauthCardPath) &&
            OauthCardPath is JsonValue OauthCardPathValue && OauthCardPathValue.GetValueKind() is JsonValueKind.String &&
            OauthCardPathValue.TryGetValue<string>(out var OauthCardPathStrValue))
        {
            responseSemantics.OauthCardPath = OauthCardPathStrValue;
        }

        return responseSemantics;
    }

    private void WriteResponseSemantics(IOpenApiWriter writer, ExtensionResponseSemantics responseSemantics)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(ResponseSemantics.DataPath).ToFirstCharacterLowerCase().ToSnakeCase());
        writer.WriteValue(responseSemantics.DataPath);

        if (responseSemantics.StaticTemplate != null && responseSemantics.StaticTemplate is JsonObject staticTemplateObj)
        {
            writer.WritePropertyName(nameof(ResponseSemantics.StaticTemplate).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteRaw(staticTemplateObj.ToJsonString());
        }

        if (responseSemantics.Properties != null)
        {
            writer.WritePropertyName(nameof(ResponseSemantics.Properties).ToFirstCharacterLowerCase());
            WriteResponseSemanticsProperties(writer, responseSemantics.Properties);
        }

        if (!string.IsNullOrEmpty(responseSemantics.OauthCardPath))
        {
            writer.WritePropertyName(nameof(ResponseSemantics.OauthCardPath).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteValue(responseSemantics.OauthCardPath);
        }

        writer.WriteEndObject();
    }

    private static ExtensionResponseSemanticsProperties ParseResponseSemanticsProperties(JsonObject source)
    {
        var properties = new ExtensionResponseSemanticsProperties();

        if (source.TryGetPropertyValue(nameof(ExtensionResponseSemanticsProperties.Title).ToFirstCharacterLowerCase(), out var title) &&
            title is JsonValue titleValue && titleValue.GetValueKind() is JsonValueKind.String &&
            titleValue.TryGetValue<string>(out var titleStrValue))
        {
            properties.Title = titleStrValue;
        }

        if (source.TryGetPropertyValue(nameof(ExtensionResponseSemanticsProperties.Subtitle).ToFirstCharacterLowerCase(), out var subtitle) &&
            subtitle is JsonValue subtitleValue && subtitleValue.GetValueKind() is JsonValueKind.String &&
            subtitleValue.TryGetValue<string>(out var subtitleStrValue))
        {
            properties.Subtitle = subtitleStrValue;
        }

        if (source.TryGetPropertyValue(nameof(ExtensionResponseSemanticsProperties.Url).ToFirstCharacterLowerCase(), out var url) &&
            url is JsonValue urlValue && urlValue.GetValueKind() is JsonValueKind.String &&
            urlValue.TryGetValue<string>(out var urlStrValue))
        {
            properties.Url = urlStrValue;
        }

        if (source.TryGetPropertyValue(nameof(ExtensionResponseSemanticsProperties.ThumbnailUrl).ToFirstCharacterLowerCase().ToSnakeCase(), out var thumbnailUrl) &&
            thumbnailUrl is JsonValue thumbnailUrlValue && thumbnailUrlValue.GetValueKind() is JsonValueKind.String &&
            thumbnailUrlValue.TryGetValue<string>(out var thumbnailUrlStrValue))
        {
            properties.ThumbnailUrl = thumbnailUrlStrValue;
        }

        if (source.TryGetPropertyValue(nameof(ExtensionResponseSemanticsProperties.InformationProtectionLabel).ToFirstCharacterLowerCase().ToSnakeCase(), out var informationProtectionLabel) &&
            informationProtectionLabel is JsonValue informationProtectionLabelValue && informationProtectionLabelValue.GetValueKind() is JsonValueKind.String &&
            informationProtectionLabelValue.TryGetValue<string>(out var informationProtectionLabelStrValue))
        {
            properties.InformationProtectionLabel = informationProtectionLabelStrValue;
        }

        if (source.TryGetPropertyValue(nameof(ExtensionResponseSemanticsProperties.TemplateSelector).ToFirstCharacterLowerCase().ToSnakeCase(), out var templateSelector) &&
            templateSelector is JsonValue templateSelectorValue && templateSelectorValue.GetValueKind() is JsonValueKind.String &&
            templateSelectorValue.TryGetValue<string>(out var templateSelectorStrValue))
        {
            properties.TemplateSelector = templateSelectorStrValue;
        }

        return properties;
    }

    private void WriteResponseSemanticsProperties(IOpenApiWriter writer, ExtensionResponseSemanticsProperties properties)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(properties.Title))
        {
            writer.WritePropertyName(nameof(ExtensionResponseSemanticsProperties.Title).ToFirstCharacterLowerCase());
            writer.WriteValue(properties.Title);
        }

        if (!string.IsNullOrEmpty(properties.Subtitle))
        {
            writer.WritePropertyName(nameof(ExtensionResponseSemanticsProperties.Subtitle).ToFirstCharacterLowerCase());
            writer.WriteValue(properties.Subtitle);
        }

        if (!string.IsNullOrEmpty(properties.Url))
        {
            writer.WritePropertyName(nameof(ExtensionResponseSemanticsProperties.Url).ToFirstCharacterLowerCase());
            writer.WriteValue(properties.Url);
        }

        if (!string.IsNullOrEmpty(properties.ThumbnailUrl))
        {
            writer.WritePropertyName(nameof(ExtensionResponseSemanticsProperties.ThumbnailUrl).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteValue(properties.ThumbnailUrl);
        }

        if (!string.IsNullOrEmpty(properties.InformationProtectionLabel))
        {
            writer.WritePropertyName(nameof(ExtensionResponseSemanticsProperties.InformationProtectionLabel).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteValue(properties.InformationProtectionLabel);
        }

        if (!string.IsNullOrEmpty(properties.TemplateSelector))
        {
            writer.WritePropertyName(nameof(ExtensionResponseSemanticsProperties.TemplateSelector).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteValue(properties.TemplateSelector);
        }

        writer.WriteEndObject();
    }

    private static ExtensionSecurityInfo ParseSecurityInfo(JsonObject source)
    {
        var securityInfo = new ExtensionSecurityInfo();

        if (source.TryGetPropertyValue(nameof(ExtensionSecurityInfo.DataHandling).ToFirstCharacterLowerCase().ToSnakeCase(), out var dataHandling) &&
            dataHandling is JsonArray dataHandlingArray)
        {
            var dataHandlingList = new List<string>();

            foreach (var item in dataHandlingArray)
            {
                if (item is JsonValue valueItem && valueItem.GetValueKind() is JsonValueKind.String &&
                    valueItem.TryGetValue<string>(out var strValue))
                {
                    dataHandlingList.Add(strValue);
                }
            }

            securityInfo.DataHandling = dataHandlingList;
        }

        return securityInfo;
    }

    private void WriteSecurityInfo(IOpenApiWriter writer, ExtensionSecurityInfo securityInfo)
    {
        writer.WriteStartObject();

        if (securityInfo.DataHandling != null && securityInfo.DataHandling.Count > 0)
        {
            writer.WritePropertyName(nameof(securityInfo.DataHandling).ToFirstCharacterLowerCase().ToSnakeCase());
            writer.WriteStartArray();

            foreach (var item in securityInfo.DataHandling)
            {
                writer.WriteValue(item);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}


public class ExtensionConfirmation
{
    public string? Type
    {
        get; set;
    }
    public string? Title
    {
        get; set;
    }
    public string? Body
    {
        get; set;
    }
}

public class ExtensionResponseSemantics
{
    public string DataPath { get; set; } = string.Empty;
    public object? StaticTemplate
    {
        get; set;
    }
    public ExtensionResponseSemanticsProperties? Properties
    {
        get; set;
    }
    public string? OauthCardPath
    {
        get; set;
    }
}

public class ExtensionResponseSemanticsProperties
{
    public string? Title
    {
        get; set;
    }
    public string? Subtitle
    {
        get; set;
    }
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string? Url
    {
        get; set;
    }
    public string? ThumbnailUrl
    {
        get; set;
    }
#pragma warning restore CA1056 // URI-like properties should not be strings
    public string? InformationProtectionLabel
    {
        get; set;
    }
    public string? TemplateSelector
    {
        get; set;
    }
}

public class ExtensionSecurityInfo
{
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
    public List<string> DataHandling { get; set; } = new List<string>();
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists
}


