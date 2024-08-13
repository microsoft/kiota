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
    /// Visits the OpenAPI response.
    /// </summary>
    public override void Visit(OpenApiResponses response)
    {
        ArgumentNullException.ThrowIfNull(response);

        // Ensure description strings are not empty strings.
        foreach (var responseItem in response.Where(static res => string.IsNullOrEmpty(res.Value.Description)))
        {
            responseItem.Value.Description = "Api Response";
        }
    }
}
