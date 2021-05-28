using System.Linq;

namespace Microsoft.Kiota.Abstractions.Extensions {
    public static class StringExtensions {
        public static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input[0])}{input[1..]}";
    }
}
