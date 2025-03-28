using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Interfaces;
namespace Kiota.Builder.OpenApiExtensions;

using Microsoft.DeclarativeAgents.Manifest;
using Microsoft.OpenApi.Writers;

public class OpenApiAiCapabilitiesExtension : IOpenApiExtension
{
    public static string Name => "x-ai-capabilities";
    public object? Confirmation
    {
        get; set;
    }
    public object? ResponseSemantics
    {
        get; set;
    }
    public object? SecurityInfo
    {
        get; set;
    }
    public static OpenApiAiCapabilitiesExtension Parse(JsonNode source)
    {
        if (source is not JsonObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiAiCapabilitiesExtension();
        if (rawObject.TryGetPropertyValue(nameof(ResponseSemantics).ToFirstCharacterLowerCase().ToSnakeCase(), out var responseSemantics) && responseSemantics is JsonObject responseSemanticsObject)
        {
            extension.ResponseSemantics = responseSemanticsObject;
        }
        if (rawObject.TryGetPropertyValue(nameof(Confirmation).ToFirstCharacterLowerCase(), out var confirmation) && confirmation is JsonObject confirmationObject)
        {
            extension.Confirmation = confirmationObject;
        }
        if (rawObject.TryGetPropertyValue(nameof(SecurityInfo).ToFirstCharacterLowerCase().ToSnakeCase(), out var securityInfo) && securityInfo is JsonObject securityInfoObject)
        {
            extension.SecurityInfo = securityInfoObject;
        }
        if (extension.ResponseSemantics is JsonObject responseSemanticsObj)
        {
            bool hasDataPath = responseSemanticsObj.TryGetPropertyValue("data_path", out _);
            bool hasStaticTemplate = responseSemanticsObj.TryGetPropertyValue("static_template", out _);
            bool hasTemplateSelector = responseSemanticsObj.TryGetPropertyValue("properties", out var props) &&
                                    props is JsonObject propsObj &&
                                    propsObj.TryGetPropertyValue("template_selector", out _);

            if (!hasDataPath)
            {
                throw new ArgumentOutOfRangeException(nameof(source),
                    "The property 'data_path' must be set when 'response_semantics' is provided.");
            }
            if (!hasStaticTemplate && !hasTemplateSelector)
            {
                throw new ArgumentOutOfRangeException(nameof(source),
                    "When 'response_semantics' is provided, either 'static_template' or 'properties.template_selector' must be set.");
            }
        }
        return extension;
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (ResponseSemantics != null || Confirmation != null || SecurityInfo != null)
        {
            writer.WriteStartObject();
            if (ResponseSemantics is JsonObject responseSemanticsObj)
            {
                writer.WritePropertyName(nameof(ResponseSemantics).ToFirstCharacterLowerCase().ToSnakeCase());
                writer.WriteValue(responseSemanticsObj.ToString());
            }
            if (Confirmation is JsonObject confirmationObj)
            {
                writer.WritePropertyName(nameof(Confirmation).ToFirstCharacterLowerCase());
                writer.WriteValue(confirmationObj.ToString());
            }
            if (SecurityInfo is JsonObject securityInfoObj)
            {
                writer.WritePropertyName(nameof(SecurityInfo).ToFirstCharacterLowerCase().ToSnakeCase());
                writer.WriteValue(securityInfoObj.ToString());
            }
            writer.WriteEndObject();
        }
    }
}
