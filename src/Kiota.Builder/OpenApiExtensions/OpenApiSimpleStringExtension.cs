using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.OpenApiExtensions;

public abstract class OpenApiSimpleStringExtension : IOpenApiExtension
{
    protected abstract string? ValueSelector
    {
        get;
    }
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!string.IsNullOrWhiteSpace(ValueSelector))
        {
            writer.WriteValue(ValueSelector);
        }
    }
    public static string ParseString(JsonNode source)
    {
        if (source is not JsonValue rawString ||
            rawString.GetValueKind() is not JsonValueKind.String) throw new ArgumentOutOfRangeException(nameof(source));
        return rawString.GetValue<string>();
    }
}
