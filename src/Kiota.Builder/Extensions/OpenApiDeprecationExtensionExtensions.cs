using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Extensions;
internal static class OpenApiDeprecationExtensionExtensions
{
    internal static DeprecationInformation ToDeprecationInformation(this OpenApiDeprecationExtension value)
    {
        return new DeprecationInformation(value.Description, value.Date, value.RemovalDate, value.Version, true);
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
        else if (parameter.Content.Values.Select(static x => x.Schema).Where(static x => x != null && !x.IsReferencedSchema() && x.Deprecated).Select(static x => x.GetDeprecationInformation()).FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation contentDeprecationInformation)
            return contentDeprecationInformation;
        return new(null, null, null, null, parameter.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiOperation operation)
    {
        if (operation.Deprecated && operation.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
            return deprecatedValue.ToDeprecationInformation();
        else if (operation.Responses.Values
                                .SelectMany(static x => x.Content.Values)
                                .Select(static x => x.Schema)
                                .Where(static x => x != null && !x.IsReferencedSchema())
                                .Select(static x => x.GetDeprecationInformation())
                                .FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation responseDeprecationInformation)
            return responseDeprecationInformation;
        else if (operation.RequestBody?.Content.Values
                                            .Select(static x => x.Schema)
                                            .Where(static x => x != null && !x.IsReferencedSchema())
                                            .Select(static x => x.GetDeprecationInformation())
                                            .FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation requestDeprecationInformation)
            return requestDeprecationInformation;
        return new(null, null, null, null, operation.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiUrlTreeNode treeNode)
    {
        var operations = treeNode.PathItems[Constants.DefaultOpenApiLabel].Operations.Values.ToArray();
        if (operations.All(static x => x.Deprecated) && operations.Select(static x => x.GetDeprecationInformation()).FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation deprecationInformation)
            return deprecationInformation;
        return new(null, null, null, null, false);
    }
}
