using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers
{
    public class RelativeImportManager
    {
        private readonly string prefix;
        private readonly char separator;
        public RelativeImportManager(string namespacePrefix, char namespaceSeparator)
        {
            if (string.IsNullOrEmpty(namespacePrefix))
                throw new ArgumentNullException(nameof(namespacePrefix));
            if (namespaceSeparator == default)
                throw new ArgumentNullException(nameof(namespaceSeparator));

            prefix = namespacePrefix;
            separator = namespaceSeparator;
        }
        /// <summary>
        /// Returns the relative import path for the given using and import context namespace.
        /// </summary>
        /// <param name="codeUsing">The using to import into the current namespace context</param>
        /// <param name="currentNamespace">The current namespace</param>
        /// <returns>The import symbol, it's alias if any and the relative import path</returns>
        public virtual (string, string, string) GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace)
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
                if (string.IsNullOrEmpty(importPath))
                    importPath += codeUsing.Name;
                else
                    importPath += codeUsing.Declaration.Name.ToFirstCharacterLowerCase();
                return (importSymbol, codeUsing.Alias, importPath);
            }
        }
        protected string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace)
        {
            var result = currentNamespace.GetDifferential(importNamespace, prefix, separator);
            return result.State switch
            {
                NamespaceDifferentialTrackerState.Same => "./",
                NamespaceDifferentialTrackerState.Downwards => $"./{GetRemainingImportPath(result.DownwardsSegments)}",
                NamespaceDifferentialTrackerState.Upwards => GetUpwardsMoves(result.UpwardsMovesCount),
                NamespaceDifferentialTrackerState.UpwardsAndThenDownwards => $"{GetUpwardsMoves(result.UpwardsMovesCount)}/{GetRemainingImportPath(result.DownwardsSegments)}",
                _ => throw new NotImplementedException(),
            };
        }
        protected static string GetUpwardsMoves(int UpwardsMovesCount) => string.Join("/", Enumerable.Repeat("..", UpwardsMovesCount));
        protected static string GetRemainingImportPath(IEnumerable<string> remainingSegments)
        {
            if (remainingSegments.Any())
                return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}/{y}") + '/';
            else
                return string.Empty;
        }
    }
}
