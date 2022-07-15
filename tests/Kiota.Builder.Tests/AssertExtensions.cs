using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests;
public static class AssertExtensions {
    public static void CurlyBracesAreClosed(string generatedCode, int offset = 0) {
        if(!string.IsNullOrEmpty(generatedCode))
            Assert.Equal(generatedCode.Count(x => x == '{'), generatedCode.Count(x => x == '}') + offset);
    }
    public static void Before(string before, string after, string generatedCode, string start = default, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
        Assert.InRange(generatedCode.IndexOf(before, comparison), string.IsNullOrEmpty(start) ? 0 : generatedCode.IndexOf(start, comparison), generatedCode.IndexOf(after, comparison));
    }
}
