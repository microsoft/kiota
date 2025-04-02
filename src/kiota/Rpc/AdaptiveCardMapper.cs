﻿using Kiota.Builder.OpenApiExtensions;
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
                if (adaptiveCard.DataPath is not null && adaptiveCard.File is not null)
                {
                    return new AdaptiveCardInfo(adaptiveCard.DataPath, adaptiveCard.File);
                }
            }
            return null;
        }
    }
}
