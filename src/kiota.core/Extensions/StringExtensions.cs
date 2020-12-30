using System.Linq;

namespace kiota.core {
    public static class StringExtensions {
        public static string ToLowerFirstCharacter(this string current) {
            return $"{char.ToLowerInvariant(current.FirstOrDefault())}{current.Substring(1)}";
        }
    }
}
