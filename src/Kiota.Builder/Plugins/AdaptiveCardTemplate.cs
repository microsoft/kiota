﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Plugins
{

    internal class AdaptiveCardTemplate
    {
        private readonly ILogger<KiotaBuilder> Logger;
        private string AdaptiveCard => @"
            {
                ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
                ""type"": ""AdaptiveCard"",
                ""version"": ""1.0"",
                ""body"": [
                    {
                        ""type"": ""Container"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""This is your adaptive card template"",
                                ""weight"": ""bolder"",
                                ""size"": ""medium""
                            },
                            {
                                ""type"": ""ColumnSet"",
                                ""columns"": [
                                    {
                                        ""type"": ""Column"",
                                        ""width"": ""auto"",
                                        ""items"": [
                                            {
                                                ""type"": ""Image"",
                                                ""url"": ""https://github.com/microsoft/kiota/blob/main/vscode/microsoft-kiota/images/logo.png?raw=true"",
                                                ""altText"": ""Kiota logo"",
                                                ""size"": ""medium"",
                                                ""style"": ""person""
                                            }
                                        ]
                                    },
                                    {
                                        ""type"": ""Column"",
                                        ""width"": ""auto"",
                                        ""items"": [
                                            {
                                                ""type"": ""TextBlock"",
                                                ""text"": ""Adaptive Card"",
                                                ""weight"": ""bolder"",
                                                ""wrap"": true
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    },
                    {
                        ""type"": ""Container"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Now that we have defined the adaptive card template, one can go to the plugin manifest file and edit it to create a card that displays the relevant information for their users."",
                                ""wrap"": true
                            }
                        ]
                    },
                    {
                        ""type"": ""ColumnSet"",
                        ""columns"": [
                            {
                                ""type"": ""Column"",
                                ""width"": ""auto"",
                                ""items"": [
                                    {
                                        ""type"": ""TextBlock"",
                                        ""horizontalAlignment"": ""center"",
                                        ""text"": ""Learn about [Adaptive Cards](https://adaptivecards.io/)"",
                                        ""wrap"": true
                                    }
                                ]
                            },
                            {
                                ""type"": ""Column"",
                                ""width"": ""auto"",
                                ""items"": [
                                    {
                                        ""type"": ""TextBlock"",
                                        ""horizontalAlignment"": ""center"",
                                        ""text"": ""Learn about [API Plugin for Microsoft 365 Copilot](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-api-plugins)"",
                                        ""wrap"": true
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }";

        public AdaptiveCardTemplate(ILogger<KiotaBuilder> logger)
        {
            Logger = logger;
        }

        public void Write(string filePath)
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllText(filePath, AdaptiveCard);
            }
            catch (IOException e)
            {
                Logger.LogCritical("Failed to add adaptive-card.json due to an IO error: {Message}", e.Message);
            }
        }
    }
}
