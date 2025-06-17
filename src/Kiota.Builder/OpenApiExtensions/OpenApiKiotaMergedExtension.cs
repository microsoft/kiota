
using System;
using Microsoft.OpenApi;

namespace Kiota.Builder.OpenApiExtensions;
/// <summary>
/// Temporary stores the original reference id when we merge schemas so the reference id can be used to lookup the inheritance index and find the discriminator values
/// </summary>
internal class OpenApiKiotaMergedExtension : IOpenApiExtension
{
    public static string Name => "x-kiota-merged";
    public string OriginalName
    {
        get;
    }
    public OpenApiKiotaMergedExtension(string originalName)
    {
        ArgumentException.ThrowIfNullOrEmpty(originalName);
        OriginalName = originalName;
    }
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
    }
}
