using System;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiDescriptionForModelExtension : IOpenApiExtension
{
    public static string Name => "x-ai-description";
    public string? Description
    {
        get; set;
    }
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!string.IsNullOrWhiteSpace(Description))
        {
            writer.WriteValue(Description);
        }
    }
    public static OpenApiDescriptionForModelExtension Parse(IOpenApiAny source)
    {
        if (source is not OpenApiString rawString) throw new ArgumentOutOfRangeException(nameof(source));
        return new OpenApiDescriptionForModelExtension
        {
            Description = rawString.Value
        };
    }
}
