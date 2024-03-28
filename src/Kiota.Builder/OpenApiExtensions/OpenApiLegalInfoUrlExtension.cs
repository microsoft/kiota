using Microsoft.OpenApi.Any;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiLegalInfoUrlExtension : OpenApiSimpleStringExtension
{
    public static string Name => "x-legal-info-url";
    public string? Legal
    {
        get; set;
    }
    protected override string? ValueSelector => Legal;
    public static OpenApiLegalInfoUrlExtension Parse(IOpenApiAny source)
    {
        return new OpenApiLegalInfoUrlExtension
        {
            Legal = ParseString(source)
        };
    }
}
