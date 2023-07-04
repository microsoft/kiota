using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Extensions;

internal static class CodePropertiesEnumerableExtensions
{
    public static CodeProperty? FirstOrDefaultOfKind(this IEnumerable<CodeProperty> properties, params CodePropertyKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.FirstOrDefault(x => x != null && x.IsOfKind(kinds));
    }
    public static IEnumerable<CodeProperty> OfKind(this IEnumerable<CodeProperty> properties, params CodePropertyKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.Where(x => x != null && x.IsOfKind(kinds));
    }
}
