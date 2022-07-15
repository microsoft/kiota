// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Kiota.Builder.OpenApiExtensions;

/// <summary>
/// Extension element for OpenAPI to add enum values descriptions.
/// Based of the AutoRest specification https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md#x-ms-enum
/// THIS FILE IS A COPY OF https://github.com/microsoft/OpenAPI.NET.OData/blob/dbcf68683d1e21e00af9bfe5338e74556278419f/src/Microsoft.OpenApi.OData.Reader/OpenApiExtensions/OpenApiEnumValuesDescriptionExtension.cs
/// except for the parse method
/// </summary>
public class OpenApiEnumValuesDescriptionExtension : IOpenApiExtension
{
	/// <summary>
    /// Name of the extension as used in the description.
    /// </summary>
	public static string Name => "x-ms-enum";

	/// <summary>
	/// The of the enum.
	/// </summary>
	public string EnumName { get; set; }

	/// <summary>
	/// Descriptions for the enum symbols, where the value MUST match the enum symbols in the main description
	/// </summary>
	public List<EnumDescription> ValuesDescriptions { get; set; } = new();

	/// <inheritdoc />
	public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
	{
		if(writer == null)
			throw new ArgumentNullException(nameof(writer));
		if((specVersion == OpenApiSpecVersion.OpenApi2_0 || specVersion == OpenApiSpecVersion.OpenApi3_0) &&
			!string.IsNullOrEmpty(EnumName) &&
			ValuesDescriptions.Any())
		{ // when we upgrade to 3.1, we don't need to write this extension as JSON schema will support writing enum values
			writer.WriteStartObject();
			writer.WriteProperty(nameof(Name).ToFirstCharacterLowerCase(), EnumName);
			writer.WriteProperty("modelAsString", false);
			writer.WriteRequiredCollection("values", ValuesDescriptions, (w, x) => {
				w.WriteStartObject();
				w.WriteProperty(nameof(x.Value).ToFirstCharacterLowerCase(), x.Value);
				w.WriteProperty(nameof(x.Description).ToFirstCharacterLowerCase(), x.Description);
				w.WriteProperty(nameof(x.Name).ToFirstCharacterLowerCase(), x.Name);
				w.WriteEndObject();
			});
			writer.WriteEndObject();
		}
	}
    public static OpenApiEnumValuesDescriptionExtension Parse(IOpenApiAny source)
    {
        if (source is not OpenApiObject rawObject) throw new ArgumentOutOfRangeException(nameof(source));
        var extension = new OpenApiEnumValuesDescriptionExtension();
        if (rawObject.TryGetValue("values", out var values) && values is OpenApiArray valuesArray) {
            extension.ValuesDescriptions.AddRange(valuesArray
                                            .OfType<OpenApiObject>()
                                            .Select(x => new EnumDescription(x)));
        }
        return extension;
    }
}

public class EnumDescription : IOpenApiElement
{
    public EnumDescription()
    {
        
    }
    public EnumDescription(OpenApiObject source)
    {
        if(source.TryGetValue("value", out var rawValue) && rawValue is OpenApiString value)
            Value = value.Value;
        if(source.TryGetValue("description", out var rawDescription) && rawDescription is OpenApiString description)
            Description = description.Value;
        if(source.TryGetValue("name", out var rawName) && rawName is OpenApiString name)
            Name = name.Value; 
    }
	/// <summary>
	/// The description for the enum symbol
	/// </summary>
	public string Description { get; set; }
	/// <summary>
	/// The symbol for the enum symbol to use for code-generation
	/// </summary>
	public string Name { get; set; }
	/// <summary>
	/// The symbol as described in the main enum schema.
	/// </summary>
	public string Value { get; set; }
}
