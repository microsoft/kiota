using Kiota.Builder.CodeDOM;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Extensions;
internal static class OpenApiDeprecationExtensionExtensions
{
    internal static DeprecationInformation ToDeprecationInformation(this OpenApiDeprecationExtension value)
    {
        return new DeprecationInformation(value.Description.CleanupDescription().CleanupXMLString(), value.Date, value.RemovalDate, value.Version.CleanupDescription().CleanupXMLString(), true);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiSchema schema)
    {
        if (schema.Deprecated && schema.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
            return deprecatedValue.ToDeprecationInformation();
        return new(null, null, null, null, schema.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiParameter parameter)
    {
        if (parameter.Deprecated && parameter.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
            return deprecatedValue.ToDeprecationInformation();
        else if (parameter.Schema != null && !parameter.Schema.IsReferencedSchema() && parameter.Schema.Deprecated)
            return parameter.Schema.GetDeprecationInformation();
        else
        {
            foreach (var content in parameter.Content.Values)
            {
                if (content.Schema != null && !content.Schema.IsReferencedSchema() && content.Schema.Deprecated)
                {
                    var deprecationInformation = content.Schema.GetDeprecationInformation();
                    if (deprecationInformation.IsDeprecated)
                    {
                        return deprecationInformation;
                    }
                }
            }
        }
        return new(null, null, null, null, parameter.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiOperation operation)
    {
        if (operation.Deprecated && operation.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
            return deprecatedValue.ToDeprecationInformation();
        else
        {
            foreach (var response in operation.Responses.Values)
            {
                foreach (var content in response.Content.Values)
                {
                    if (content?.Schema is OpenApiSchema schema && !schema.IsReferencedSchema())
                    {
                        var deprecationInformation = schema.GetDeprecationInformation();
                        if (deprecationInformation.IsDeprecated)
                        {
                            return deprecationInformation;
                        }
                    }
                }
            }

            if (operation.RequestBody != null)
            {
                foreach (var content in operation.RequestBody.Content.Values)
                {
                    if (content?.Schema is OpenApiSchema schema && !schema.IsReferencedSchema())
                    {
                        var deprecationInformation = schema.GetDeprecationInformation();
                        if (deprecationInformation.IsDeprecated)
                        {
                            return deprecationInformation;
                        }
                    }
                }
            }
        }
        return new(null, null, null, null, operation.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiUrlTreeNode treeNode)
    {
        var operations = treeNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem) ? pathItem.Operations.Values : [];
        bool allDeprecated = true;
        foreach (var operation in operations)
        {
            if (!operation.Deprecated)
            {
                allDeprecated = false;
                break;
            }
        }

        if (allDeprecated)
        {
            foreach (var operation in operations)
            {
                var deprecationInformation = operation.GetDeprecationInformation();
                if (deprecationInformation.IsDeprecated)
                {
                    return deprecationInformation;
                }
            }
        }

        return new(null, null, null, null, false);
    }
}
