using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi;

namespace kiota.Rpc
{
    internal class AdaptiveCardMapper
    {
        internal static AdaptiveCardInfo? FromExtensions(IDictionary<string, IOpenApiExtension>? extensions)
        {
            if (extensions is not null &&
                extensions.TryGetValue(OpenApiAiAdaptiveCardExtension.Name, out var adaptiveCardExtension) && adaptiveCardExtension is OpenApiAiAdaptiveCardExtension adaptiveCard)
            {
                if (adaptiveCard.DataPath is not null && adaptiveCard.File is not null && adaptiveCard.Title is not null)
                {
                    return new AdaptiveCardInfo(adaptiveCard.DataPath, adaptiveCard.File, adaptiveCard.Title, adaptiveCard.Url, adaptiveCard.Subtitle, adaptiveCard.ThumbnailUrl, adaptiveCard.InformationProtectionLabel);
                }
            }
            return null;
        }
    }
}
