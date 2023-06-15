using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
internal static class OpenApiDeprecationExtensionExtensions
{
    internal static DeprecationInformation ToDeprecationInformation(this OpenApiDeprecationExtension value)
    {
        return new DeprecationInformation(value.Description, value.Date, value.RemovalDate, value.Version, true);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiSchema schema)
    {
        if (schema.Deprecated)
            if (schema.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
                return deprecatedValue.ToDeprecationInformation();
        return new(null, null, null, null, schema.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiOperation operation)
    {
        if (operation.Deprecated)
            if (operation.Extensions.TryGetValue(OpenApiDeprecationExtension.Name, out var deprecatedExtension) && deprecatedExtension is OpenApiDeprecationExtension deprecatedValue)
                return deprecatedValue.ToDeprecationInformation();
            else if (operation.Responses.Values
                                    .SelectMany(static x => x.Content.Values)
                                    .Select(static x => x.Schema)
                                    .Where(static x => x != null && !x.IsReferencedSchema())
                                    .Select(static x => x.GetDeprecationInformation())
                                    .FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation responseDeprecationInformation)
                return responseDeprecationInformation;
            else if (operation.RequestBody.Content.Values
                                                .Select(static x => x.Schema)
                                                .Where(static x => x != null && !x.IsReferencedSchema())
                                                .Select(static x => x.GetDeprecationInformation())
                                                .FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation requestDeprecationInformation)
                return requestDeprecationInformation;
        return new(null, null, null, null, operation.Deprecated);
    }
    internal static DeprecationInformation GetDeprecationInformation(this OpenApiPathItem pathItem)
    {
        if (pathItem.Operations.Values.All(static x => x.Deprecated))
            if (pathItem.Operations.Values.Select(static x => x.GetDeprecationInformation()).FirstOrDefault(static x => x.IsDeprecated) is DeprecationInformation deprecationInformation)
                return deprecationInformation;
            else
                return new(null, null, null, null, true);
        return new(null, null, null, null, false);
    }
}
