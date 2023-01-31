
using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public class KnownAndNotSupportedFormats : ValidationRule<OpenApiSchema>
{
    internal static readonly HashSet<string> knownAndUnsupportedFormats = new(StringComparer.OrdinalIgnoreCase) {
        "email",
        "idn-email",
        "hostname",
        "idn-hostname",
        "ipv4",
        "ipv6",
        "uri",
        "uri-reference",
        "iri",
        "iri-reference",
        "uri-template",
        "json-pointer",
        "relative-json-pointer",
        "regex",
    };
    public KnownAndNotSupportedFormats() : base(static (context, schema) =>
    {
        if (!string.IsNullOrEmpty(schema.Format) && knownAndUnsupportedFormats.Contains(schema.Format))
            context.CreateWarning(nameof(KnownAndNotSupportedFormats), $"The format {schema.Format} is not supported by Kiota and the string type will be used.");
    })
    {
    }
}
