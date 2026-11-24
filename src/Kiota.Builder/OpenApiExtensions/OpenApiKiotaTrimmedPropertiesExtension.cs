using System;
using Microsoft.OpenApi;

namespace Kiota.Builder.OpenApiExtensions;

internal sealed class OpenApiKiotaTrimmedPropertiesExtension : IOpenApiExtension
{
    internal const string Name = "x-kiota-trimmed-properties";
    internal static readonly OpenApiKiotaTrimmedPropertiesExtension Instance = new();

    private OpenApiKiotaTrimmedPropertiesExtension()
    {
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteValue(true);
    }
}
