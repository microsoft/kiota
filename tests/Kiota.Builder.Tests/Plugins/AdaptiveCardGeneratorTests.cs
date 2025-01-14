using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AdaptiveCards;
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
            var expectedCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5));
            expectedCard.Body.Add(new AdaptiveTextBlock()
            {
                Text = "${name, name, 'N/A'}",
            });
            expectedCard.Body.Add(new AdaptiveTextBlock()
            {
                Text = "${age, age, 'N/A'}",
            });

            var generator = new AdaptiveCardGenerator();
            var actualCard = generator.GenerateAdaptiveCard(sample);
           
            Assert.Equal(expectedCard.Body.Count, actualCard.Body.Count);
        }
    }
}
