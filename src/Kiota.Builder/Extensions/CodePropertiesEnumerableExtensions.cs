using System;
using System.Collections.Generic;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Extensions;

internal static class CodePropertiesEnumerableExtensions
{
    public static CodeProperty? FirstOrDefaultOfKind(this IEnumerable<CodeProperty> properties, params CodePropertyKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(properties);

        foreach (var property in properties)
        {
            if (property != null && property.IsOfKind(kinds))
            {
                return property;
            }
        }

        return null;
    }

    public static IEnumerable<CodeProperty> OfKind(this IEnumerable<CodeProperty> properties, params CodePropertyKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var result = new List<CodeProperty>();
        foreach (var property in properties)
        {
            if (property != null && property.IsOfKind(kinds))
            {
                result.Add(property);
            }
        }

        return result;
    }
}
