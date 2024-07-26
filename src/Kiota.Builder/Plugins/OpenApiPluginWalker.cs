using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Plugins;

public class OpenApiPluginWalker : OpenApiVisitorBase
{
    private static readonly HashSet<string> SupportedExtensions = OpenApiSettingsExtensions.KiotaSupportedExtensions();

    /// <summary>
    /// Visits <see cref="OpenApiSchema"/>
    /// </summary>
    public override void Visit(IOpenApiExtensible openApiExtensible)
    {
        ArgumentNullException.ThrowIfNull(openApiExtensible);
        // remove any extensions we do not support
        foreach (var extension in openApiExtensible.Extensions.Where(static extension => !SupportedExtensions.Contains(extension.Key)))
        {
            openApiExtensible.Extensions.Remove(extension.Key);
        }
    }
    /// <summary>
    /// Visits the operations.
    /// </summary>
    public override void Visit(IDictionary<OperationType, OpenApiOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);

        // Cleanup responses for the operation
        foreach (var operation in operations.Values)
        {
            var responseDescription = operation.Responses.Values.Select(static response => response.Description)
                .FirstOrDefault(static desc => !string.IsNullOrEmpty(desc)) ?? "Api Response";

            operation.Responses = new OpenApiResponses()
            {
                {
                    "2XX",new OpenApiResponse
                    {
                        Description = responseDescription,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "text/plain", new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string"
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
