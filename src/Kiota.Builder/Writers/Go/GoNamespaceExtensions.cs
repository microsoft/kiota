using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Kiota.Builder.Writers.Go {
    public static class GoNamespaceExtensions {
        public static string GetLastNamespaceSegment(this string nsName) { 
            var urlPrefixIndex = nsName.LastIndexOf('/') + 1;
            var tentativeSegment = nsName[urlPrefixIndex..].Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if(string.IsNullOrEmpty(tentativeSegment)) tentativeSegment = nsName.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
            return tentativeSegment.ToLowerInvariant();
        }
        public static string GetInternalNamespaceImport(this CodeElement ns) {
            if(ns == null) return string.Empty;
            var urlPrefixIndex = ns.Name.LastIndexOf('/') + 1;
            return (ns.Name[0..urlPrefixIndex] + ns.Name[urlPrefixIndex..].Split('.', StringSplitOptions.RemoveEmptyEntries).Aggregate((x, y) => $"{x}/{y}")).ToLowerInvariant();
        }
        public static string GetNamespaceImportSymbol(this CodeElement ns) {
            if(ns == null) return string.Empty;
            var importName = ns.GetInternalNamespaceImport();
            return importName.GetNamespaceImportSymbol();
        }
        private static HashAlgorithm sha = SHA256.Create();
        public static string GetNamespaceImportSymbol(this string importName) {
            if(string.IsNullOrEmpty(importName)) return string.Empty;
            return "i" + HashString(importName).ToLowerInvariant();
        }
        private static string HashString(string input) {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return hash.Select(b => b.ToString("x2")).Aggregate((x, y) => x + y);
        }
    }
}
