using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Kiota.Builder.Extensions {
    public static class StringExtensions {
        public static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : char.ToLowerInvariant(input[0]) + input[1..];
        public static string ToFirstCharacterUpperCase(this string input)
            => string.IsNullOrEmpty(input) ? input : char.ToUpperInvariant(input[0]) + input[1..];

        /// <summary>
        /// Converts a string delimited by a symbol to camel case
        /// </summary>
        /// <param name="input">The input string</param>
        /// <param name="delimiters">The delimiters to use when converting to camel case. If none is given, defaults to '-'</param>
        /// <returns>A camel case string</returns>
        public static string ToCamelCase(this string input, params string[] delimiters)
        {
            if (string.IsNullOrEmpty(input)) return input;
            delimiters = delimiters.Any() ? delimiters : new[] { "-" };
            var chunks = input.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (chunks.Length < 1) return string.Empty;
            return chunks[0] + string.Join(string.Empty, chunks.Skip(1).Select(c => c.ToFirstCharacterUpperCase()));
        }

        public static string ToPascalCase(this string name)
            => string.IsNullOrEmpty(name) ? name : string.Join(null, name.Split("-", StringSplitOptions.RemoveEmptyEntries)
                                                                            .Select(s => ToFirstCharacterUpperCase(s)));
        public static string ReplaceValueIdentifier(this string original) =>
            original?.Replace("$value", "Content");
        public static string TrimQuotes(this string original) =>
            original?.Trim('\'', '"');

        public static string ToSnakeCase(this string name, char separator = '_')
        {
            if (string.IsNullOrEmpty(name)) return name;

            var nameSpan = name.AsSpan();
            var indexOfLess = nameSpan.IndexOf('<');
            if (indexOfLess >= 0)
            {
                nameSpan = nameSpan[..indexOfLess];
            }

            static int CountNecessaryNewSeparators(ReadOnlySpan<char> span)
            {
                int count = 0;
                for (var i = 1; i < span.Length; i++)
                {
                    if (char.IsUpper(span[i]) && span[i - 1] is not '_' and not '-')
                    {
                        count++;
                    }
                }

                return count;
            }

            Span<char> span = stackalloc char[nameSpan.Length + CountNecessaryNewSeparators(nameSpan)];
            var current = nameSpan[0];
            span[0] = char.ToLowerInvariant(current);
            var counter = 1;
            for (int i = 1; i < nameSpan.Length; i++)
            {
                current = nameSpan[i];
                if (current == '-')
                {
                    if (!char.IsUpper(nameSpan[i + 1])) span[counter++] = separator;
                }
                else if (char.IsUpper(current))
                {
                    if (nameSpan[i - 1] != '_') span[counter++] = separator;
                    span[counter++] = char.ToLowerInvariant(current);
                }
                else
                {
                    span[counter++] = current;
                }
            }

            return new string(span);
        }

        public static string NormalizeNameSpaceName(this string original, string delimiter) => 
            string.IsNullOrEmpty(original) ? 
                original :
                original?.Split('.').Select(x => x.ToFirstCharacterUpperCase()).Aggregate((z,y) => z + delimiter + y);
        private static readonly ThreadLocal<HashAlgorithm> sha = new(() => SHA256.Create()); // getting safe handle null exception from BCrypt on concurrent multi-threaded access
        public static string GetNamespaceImportSymbol(this string importName, string prefix = "i") {
            if(string.IsNullOrEmpty(importName)) return string.Empty;
            return prefix + HashString(importName).ToLowerInvariant();
        }
        private static string HashString(string input) {
                var hash = sha.Value.ComputeHash(Encoding.UTF8.GetBytes(input));
                return hash.Select(b => b.ToString("x2")).Aggregate((x, y) => x + y);
        }
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
        ///<summary>
        /// Cleanup regex that removes all special characters from ASCII 0-127
        ///</summary>
        private static readonly Regex propertyCleanupRegex = new(@"[""\s!#$%&'()*+,./:;<=>?@\[\]\\^`{}|~-](?<followingLetter>\w)?", RegexOptions.Compiled);
        private const string CleanupGroupName = "followingLetter";
        public static string CleanupSymbolName(this string original)
        {
            if (string.IsNullOrEmpty(original))
                return original;

            var result = propertyCleanupRegex.Replace(original, 
                                    static x => x.Groups.Keys.Contains(CleanupGroupName) ? 
                                                    x.Groups[CleanupGroupName].Value.ToFirstCharacterUpperCase() :
                                                    string.Empty); //strip out any invalid characters, and replace any following one by its uppercase version

            if(result.Any() && int.TryParse(result.AsSpan(0, 1), out var _)) // in most languages a number or starting with a number is not a valid symbol name
                result = NumbersSpellingRegex.Replace(result, static x => x.Groups["number"]
                                                                        .Value
                                                                        .Select(static x => SpelledOutNumbers[x])
                                                                        .Aggregate(static (z,y) => z + y));

            return result;
        }
        private static readonly Regex NumbersSpellingRegex = new(@"^(?<number>\d+)", RegexOptions.Compiled);
        private static readonly Dictionary<char, string> SpelledOutNumbers = new() {
            {'0', "Zero"},
            {'1', "One"},
            {'2', "Two"},
            {'3', "Three"},
            {'4', "Four"},
            {'5', "Five"},
            {'6', "Six"},
            {'7', "Seven"},
            {'8', "Eight"},
            {'9', "Nine"},
        };

        /// <summary>
        /// Cleanup the XML string
        /// </summary>
        /// <param name="original">The original string</param>
        /// <returns></returns>
        public static string CleanupXMLString(this string original) 
            => SecurityElement.Escape(original);
    }
}
