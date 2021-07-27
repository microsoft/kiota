using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, GoConventionService>
    {
        public CodeClassDeclarationWriter(GoConventionService conventionService) : base(conventionService) {}

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            if(codeElement?.Parent?.Parent is CodeNamespace ns)
                writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment()}");
            var importSegments = codeElement
                                .Usings
                                .Where(x => !x.Declaration.IsExternal)
                                .Select(i => i.Name.GetInternalNamespaceImport())
                                .Distinct()
                                .ToList();
            importSegments.AddRange(codeElement.Usings.Where(x => x.Declaration.IsExternal).Select(i => i.Declaration.Name).Distinct());
            if(importSegments.Any()) {
                writer.WriteLine("import (");
                writer.IncreaseIndent();
                foreach(var importSegment in importSegments)
                    writer.WriteLine($"\"{importSegment}\"");
                writer.DecreaseIndent();
                writer.WriteLine(")");
            }
            writer.WriteLine($"type {codeElement.Name.ToFirstCharacterUpperCase()} struct {{");
            writer.IncreaseIndent();
        }
        
    }
    public static class NamespaceStringExtensions {
        public static string GetLastNamespaceSegment(this string nsName) { 
            var urlPrefixIndex = nsName.LastIndexOf('/') + 1;
            return nsName[urlPrefixIndex..].Split('.').Last().ToLowerInvariant();
        }
        public static string GetInternalNamespaceImport(this string nsName) {
            var urlPrefixIndex = nsName.LastIndexOf('/') + 1;
            return nsName[0..urlPrefixIndex] + nsName[urlPrefixIndex..].Split('.').Aggregate((x, y) => $"{x}/{y}");
        }
    }
}
