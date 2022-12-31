using System;
using System.IO;

namespace Kiota.Builder.Extensions;

public static class UriExtensions {
    public static string GetFileName(this Uri uri) {
        if(uri is null) return string.Empty;
        return Path.GetFileName($"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}");
    }
}
