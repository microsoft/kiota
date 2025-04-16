using System.Text.Json.Nodes;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiAiAuthReferenceIdExtension : OpenApiSimpleStringExtension
{
    public static string Name => "x-ai-auth-reference-id";
    public string? AuthenticationReferenceId
    {
        get; set;
    }
    protected override string? ValueSelector => AuthenticationReferenceId;
    public static OpenApiAiAuthReferenceIdExtension Parse(JsonNode source)
    {
        return new OpenApiAiAuthReferenceIdExtension
        {
            AuthenticationReferenceId = ParseString(source)
        };
    }
}
