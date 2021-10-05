using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public static class GoNamespaceExtensions {
        public static string GetLastNamespaceSegment(this string nsName) {
            if(string.IsNullOrEmpty(nsName)) return string.Empty;
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
    }
}
