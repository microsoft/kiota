using Microsoft.OpenApi.Any;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiDescriptionForModelExtension : OpenApiSimpleStringExtension
{
    public static string Name => "x-ai-description";
    public string? Description
    {
        get; set;
    }
    protected override string? ValueSelector => Description;
    public static OpenApiDescriptionForModelExtension Parse(IOpenApiAny source)
    {
        return new OpenApiDescriptionForModelExtension
        {
            Description = ParseString(source)
        };
    }
}
