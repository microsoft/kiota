using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests;
public static class AssertExtensions {
    public static void CurlyBracesAreClosed(string generatedCode, int offset = 0) {
        if(!string.IsNullOrEmpty(generatedCode))
            Assert.Equal(generatedCode.Count(x => x == '{'), generatedCode.Count(x => x == '}') + offset);
    }
}
