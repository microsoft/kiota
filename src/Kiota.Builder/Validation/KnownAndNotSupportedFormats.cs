
using System;
using System.Collections.Generic;
using Microsoft.OpenApi;

namespace Kiota.Builder.Validation;

public class KnownAndNotSupportedFormats : ValidationRule<IOpenApiSchema>
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
    public KnownAndNotSupportedFormats() : base(nameof(KnownAndNotSupportedFormats), static (context, schema) =>
    {
        if (!IsHeaderSchema(context.PathString) &&
            !string.IsNullOrEmpty(schema.Format) && knownAndUnsupportedFormats.Contains(schema.Format))
            context.CreateWarning(nameof(KnownAndNotSupportedFormats), $"The format {schema.Format} is not supported by Kiota and the string type will be used.");
    })
    {
    }

    internal static bool IsHeaderSchema(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < pathSegments.Length - 1; i++)
        {
            if (pathSegments[i].Equals("components", StringComparison.OrdinalIgnoreCase) &&
                pathSegments[i + 1].Equals("headers", StringComparison.OrdinalIgnoreCase))
                return true;
            if (pathSegments[i].Equals("responses", StringComparison.OrdinalIgnoreCase) &&
                i + 2 < pathSegments.Length &&
                pathSegments[i + 2].Equals("headers", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
