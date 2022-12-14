using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public class InconsistentTypeFormatPair : ValidationRule<OpenApiSchema>
{
    private static readonly Dictionary<string, HashSet<string>> validPairs = new(StringComparer.OrdinalIgnoreCase) {
        ["string"] = new(StringComparer.OrdinalIgnoreCase) {
            "commonmark",
            "html",
            "date",
            "date-time",
            "duration",
            "time",
            "base64url",
            "uuid",
            "binary",
        },
        ["integer"] = new(StringComparer.OrdinalIgnoreCase) {
            "int32",
            "int64",
            "int8",
            "uint8",
            "int16",
            "uint16",
        },
        ["number"] = new(StringComparer.OrdinalIgnoreCase) {
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
    private static readonly HashSet<string> escapedTypes = new(StringComparer.OrdinalIgnoreCase) {
        "array",
        "boolean",
        "const",
        "enum",
        "null",
        "object",
    };
    public InconsistentTypeFormatPair() : base(static (context, schema) => {
        if (string.IsNullOrEmpty(schema?.Type) || string.IsNullOrEmpty(schema.Format) || KnownAndNotSupportedFormats.knownAndUnsupportedFormats.Contains(schema.Format) || escapedTypes.Contains(schema.Type))
            return;
        if (!validPairs.TryGetValue(schema.Type, out var validFormats) || !validFormats.Contains(schema.Format))
            context.CreateWarning(nameof(InconsistentTypeFormatPair), $"The format {schema.Format} is not supported by Kiota for the type {schema.Type} and the string type will be used.");
    })
    {
    }
}
