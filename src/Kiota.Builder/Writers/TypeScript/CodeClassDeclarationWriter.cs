using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, TypeScriptConventionService>
    {
        public CodeClassDeclarationWriter(TypeScriptConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            foreach (var codeUsing in codeElement.Usings
                                        .Where(x => x.Declaration?.IsExternal ?? false)
                                        .GroupBy(x => x.Declaration?.Name)
                                        .OrderBy(x => x.Key))
            {
                writer.WriteLine($"import {{{codeUsing.Select(x => x.Name).Distinct().Aggregate((x,y) => x + ", " + y)}}} from '{codeUsing.Key}';");
            }
            foreach (var codeUsing in codeElement.Usings
                                        .Where(x => (!x.Declaration?.IsExternal) ?? true)
                                        .Where(x => !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase))
                                        .Select(x => {
                                            var relativeImportPath = GetRelativeImportPathForUsing(x, codeElement.GetImmediateParentOfType<CodeNamespace>());
                                            return new {
                                                sourceSymbol = $"{relativeImportPath}{(string.IsNullOrEmpty(relativeImportPath) ? x.Name : x.Declaration.Name.ToFirstCharacterLowerCase())}",
                                                importSymbol = $"{x.Declaration?.Name?.ToFirstCharacterUpperCase() ?? x.Name}",
                                            };
                                        })
                                        .GroupBy(x => x.sourceSymbol)
                                        .OrderBy(x => x.Key))
            {
                                                    
                writer.WriteLine($"import {{{codeUsing.Select(x => x.importSymbol).Distinct().Aggregate((x,y) => x + ", " + y)}}} from '{codeUsing.Key}';");
            }
            writer.WriteLine();
            var derivation = (codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                            (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
            writer.WriteLine($"export class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            writer.IncreaseIndent();
        }
        private static string GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace) {
            if(codeUsing.Declaration == null)
                return string.Empty;//it's an external import, add nothing
            var typeDef = codeUsing.Declaration.TypeDefinition;

            if(typeDef == null)
                return "./"; // it's relative to the folder, with no declaration (default failsafe)
            else
                return GetImportRelativePathFromNamespaces(currentNamespace, 
                                                        typeDef.GetImmediateParentOfType<CodeNamespace>());
        }
        private static char namespaceNameSeparator = '.';
        private static string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace) {
            if(currentNamespace == null)
                throw new ArgumentNullException(nameof(currentNamespace));
            else if (importNamespace == null)
                throw new ArgumentNullException(nameof(importNamespace));
            else if(currentNamespace.Name.Equals(importNamespace.Name, StringComparison.OrdinalIgnoreCase)) // we're in the same namespace
                return "./";
            else
                return GetRelativeImportPathFromSegments(currentNamespace, importNamespace);                
        }
        private static string GetRelativeImportPathFromSegments(CodeNamespace currentNamespace, CodeNamespace importNamespace) {
            var currentNamespaceSegements = currentNamespace
                                    .Name
                                    .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegments = importNamespace
                                .Name
                                .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var importNamespaceSegmentsCount = importNamespaceSegments.Length;
            var currentNamespaceSegementsCount = currentNamespaceSegements.Length;
            var deeperMostSegmentIndex = 0;
            while(deeperMostSegmentIndex < Math.Min(importNamespaceSegmentsCount, currentNamespaceSegementsCount)) {
                if(currentNamespaceSegements.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex), StringComparison.OrdinalIgnoreCase))
                    deeperMostSegmentIndex++;
                else
                    break;
            }
            if (deeperMostSegmentIndex == currentNamespaceSegementsCount) { // we're in a parent namespace and need to import with a relative path
                return "./" + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            } else { // we're in a sub namespace and need to go "up" with dot dots
                var upMoves = currentNamespaceSegementsCount - deeperMostSegmentIndex;
                var upMovesBuilder = new StringBuilder();
                for(var i = 0; i < upMoves; i++)
                    upMovesBuilder.Append("../");
                return upMovesBuilder.ToString() + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
            }
        }
        private static string GetRemainingImportPath(IEnumerable<string> remainingSegments) {
            if(remainingSegments.Any())
                return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}/{y}") + '/';
            else
                return string.Empty;
        }
    }
}
