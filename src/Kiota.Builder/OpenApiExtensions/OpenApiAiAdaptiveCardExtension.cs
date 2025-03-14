using System.Text.Json.Nodes;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiAiAdaptiveCardExtension : OpenApiSimpleStringExtension
{
    public static string Name => "x-ai-adaptive-card";
    public string? AdaptiveCard
    {
        get; set;
    }
    protected override string? ValueSelector => AdaptiveCard;
    public static OpenApiAiAdaptiveCardExtension Parse(JsonNode source)
    {
        return new OpenApiAiAdaptiveCardExtension
        {
            AdaptiveCard = ParseString(source)
        };
    }
}
