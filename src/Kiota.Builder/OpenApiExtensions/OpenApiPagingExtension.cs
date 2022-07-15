using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

[assembly: InternalsVisibleTo("Kiota.Builder.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100957cb48387b2a5f54f5ce39255f18f26d32a39990db27cf48737afc6bc62759ba996b8a2bfb675d4e39f3d06ecb55a178b1b4031dcb2a767e29977d88cce864a0d16bfc1b3bebb0edf9fe285f10fffc0a85f93d664fa05af07faa3aad2e545182dbf787e3fd32b56aca95df1a3c4e75dec164a3f1a4c653d971b01ffc39eb3c4")]
namespace Kiota.Builder.OpenApiExtensions;

/// <summary>
/// Extension element for OpenAPI to add pageable information.
/// Based of the AutoRest specification https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md#x-ms-pageable
/// </summary>
internal class OpenApiPagingExtension : IOpenApiExtension
{
    /// <summary>
    /// Name of the extension as used in the description.
    /// </summary>
    public static string Name => "x-ms-pageable";

    /// <summary>
    /// The name of the property that provides the collection of pageable items.
    /// </summary>
    public string ItemName
    {
        get; set;
    } = "value";

    /// <summary>
    /// The name of the property that provides the next link (common: nextLink)
    /// </summary>
    public string NextLinkName
    {
        get; set;
    } = "nextLink";

    /// <summary>
    /// The name (operationId) of the operation for retrieving the next page.
    /// </summary>
    public string OperationName
    {
        get; set;
    }

    /// <inheritdoc />
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(NextLinkName))
        {
            writer.WriteProperty(nameof(NextLinkName).ToFirstCharacterLowerCase(), NextLinkName);
        }

        if (!string.IsNullOrEmpty(OperationName))
        {
            writer.WriteProperty(nameof(OperationName).ToFirstCharacterLowerCase(), OperationName);
        }

        writer.WriteProperty(nameof(ItemName).ToFirstCharacterLowerCase(), ItemName);

        writer.WriteEndObject();
    }

    public static OpenApiPagingExtension Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiPagingExtension();
        if (rawObject.TryGetValue(nameof(NextLinkName).ToFirstCharacterLowerCase(), out var nextLinkName) && nextLinkName is OpenApiString nextLinkNameStr)
        {
            extension.NextLinkName = nextLinkNameStr.Value;
        }

        if (rawObject.TryGetValue(nameof(OperationName).ToFirstCharacterLowerCase(), out var opName) && opName is OpenApiString opNameStr)
        {
            extension.OperationName = opNameStr.Value;
        }

        if (rawObject.TryGetValue(nameof(ItemName).ToFirstCharacterLowerCase(), out var itemName) && itemName is OpenApiString itemNameStr)
        {
            extension.ItemName = itemNameStr.Value;
        }

        return extension;
    }
}
