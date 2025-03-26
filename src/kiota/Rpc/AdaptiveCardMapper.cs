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
                return new AdaptiveCardInfo(adaptiveCard.DataPath!, adaptiveCard.File!);
            }
            return null;
        }
    }
}
