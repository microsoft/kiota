using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Go {
    public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, GoConventionService>
    {
        public CodeNamespaceWriter(GoConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
            writer.WriteLines($"module {codeElement.Name.GetLastNamespaceSegment().ToLowerInvariant()}",
                            string.Empty);
            var importNamespaces = codeElement.GetChildElements(true)
                                            .OfType<CodeNamespace>()
                                            .Select(ns => ns.GetInternalNamespaceImport())
                                            .ToList();
            importNamespaces
                        .Select(x => $"{replacePrefix} {x} {replaceMapping} ./{x.GetLastNamespaceSegment()}")
                        .Union(GetDefaultReplaces(codeElement))
                        .OrderBy(x => x.Split('=').First())
                        .ToList()
                        .ForEach(x => writer.WriteLine(x));
            writer.WriteLines(string.Empty,
                            "go 1.16",
                            string.Empty,
                            "require (");
            writer.IncreaseIndent();
            importNamespaces
                        .Select(x => $"{x} {tempVersionSuffix}")
                        .Union(defaultRefs)
                        .OrderBy(x => x.Split(' ').First())
                        .ToList()
                        .ForEach(x => writer.WriteLine(x));
            writer.DecreaseIndent();
            writer.WriteLine(")");
        }
        private static HashSet<string> GetDefaultReplaces(CodeNamespace ns) { // TODO: remove this method when abstractions are published
            var depth = ns.Depth + 2; // magic number to get out of samples/msgraph-mail
            var navigation = 
                Enumerable
                    .Range(0, depth)
                    .Select(_ => "../")
                    .Aggregate((x, y) => $"{x}{y}");
            var pathPrefix = $"{navigation}abstractions/go";
            return new () {
                $"{replacePrefix} {abstractionsRef} {replaceMapping} {pathPrefix}",
                $"{replacePrefix} {serializationRef} {replaceMapping} {pathPrefix}/serialization",    
            };
        }
        private static HashSet<string> defaultRefs = new () { //TODO update versions when abstractions are published
            $"{abstractionsRef} {tempVersionSuffix}",
            $"{serializationRef} {tempVersionSuffix}",
        };
        private const string tempVersionSuffix = "v0.0.0-00010101000000-000000000000";
        private const string abstractionsRef = "github.com/microsoft/kiota/abstractions/go";
        private const string serializationRef = "github.com/microsoft/kiota/abstractions/go/serialization";
        private const string replacePrefix = "replace";
        private const string replaceMapping = "=>";
    }
}
