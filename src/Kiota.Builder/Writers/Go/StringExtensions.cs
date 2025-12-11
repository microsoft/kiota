using System;
using System.Linq;

namespace Kiota.Builder.Writers.Go;

public static class StringExtensions
{
    public static string TrimCollectionAndPointerSymbols(this string s) =>
        string.IsNullOrEmpty(s) ? s : s.TrimStart('[').TrimStart(']').TrimStart('*');

    public static string TrimPackageReference(this string s) =>
        !string.IsNullOrEmpty(s) && s.Contains('.', StringComparison.InvariantCultureIgnoreCase) ? s.Split('.').Last() : s;

}
