using System;
using System.Collections.Generic;
using Microsoft.OpenApi;

namespace Kiota.Builder.Validation;

public class InconsistentTypeFormatPair : ValidationRule<IOpenApiSchema>
{
    private static readonly Dictionary<JsonSchemaType, HashSet<string>> validPairs = new()
    {
        [JsonSchemaType.String] = new(StringComparer.OrdinalIgnoreCase) {
            "commonmark",
            "html",
            "date",
            "date-time",
            "duration",
            "time",
            "base64url",
            "uuid",
            "binary",
            "byte",
        },
        [JsonSchemaType.Integer] = new(StringComparer.OrdinalIgnoreCase) {
            "int32",
            "int64",
            "int8",
            "uint8",
            "int16",
            "uint16",
        },
        [JsonSchemaType.Number] = new(StringComparer.OrdinalIgnoreCase) {
            "float",
            "double",
            "decimal",
            "int32",
            "int64",
            "int8",
            "uint8",
            "int16",
            "uint16",
        },
    };
    private static readonly HashSet<JsonSchemaType> escapedTypes = [
        JsonSchemaType.Array,
        JsonSchemaType.Boolean,
        JsonSchemaType.Null,
        JsonSchemaType.Object,
    ];
    public InconsistentTypeFormatPair() : base(nameof(InconsistentTypeFormatPair), static (context, schema) =>
    {
        if (schema is null || !schema.Type.HasValue || string.IsNullOrEmpty(schema.Format) || KnownAndNotSupportedFormats.knownAndUnsupportedFormats.Contains(schema.Format) || escapedTypes.Contains(schema.Type.Value))
            return;
        var sanitizedType = schema.Type.Value & ~JsonSchemaType.Null;
        if (!validPairs.TryGetValue(sanitizedType, out var validFormats) || !validFormats.Contains(schema.Format))
            context.CreateWarning(nameof(InconsistentTypeFormatPair), $"The format {schema.Format} is not supported by Kiota for the type {sanitizedType} and the string type will be used.");
    })
    {
    }
}
