using Microsoft.OpenApi.Any;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiLogoExtension : OpenApiSimpleStringExtension
{
    public static string Name => "x-logo";
    public string? Logo
    {
        get; set;
    }
    protected override string? ValueSelector => Logo;
    public static OpenApiLogoExtension Parse(IOpenApiAny source)
    {
        return new OpenApiLogoExtension
        {
            Logo = ParseString(source)
        };
    }
}
