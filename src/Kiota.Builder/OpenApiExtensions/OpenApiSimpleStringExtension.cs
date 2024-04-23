using System;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
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
    public static string ParseString(IOpenApiAny source)
    {
        if (source is not OpenApiString rawString) throw new ArgumentOutOfRangeException(nameof(source));
        return rawString.Value;
    }
}
