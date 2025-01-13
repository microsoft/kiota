using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var expected = @"{""type"":""AdaptiveCard"",""$schema"":""https://adaptivecards.io/schemas/adaptive-card.json"",""version"":""1.5"",""body"":[{""type"":""TextBlock"",""text"":""name: ${if(name, name, 'N/A')}"",""wrap"":true},{""type"":""TextBlock"",""text"":""age: ${if(age, age, 'N/A')}"",""wrap"":true}]}";

            var generator = new AdaptiveCardGenerator();
            var card = generator.GenerateAdaptiveCard(sample);
            Assert.Equal(expected, card);
        }
    }
}
