using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Plugins
{
    public class AdaptiveCardGenerator
    {
        public AdaptiveCardGenerator()
        {
        }

        public string GenerateAdaptiveCard(OpenApiOperation operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            var responses = operation.Responses;
            var response = responses["200"];
            ArgumentNullException.ThrowIfNull(response);

            var schema = response.Content["application/json"].Schema;
            ArgumentNullException.ThrowIfNull(schema);

            var properties = schema.Properties;
            ArgumentNullException.ThrowIfNull(properties);

            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5));

            foreach (var property in properties)
            {

                if (property.Value.Type == "string" && property.Value.Format == "uri")
                {
                    card.Body.Add(new AdaptiveImage()
                    {
                        Url = new Uri($"${{{property.Key}}}"),
                        Size = AdaptiveImageSize.Large,
                    });
                }
                else if (property.Value.Type == "array")
                {
                    card.Body.Add(new AdaptiveTextBlock()
                    {
                        Text = $"${{{property.Key}.join(', ')}}",
                    });
                }
                else
                {
                    card.Body.Add(new AdaptiveTextBlock()
                    {
                        Text = $"${{{property.Key}, {property.Key}, 'N/A'}}",
                    });
                }
            }
            return card.ToJson();
        }
    }
}
