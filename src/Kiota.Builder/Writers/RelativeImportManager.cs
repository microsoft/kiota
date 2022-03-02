using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers
{
    public class RelativeImportManager
    {
        private readonly int prefixLength;
        private readonly char separator;
        public RelativeImportManager(string namespacePrefix, char namespaceSeparator)
        {
            if (string.IsNullOrEmpty(namespacePrefix))
                throw new ArgumentNullException(nameof(namespacePrefix));
            if (namespaceSeparator == default)
                throw new ArgumentNullException(nameof(namespaceSeparator));

            prefixLength = namespacePrefix.Length;
            separator = namespaceSeparator;
        }
        /// <summary>
        /// Returns the relative import path for the given using and import context namespace.
        /// </summary>
        /// <param name="codeUsing">The using to import into the current namespace context</param>
        /// <param name="currentNamespace">The current namespace</param>
        /// <returns>The import symbol, it's alias if any and the relative import path</returns>
        public (string, string, string) GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace)
        {
            if (codeUsing?.IsExternal ?? true)
                return (string.Empty, string.Empty, string.Empty);//it's an external import, add nothing
            var typeDef = codeUsing.Declaration.TypeDefinition;

            var importSymbol = codeUsing.Declaration == null ? codeUsing.Name : codeUsing.Declaration.TypeDefinition switch
            {
                CodeFunction f => f.Name.ToFirstCharacterLowerCase(),
                _ => codeUsing.Declaration.TypeDefinition.Name.ToFirstCharacterUpperCase(),
            };

            if (typeDef == null)
                return (importSymbol, codeUsing.Alias, "./"); // it's relative to the folder, with no declaration (default failsafe)
            else
            {
                var importPath = GetImportRelativePathFromNamespaces(currentNamespace,
                                                        typeDef.GetImmediateParentOfType<CodeNamespace>());
                if (importPath == "./")
                {
                    importPath += "index";
                }
                else if (string.IsNullOrEmpty(importPath))
                {
                    importPath += codeUsing.Name;
                }
                return (importSymbol, codeUsing.Alias, importPath);
            }
        }
        private string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace)
        {
            if (currentNamespace == null)
                throw new ArgumentNullException(nameof(currentNamespace));
            else if (importNamespace == null)
                throw new ArgumentNullException(nameof(importNamespace));
            else if (currentNamespace == importNamespace || currentNamespace.Name.Equals(importNamespace.Name, StringComparison.OrdinalIgnoreCase)) // we're in the same namespace
                return "./";
            else
                return GetRelativeImportPathFromSegments(currentNamespace, importNamespace);
        }
        private string GetRelativeImportPathFromSegments(CodeNamespace currentNamespace, CodeNamespace importNamespace)
        {
            var currentNamespaceSegments = currentNamespace
                                    .Name[prefixLength..]
                                    .Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegments = importNamespace
                                .Name[prefixLength..]
                                .Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegmentsCount = importNamespaceSegments.Length;
            var currentNamespaceSegmentsCount = currentNamespaceSegments.Length;
            var deeperMostSegmentIndex = 0;
            while (deeperMostSegmentIndex < Math.Min(importNamespaceSegmentsCount, currentNamespaceSegmentsCount))
            {
                if (currentNamespaceSegments.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex), StringComparison.OrdinalIgnoreCase))
                    deeperMostSegmentIndex++;
                else
                    break;
            }
            if (deeperMostSegmentIndex == currentNamespaceSegmentsCount)
            { // we're in a parent namespace and need to import with a relative path
                return "./" + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            }
            else
            { // we're in a sub namespace and need to go "up" with dot dots
                var upMoves = currentNamespaceSegmentsCount - deeperMostSegmentIndex;
                var pathSegmentSeparator = upMoves > 0 ? "/" : string.Empty;
                return string.Join("/", Enumerable.Repeat("..", upMoves)) +
                        pathSegmentSeparator +
                        GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            }
        }
        private static string GetRemainingImportPath(IEnumerable<string> remainingSegments)
        {
            if (remainingSegments.Any())
                return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}/{y}") + '/';
            else
                return string.Empty;
        }
    }
}
