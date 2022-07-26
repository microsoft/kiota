using System.Linq;

namespace  Kiota.Builder.Writers.Swift {
    public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, SwiftConventionService>
    {
        public CodeNamespaceWriter(SwiftConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
            var segments = codeElement.Name.Split(".");
            var lastSegment = segments.Last();
            var parentNamespaces = string.Join('.', segments[..^1]);
            writer.WriteLine($"extension {parentNamespaces} {{");
            writer.IncreaseIndent();
            writer.WriteLine($"public struct {lastSegment} {{");
            writer.WriteLine("}");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
