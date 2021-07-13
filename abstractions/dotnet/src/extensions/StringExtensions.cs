using System.Linq;

namespace Microsoft.Kiota.Abstractions.Extensions {
    public static class StringExtensions {
        /// <summary>
        ///     Returns a string with the first letter lowered.
        /// </summary>
        /// <param name="input">The string to lowercase.</param>
        /// <returns>The input string with the first letter lowered.</returns>
        public static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input[0])}{input[1..]}";
    }
}
