using System.Text.Json.Nodes;
using System.Text.Json;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi.Interfaces;

namespace kiota.Rpc
{
    internal class AdaptiveCardMapper
    {
        internal static AdaptiveCardInfo? FromExtensions(IDictionary<string, IOpenApiExtension>? extensions)
        {
            if (extensions is not null &&
                extensions.TryGetValue(OpenApiAiAdaptiveCardExtension.Name, out var adaptiveCardExtension) && adaptiveCardExtension is OpenApiAiAdaptiveCardExtension adaptiveCard)
            {
                JsonNode node = new JsonObject();
                node["file"] = JsonValue.Create(adaptiveCard.File);
                using JsonDocument doc = JsonDocument.Parse(node.ToJsonString());
                JsonElement staticTemplate = doc.RootElement.Clone();
                return new AdaptiveCardInfo(adaptiveCard.DataPath!, staticTemplate);
            }
            return null;

        }
    }
}
