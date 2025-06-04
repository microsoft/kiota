using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Microsoft.OpenApi;
using Microsoft.OpenApi.MicrosoftExtensions;

namespace Kiota.Builder.Extensions;

internal static class OpenApiDeprecationExtensionExtensions
{
    internal static DeprecationInformation ToDeprecationInformation(this OpenApiDeprecationExtension value)
    {
        return new DeprecationInformation(value.Description.CleanupDescription().CleanupXMLString(), value.Date, value.RemovalDate, value.Version.CleanupDescription().CleanupXMLString(), true);
    }
    internal static DeprecationInformation GetDeprecationInformation(this IOpenApiSchema schema)
    {
        if (schema.Deprecated && schema.Extensions is not null && schema.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
            return deprecatedValue.ToDeprecationInformation();
        return new(null, null, null, null, schema.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this IOpenApiParameter parameter)
    {
        if (parameter.Deprecated && parameter.Extensions is not null && parameter.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
            return deprecatedValue.ToDeprecationInformation();
        else if (parameter.Schema != null && !parameter.Schema.IsReferencedSchema() && parameter.Schema.Deprecated)
            return parameter.Schema.GetDeprecationInformation();
        else if (parameter.Content?.Values.Select(static x => x.Schema).Where(static x => x != null && !x.IsReferencedSchema() && x.Deprecated).Select(static x => x!.GetDeprecationInformation()).FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation contentDeprecationInformation)
            return contentDeprecationInformation;
        return new(null, null, null, null, parameter.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiOperation operation)
    {
        if (operation.Deprecated && operation.Extensions is not null && operation.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
            return deprecatedValue.ToDeprecationInformation();
        else if (operation.Responses?.Values
                                .SelectMany(static x => x.Content?.Values.Select(static x => x) ?? [])
                                .Select(static x => x?.Schema)
                                .OfType<OpenApiSchema>()
                                .Select(static x => x.GetDeprecationInformation())
                                .FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation responseDeprecationInformation)
            return responseDeprecationInformation;
        else if (operation.RequestBody?.Content?.Values
                                            .Select(static x => x?.Schema)
                                            .OfType<OpenApiSchema>()
                                            .Select(static x => x.GetDeprecationInformation())
                                            .FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation requestDeprecationInformation)
            return requestDeprecationInformation;
        return new(null, null, null, null, operation.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiUrlTreeNode treeNode)
    {
        var operations = treeNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem) ? (pathItem.Operations?.Values.ToArray() ?? []) : [];
        if (Array.TrueForAll(operations, static x => x.Deprecated) && operations.Select(static x => x.GetDeprecationInformation()).FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation deprecationInformation)
            return deprecationInformation;
        return new(null, null, null, null, false);
    }
}
