// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.OpenApiExtensions;

/// <summary>
/// Extension element for OpenAPI to add deprecation information. x-ms-deprecation
/// Copied from https://github.com/microsoft/OpenAPI.NET.OData/blob/9bc5d29ded93dd1b7166f7f2434fa8fdbee6df5a/src/Microsoft.OpenApi.OData.Reader/OpenApiExtensions/OpenApiDeprecationExtension.cs#L16
/// Except for the Parse method
/// </summary>
public class OpenApiDeprecationExtension : IOpenApiExtension
{
    /// <summary>
    /// Name of the extension as used in the description.
    /// </summary>
    public static string Name => "x-ms-deprecation";
    /// <summary>
    /// The date at which the element has been/will be removed entirely from the service.
    /// </summary>
    public DateTimeOffset? RemovalDate
    {
        get; set;
    }
    /// <summary>
    /// The date at which the element has been/will be deprecated.
    /// </summary>
    public DateTimeOffset? Date
    {
        get; set;
    }
    /// <summary>
    /// The version this revision was introduced.
    /// </summary>
    public string Version
    {
        get; set;
    } = string.Empty;
    /// <summary>
    /// The description of the revision.
    /// </summary>
    public string Description
    {
        get; set;
    } = string.Empty;
    /// <inheritdoc />
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        if (RemovalDate.HasValue || Date.HasValue || !string.IsNullOrEmpty(Version) || !string.IsNullOrEmpty(Description))
        {
            writer.WriteStartObject();

            if (RemovalDate.HasValue)
                writer.WriteProperty(nameof(RemovalDate).ToFirstCharacterLowerCase(), RemovalDate.Value);
            if (Date.HasValue)
                writer.WriteProperty(nameof(Date).ToFirstCharacterLowerCase(), Date.Value);
            if (!string.IsNullOrEmpty(Version))
                writer.WriteProperty(nameof(Version).ToFirstCharacterLowerCase(), Version);
            if (!string.IsNullOrEmpty(Description))
                writer.WriteProperty(nameof(Description).ToFirstCharacterLowerCase(), Description);

            writer.WriteEndObject();
        }
    }
    /// <summary>
    /// Parses the <see cref="IOpenApiAny"/> to <see cref="OpenApiDeprecationExtension"/>.
    /// </summary>
    /// <param name="source">The source object.</param>
    /// <returns>The <see cref="OpenApiDeprecationExtension"/>.</returns>
    public static OpenApiDeprecationExtension Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiDeprecationExtension();
        if (rawObject.TryGetValue(nameof(RemovalDate).ToFirstCharacterLowerCase(), out var removalDate) && removalDate is OpenApiDateTime removalDateValue)
            extension.RemovalDate = removalDateValue.Value;
        if (rawObject.TryGetValue(nameof(Date).ToFirstCharacterLowerCase(), out var date) && date is OpenApiDateTime dateValue)
            extension.Date = dateValue.Value;
        if (rawObject.TryGetValue(nameof(Version).ToFirstCharacterLowerCase(), out var version) && version is OpenApiString versionValue)
            extension.Version = versionValue.Value;
        if (rawObject.TryGetValue(nameof(Description).ToFirstCharacterLowerCase(), out var description) && description is OpenApiString descriptionValue)
            extension.Description = descriptionValue.Value;
        return extension;
    }
}
