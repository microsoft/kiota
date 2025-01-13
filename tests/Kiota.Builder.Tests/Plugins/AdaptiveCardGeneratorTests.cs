using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kiota.Builder.Plugins;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Kiota.Builder.Tests.Plugins
{
    public sealed class AdaptiveCardGeneratorTests
    {
        [Fact]
        public void GenerateAdaptiveCardFromOperation()
        {
            var sample = new OpenApiOperation
            {
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "OK",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        ["name"] = new OpenApiSchema
                                        {
                                            Type = "string"
                                        },
                                        ["age"] = new OpenApiSchema
                                        {
                                            Type = "number"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            string expected = "{\r\n  \"type\": \"AdaptiveCard\",\r\n  \"version\": \"1.5\",\r\n  \"body\": [\r\n    {\r\n      \"type\": \"TextBlock\",\r\n      \"text\": \"${name, name, 'N/A'}\"\r\n    },\r\n    {\r\n      \"type\": \"TextBlock\",\r\n      \"text\": \"${age, age, 'N/A'}\"\r\n    }\r\n  ]\r\n}";

            var generator = new AdaptiveCardGenerator();
            var card = generator.GenerateAdaptiveCard(sample);
           
            var expectedJson = JsonDocument.Parse(expected).RootElement.GetRawText();
            var actualJson = JsonDocument.Parse(card).RootElement.GetRawText();

            Assert.Equal(expectedJson, actualJson);
        }
    }
}
