using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Extensions;

public static class CodePropertiesEnumerableExtensions
{
    public static CodeProperty? OfKind(this IEnumerable<CodeProperty> properties, CodePropertyKind kind)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.FirstOrDefault(x => x != null && x.IsOfKind(kind));
    }
}
