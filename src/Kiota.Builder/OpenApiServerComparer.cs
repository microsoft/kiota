using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder;

internal partial class OpenApiServerComparer : IEqualityComparer<OpenApiServer>
{
    private static readonly Regex _protocolCleanupRegex = GetCleanupRegex();
    [GeneratedRegex("^https?://", RegexOptions.IgnoreCase | RegexOptions.Compiled, 200)]
    private static partial Regex GetCleanupRegex();
    public bool Equals(OpenApiServer? x, OpenApiServer? y)
    {
        return x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    public int GetHashCode([DisallowNull] OpenApiServer obj)
    {
        if (string.IsNullOrEmpty(obj?.Url))
            return 0;
        return _protocolCleanupRegex.Replace(obj.Url, string.Empty).GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
