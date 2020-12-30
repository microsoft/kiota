using System;
using System.Linq;

namespace kiota.core {
    public static class StringExtensions {
        public static string ToFirstCharacterLowerCase(this string input) {
            return string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input.FirstOrDefault())}{input.Substring(1)}";
        }
        public static string ToFirstCharacterUpperCase(this string input)
        {
            if (input.Length == 0) return input;
            return Char.ToUpperInvariant(input.FirstOrDefault()) + input.Substring(1);
        }

        public static string ToCamelCase(this string name)
        {
            var chunks = name.Split("-");
            var identifier = String.Join(null, chunks.Take(1)
                                                  .Union(chunks.Skip(1)
                                                                .Select(s => ToFirstCharacterUpperCase(s))));
            return identifier;
        }
        public static string ToPascalCase(this string name)
        {
            var chunks = name.Split("-");
            var identifier = String.Join(null, chunks.Select(s => ToFirstCharacterUpperCase(s)));
            return identifier;
        }
    }
}
