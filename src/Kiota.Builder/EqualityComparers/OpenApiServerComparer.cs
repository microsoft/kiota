using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.EqualityComparers;

internal sealed partial class OpenApiServerComparer : IEqualityComparer<OpenApiServer>
{
    [GeneratedRegex("^https?://", RegexOptions.IgnoreCase | RegexOptions.Compiled, 500)]
    private static partial Regex protocolCleanupRegex();
    public bool Equals(OpenApiServer? x, OpenApiServer? y)
    {
        return x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    public int GetHashCode([DisallowNull] OpenApiServer obj)
    {
        if (string.IsNullOrEmpty(obj?.Url))
            return 0;
        return protocolCleanupRegex().Replace(obj.Url, string.Empty).GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
