using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Extensions {
    public static class StringExtensions {
        public static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input.FirstOrDefault())}{input[1..]}";
        public static string ToFirstCharacterUpperCase(this string input)
            => string.IsNullOrEmpty(input) ? input : Char.ToUpperInvariant(input.FirstOrDefault()) + input[1..];
        public static string ToCamelCase(this string name)
        {
            if(string.IsNullOrEmpty(name)) return name;
            var chunks = name.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var identifier = chunks[0] + string.Join(string.Empty, chunks.Skip(1)
                                                                .Select(s => ToFirstCharacterUpperCase(s)));
            return identifier;
        }
        public static string ToPascalCase(this string name)
            => string.IsNullOrEmpty(name) ? name : String.Join(null, name.Split("-", StringSplitOptions.RemoveEmptyEntries)
                                                                            .Select(s => ToFirstCharacterUpperCase(s)));
        public static string ReplaceValueIdentifier(this string original) =>
            original?.Replace("$value", "Content");
        public static string TrimQuotes(this string original) =>
            original?.Trim('\'', '"');
        
        public static string ToSnakeCase(this string name, char separator = '_')
        {
            if(string.IsNullOrEmpty(name)) return name;
            var chunks = name.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var identifier = chunks[0] + string.Join(string.Empty, chunks.Skip(1)
                                                                .Select(s => ToFirstCharacterUpperCase(s)));
            if(identifier.Length < 2) {
                return identifier;
            }
            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(identifier[0]));
            foreach (var item in identifier[1..])
            {
                if(char.IsUpper(item)) {
                    sb.Append(separator);
                    sb.Append(char.ToLowerInvariant(item));
                } else {
                    sb.Append(item);
                }
            }
            var output = sb.ToString();
            int index = output.IndexOf("<");
            if (index >= 0)
                output = output.Substring(0, index);
            
            return output;
        }
        public static string NormalizeNameSpaceName(this string original, string delimiter) => 
            string.IsNullOrEmpty(original) ? 
                original :
                original?.Split('.').Select(x => x.ToFirstCharacterUpperCase()).Aggregate((z,y) => z + delimiter + y);
        private static readonly HashAlgorithm sha = SHA256.Create();
        public static string GetNamespaceImportSymbol(this string importName) {
            if(string.IsNullOrEmpty(importName)) return string.Empty;
            return "i" + HashString(importName).ToLowerInvariant();
        }
        private static string HashString(string input) {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return hash.Select(b => b.ToString("x2")).Aggregate((x, y) => x + y);
        }
        public static string SanitizeUrlTemplateParameterName(this string original) =>
            original?.Replace('-', '_');
        /// <summary>
        /// For Php strings, having double quotes around strings might cause an issue
        /// if the string contains valid variable name.
        /// For example $variable = "$value" will try too set the value of
        /// $variable to the variable named $value rather than the string '$value'
        /// around quotes as expected.
        /// </summary>
        /// <param name="current"></param>
        public static string ReplaceDoubleQuoteWithSingleQuote(this string current)
        {
            if (string.IsNullOrEmpty(current))
            {
                return current;
            }
            return current.StartsWith("\"", StringComparison.OrdinalIgnoreCase) ? current.Replace("'", "\\'").Replace('\"', '\'') : current;
        }
        
        public static string ReplaceDotsWithSlashInNamespaces(this string namespaced)
        {
            if (string.IsNullOrEmpty(namespaced))
            {
                return namespaced;
            }
            var parts = namespaced.Split('.');
            return string.Join('\\', parts.Select(x => x.ToFirstCharacterUpperCase())).Trim('\\');
        }
        private static readonly Regex propertyCleanupRegex = new(@"[""\s!#$%&'()*+,./:;<=>?@\[\]\\^`{}|~]", RegexOptions.Compiled);
        public static string CleanupSymbolName(this string original, params string[] prefixesToStrip)
        {
            if (string.IsNullOrEmpty(original))
                return original;

            foreach (var prefix in prefixesToStrip.Where(x => !string.IsNullOrEmpty(x)))
                original = original.Replace(prefix, string.Empty);
            
            original = original.ToCamelCase(); //ensure the name is camel cased to strip out any potential '-' characters

            original = propertyCleanupRegex.Replace(original, string.Empty); //strip out any invalid characters

            return original;
        }
    }
}
