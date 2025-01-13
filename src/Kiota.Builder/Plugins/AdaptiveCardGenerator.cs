using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Plugins
{
    public class AdaptiveCardGenerator
    {
        public AdaptiveCardGenerator() { }

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
            
            var builder = new StringBuilder();
            builder.Append("{\"type\":\"AdaptiveCard\",\"$schema\":\"https://adaptivecards.io/schemas/adaptive-card.json\",\"version\":\"1.5\",\"body\":[");
            foreach (var property in properties)
            {
                builder.Append("{\"type\":\"TextBlock\",\"text\":\"" + property.Key + ": ${if(" + property.Key + ", " + property.Key + ", 'N/A')}\"");
                builder.Append(",\"wrap\":true}");
                builder.Append(',');
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append("]}");
            return builder.ToString();
        }
    }
}
